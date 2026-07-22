using MegaCrit.Sts2.Core.Map;

namespace ModifiersExpanded.ModifiersExpandedCode.Map;

public class SprintMap : ActMap
{
    private const int _col = 0;

    public override MapPoint BossMapPoint { get; }
    public override MapPoint StartingMapPoint { get; }
    protected override MapPoint?[,] Grid { get; }
    public override MapPoint? SecondBossMapPoint { get; }

    public SprintMap(bool hasSecondBoss = false)
    {
        Grid = new MapPoint?[1, 7];
        StartingMapPoint = new MapPoint(_col, 0)
        {
            PointType = MapPointType.Ancient,
            CanBeModified = false,
        };
        BossMapPoint = new MapPoint(_col, GetRowCount())
        {
            PointType = MapPointType.Boss,
            CanBeModified = false,
        };

        var monster = CreateNode(1, MapPointType.Monster);
        var unknown = CreateNode(2, MapPointType.Unknown);
        var treasure = CreateNode(3, MapPointType.Treasure);
        var elite = CreateNode(4, MapPointType.Elite);
        var shop = CreateNode(5, MapPointType.Shop);
        var restSite = CreateNode(6, MapPointType.RestSite);

        // Wire up the single linear path
        StartingMapPoint.AddChildPoint(monster);
        monster.AddChildPoint(unknown);
        unknown.AddChildPoint(treasure);
        treasure.AddChildPoint(elite);
        elite.AddChildPoint(shop);
        shop.AddChildPoint(restSite);
        restSite.AddChildPoint(BossMapPoint);

        if (hasSecondBoss)
        {
            SecondBossMapPoint = new MapPoint(_col, GetRowCount() + 1);
            SecondBossMapPoint.PointType = MapPointType.Boss;
            BossMapPoint.AddChildPoint(SecondBossMapPoint);
        }

        startMapPoints.Add(monster);
    }

    private MapPoint CreateNode(int row, MapPointType pointType)
    {
        var point = new MapPoint(_col, row) { PointType = pointType, CanBeModified = false };
        Grid[_col, row] = point;
        return point;
    }
}
