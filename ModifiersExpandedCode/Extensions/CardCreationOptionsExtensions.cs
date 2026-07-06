using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ModifiersExpanded.ModifiersExpandedCode.Extensions;

public static class CardCreationOptionsExtensions
{
    // sts2-beta exposes a separate WithFilter(Func<CardModel, bool>) method.
    // sts2 main merged the filter into WithCardPools(pools, filter?).
    // Detect which API is present once at static-init time.

    // sts2-beta: WithCardPools(IEnumerable<CardPoolModel>)  — no filter param
    private static readonly MethodInfo? _withCardPoolsBeta = AccessTools.Method(
        typeof(CardCreationOptions),
        "WithCardPools",
        new[] { typeof(IEnumerable<CardPoolModel>) }
    );

    // sts2-beta: WithFilter(Func<CardModel, bool>)
    private static readonly MethodInfo? _withFilterBeta = AccessTools.Method(
        typeof(CardCreationOptions),
        "WithFilter",
        new[] { typeof(Func<CardModel, bool>) }
    );

    // sts2 main: WithCardPools(IEnumerable<CardPoolModel>, Func<CardModel, bool>?)
    private static readonly MethodInfo? _withCardPoolsMain = AccessTools.Method(
        typeof(CardCreationOptions),
        "WithCardPools",
        new[] { typeof(IEnumerable<CardPoolModel>), typeof(Func<CardModel, bool>) }
    );

    // sts2 main only: CustomCardPool property (absent in sts2-beta)
    private static readonly PropertyInfo? _customCardPoolProp = AccessTools.Property(
        typeof(CardCreationOptions),
        "CustomCardPool"
    );

    // sts2 main only: WithCustomPool(IEnumerable<CardModel>, CardRarityOddsType?)
    private static readonly MethodInfo? _withCustomPoolMain = AccessTools.Method(
        typeof(CardCreationOptions),
        "WithCustomPool"
    );

    /// <summary>
    /// Returns the flat <c>CustomCardPool</c> on sts2-main, or <c>null</c> on sts2-beta
    /// (where the property does not exist).
    /// </summary>
    public static IEnumerable<CardModel>? GetCustomCardPool(this CardCreationOptions options) =>
        (IEnumerable<CardModel>?)_customCardPoolProp?.GetValue(options);

    /// <summary>
    /// Extends the existing <c>CustomCardPool</c> with <paramref name="extra"/> cards.
    /// Only valid to call when <see cref="GetCustomCardPool"/> returns non-null.
    /// </summary>
    public static CardCreationOptions WithExtendedCustomPool(
        this CardCreationOptions options,
        IEnumerable<CardModel> extra
    )
    {
        if (_withCustomPoolMain == null)
            return options;
        var extended = options.GetCustomCardPool()!.Concat(extra);
        return (CardCreationOptions)
            _withCustomPoolMain.Invoke(options, new object?[] { extended, null })!;
    }

    /// <summary>
    /// Replaces the card pools on <paramref name="options"/> and applies
    /// <paramref name="filter"/>, compatible with both sts2 main and sts2-beta.
    /// </summary>
    public static CardCreationOptions WithCardPoolsAndFilter(
        this CardCreationOptions options,
        IEnumerable<CardPoolModel> newPools,
        Func<CardModel, bool>? filter
    )
    {
        var poolList = newPools.ToList();

        if (_withFilterBeta != null && _withCardPoolsBeta != null)
        {
            // sts2-beta: WithCardPools(pools) then WithFilter(filter)
            var afterPools = (CardCreationOptions)
                _withCardPoolsBeta.Invoke(options, new object[] { poolList })!;
            return (CardCreationOptions)
                _withFilterBeta.Invoke(afterPools, new object?[] { filter })!;
        }

        if (_withCardPoolsMain != null)
        {
            // sts2 main: WithCardPools(pools, filter?)
            return (CardCreationOptions)
                _withCardPoolsMain.Invoke(options, new object?[] { poolList, filter })!;
        }

        // Fallback: set pools only (should not be reached)
        MainFile.Logger.Warn(
            MainFile.CreateLogMessage(
                "WithCardPoolsAndFilter: could not find a compatible WithCardPools overload."
            )
        );
        return options;
    }
}
