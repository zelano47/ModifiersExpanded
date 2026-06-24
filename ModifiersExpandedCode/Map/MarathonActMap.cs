using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace ModifiersExpanded.ModifiersExpandedCode.Map;

public class MarathonActMap : ActMap
{
    private const int _mapWidth = 7;
    private const decimal _restSiteModifier = 0.5m;
    private const decimal _eliteModifier = 4m;
    private const decimal _lengthModifier = 1.5m;

    private readonly MapPointTypeCounts _pointTypeCounts;
    private readonly int _mapLength;
    private readonly int _treasureRow;
    private readonly Rng _rng;
    private readonly bool _shouldReplaceTreasureWithElites;

    private static readonly HashSet<MapPointType> _lowerMapPointRestrictions =
        new HashSet<MapPointType> { MapPointType.RestSite, MapPointType.Elite };

    private static readonly HashSet<MapPointType> _upperMapPointRestrictions =
        new HashSet<MapPointType> { MapPointType.RestSite };

    private static readonly HashSet<MapPointType> _parentMapPointRestrictions =
        new HashSet<MapPointType>
        {
            MapPointType.Elite,
            MapPointType.RestSite,
            MapPointType.Treasure,
            MapPointType.Shop,
        };

    private static readonly HashSet<MapPointType> _childMapPointRestrictions =
        new HashSet<MapPointType>
        {
            MapPointType.Elite,
            MapPointType.RestSite,
            MapPointType.Treasure,
            MapPointType.Shop,
        };

    private static readonly HashSet<MapPointType> _siblingPointTypeRestrictions =
        new HashSet<MapPointType>
        {
            MapPointType.RestSite,
            MapPointType.Monster,
            MapPointType.Unknown,
            MapPointType.Elite,
            MapPointType.Shop,
        };

    public override MapPoint BossMapPoint { get; }
    public override MapPoint StartingMapPoint { get; }
    public override MapPoint? SecondBossMapPoint { get; }
    protected override MapPoint?[,] Grid { get; }

    public MarathonActMap(
        Rng mapRng,
        ActModel actModel,
        bool isMultiplayer,
        bool shouldReplaceTreasureWithElites,
        bool hasSecondBoss = false
    )
    {
        int baseRooms = actModel.GetNumberOfRooms(isMultiplayer);
        _mapLength = (int)Math.Ceiling(baseRooms * _lengthModifier) + 1;
        _treasureRow = _mapLength / 2;
        _shouldReplaceTreasureWithElites = shouldReplaceTreasureWithElites;
        Grid = new MapPoint[_mapWidth, _mapLength];
        _rng = mapRng;

        var baseCounts = actModel.GetMapPointTypes(mapRng);
        _pointTypeCounts = new MapPointTypeCounts(
            unknownCount: (int)Math.Ceiling(baseCounts.NumOfUnknowns * _lengthModifier),
            restCount: Math.Max(0, (int)Math.Ceiling(baseCounts.NumOfRests * _restSiteModifier))
        )
        {
            NumOfElites = (int)Math.Ceiling(baseCounts.NumOfElites * _eliteModifier),
        };

        BossMapPoint = new MapPoint(GetColumnCount() / 2, GetRowCount());
        StartingMapPoint = new MapPoint(GetColumnCount() / 2, 0);
        if (hasSecondBoss)
            SecondBossMapPoint = new MapPoint(GetColumnCount() / 2, GetRowCount() + 1);

        GenerateMap();
        AssignPointTypes();
        MapPathPruning.PruneAndRepair(
            Grid,
            startMapPoints,
            this,
            _pointTypeCounts,
            _rng,
            IsValidPointType
        );
        Grid = MapPostProcessing.CenterGrid(Grid);
        Grid = MapPostProcessing.SpreadAdjacentMapPoints(Grid);
        Grid = MapPostProcessing.StraightenPaths(Grid);
    }

    private MapPoint GetOrCreateMapPoint(MapCoord coord) => GetOrCreatePoint(coord.col, coord.row);

    private MapPoint GetOrCreatePoint(int col, int row)
    {
        MapPoint? point = GetPoint(col, row);
        if (point != null)
            return point;
        point = new MapPoint(col, row);
        Grid[col, row] = point;
        return point;
    }

    private void PathGenerate(MapPoint startingPoint)
    {
        MapPoint mapPoint = startingPoint;
        while (mapPoint.coord.row < _mapLength - 1)
        {
            MapCoord coord = GenerateNextCoord(mapPoint);
            MapPoint next = GetOrCreateMapPoint(coord);
            mapPoint.AddChildPoint(next);
            mapPoint = next;
        }
    }

    private MapCoord GenerateNextCoord(MapPoint current)
    {
        int col = current.coord.col;
        int left = Mathf.Max(0, col - 1);
        int right = Mathf.Min(col + 1, 6);

        List<int> offsets = new List<int>(3);
        CollectionsMarshal.SetCount(offsets, 3);
        Span<int> span = CollectionsMarshal.AsSpan(offsets);
        span[0] = -1;
        span[1] = 0;
        span[2] = 1;
        offsets.StableShuffle(_rng);

        foreach (int offset in offsets)
        {
            int targetCol = offset switch
            {
                -1 => left,
                0 => col,
                1 => right,
                _ => throw new InvalidOperationException("Unexpected offset"),
            };
            if (!HasInvalidCrossover(current, targetCol))
            {
                return new MapCoord { col = targetCol, row = current.coord.row + 1 };
            }
        }
        throw new InvalidOperationException($"Cannot find next node: seed={_rng.Seed}");
    }

    private bool HasInvalidCrossover(MapPoint current, int targetX)
    {
        int delta = targetX - current.coord.col;
        if (delta == 0 || delta == 7)
            return false;

        MapPoint? neighbor = Grid[targetX, current.coord.row];
        if (neighbor == null)
            return false;

        foreach (MapPoint child in neighbor.Children)
        {
            if (child.coord.col - neighbor.coord.col == -delta)
                return true;
        }
        return false;
    }

    private void GenerateMap()
    {
        for (int i = 0; i < 7; i++)
        {
            MapPoint start = GetOrCreatePoint(_rng.NextInt(0, 7), 1);
            if (i == 1)
            {
                while (startMapPoints.Contains(start))
                    start = GetOrCreatePoint(_rng.NextInt(0, 7), 1);
            }
            startMapPoints.Add(start);
            PathGenerate(start);
        }

        ForEachInRow(Grid, GetRowCount() - 1, x => x.AddChildPoint(BossMapPoint));
        if (SecondBossMapPoint != null)
            BossMapPoint.AddChildPoint(SecondBossMapPoint);
        ForEachInRow(Grid, 1, x => StartingMapPoint.AddChildPoint(x));
    }

    private static void ForEachInRow(MapPoint?[,] grid, int rowIndex, Action<MapPoint> processor)
    {
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            MapPoint? mapPoint = grid[i, rowIndex];
            if (mapPoint != null)
                processor(mapPoint);
        }
    }

    private void AssignPointTypes()
    {
        ForEachInRow(
            Grid,
            GetRowCount() - 1,
            p =>
            {
                p.PointType = MapPointType.RestSite;
                p.CanBeModified = false;
            }
        );

        if (_shouldReplaceTreasureWithElites)
            ForEachInRow(
                Grid,
                _treasureRow,
                p =>
                {
                    p.PointType = MapPointType.Elite;
                    p.CanBeModified = false;
                }
            );
        else
            ForEachInRow(
                Grid,
                _treasureRow,
                p =>
                {
                    p.PointType = MapPointType.Treasure;
                    p.CanBeModified = false;
                }
            );

        ForEachInRow(
            Grid,
            1,
            p =>
            {
                p.PointType = MapPointType.Monster;
                p.CanBeModified = false;
            }
        );

        var toAssign = new Queue<MapPointType>();
        for (int i = 0; i < _pointTypeCounts.NumOfRests; i++)
            toAssign.Enqueue(MapPointType.RestSite);
        for (int i = 0; i < _pointTypeCounts.NumOfShops; i++)
            toAssign.Enqueue(MapPointType.Shop);
        for (int i = 0; i < _pointTypeCounts.NumOfElites; i++)
            toAssign.Enqueue(MapPointType.Elite);
        for (int i = 0; i < _pointTypeCounts.NumOfUnknowns; i++)
            toAssign.Enqueue(MapPointType.Unknown);

        AssignRemainingTypesToRandomPoints(toAssign);

        foreach (MapPoint p in GetAllMapPoints().Where(x => x.PointType == MapPointType.Unassigned))
            p.PointType = MapPointType.Monster;

        BossMapPoint.PointType = MapPointType.Boss;
        StartingMapPoint.PointType = MapPointType.Ancient;
        if (SecondBossMapPoint != null)
            SecondBossMapPoint.PointType = MapPointType.Boss;
    }

    private void AssignRemainingTypesToRandomPoints(Queue<MapPointType> toAssign)
    {
        for (int i = 0; i < 3; i++)
        {
            if (toAssign.Count == 0)
                break;

            List<MapPoint> unassigned = GetAllMapPoints()
                .Where(p => p.PointType == MapPointType.Unassigned)
                .ToList();
            unassigned.StableShuffle(_rng);

            foreach (MapPoint point in unassigned)
            {
                if (toAssign.Count == 0)
                    break;
                point.PointType = GetNextValidPointType(toAssign, point);
            }
        }
    }

    private MapPointType GetNextValidPointType(Queue<MapPointType> queue, MapPoint mapPoint)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            MapPointType type = queue.Dequeue();
            if (
                _pointTypeCounts.ShouldIgnoreMapPointRulesForMapPointType(type)
                || IsValidPointType(type, mapPoint)
            )
                return type;
            queue.Enqueue(type);
        }
        return MapPointType.Unassigned;
    }

    public bool IsValidPointType(MapPointType pointType, MapPoint mapPoint)
    {
        return IsValidForUpper(pointType, mapPoint)
            && IsValidForLower(pointType, mapPoint)
            && IsValidWithParents(pointType, mapPoint)
            && IsValidWithChildren(pointType, mapPoint)
            && IsValidWithSiblings(pointType, mapPoint);
    }

    private static bool IsValidForLower(MapPointType pointType, MapPoint mapPoint) =>
        mapPoint.coord.row >= 6 || !_lowerMapPointRestrictions.Contains(pointType);

    private bool IsValidForUpper(MapPointType pointType, MapPoint mapPoint) =>
        mapPoint.coord.row < _mapLength - 3 || !_upperMapPointRestrictions.Contains(pointType);

    private static bool IsValidWithParents(MapPointType pointType, MapPoint mapPoint)
    {
        if (!_parentMapPointRestrictions.Contains(pointType))
            return true;
        return !mapPoint.parents.Concat(mapPoint.Children).Any(p => p.PointType == pointType);
    }

    private static bool IsValidWithChildren(MapPointType pointType, MapPoint mapPoint)
    {
        if (!_childMapPointRestrictions.Contains(pointType))
            return true;
        return !mapPoint.Children.Any(p => p.PointType == pointType);
    }

    private static bool IsValidWithSiblings(MapPointType pointType, MapPoint mapPoint)
    {
        if (!_siblingPointTypeRestrictions.Contains(pointType))
            return true;
        return !GetSiblings(mapPoint).Any(p => p.PointType == pointType);
    }

    private static IEnumerable<MapPoint> GetSiblings(MapPoint mapPoint) =>
        mapPoint.parents.SelectMany(p => p.Children).Where(x => !object.Equals(x, mapPoint));
}
