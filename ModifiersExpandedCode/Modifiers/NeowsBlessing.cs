using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class NeowsBlessing : ModifierModel
{
    // Neow's private option-pool property getters, accessed via reflection
    private static readonly MethodInfo _curseOptionsGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "CurseOptions"
    );
    private static readonly MethodInfo _positiveOptionsGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "PositiveOptions"
    );
    private static readonly MethodInfo _lavaRockGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "LavaRockOption"
    );
    private static readonly MethodInfo _smallCapsuleGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "SmallCapsuleOption"
    );
    private static readonly MethodInfo _nutritiousOysterGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "NutritiousOysterOption"
    );
    private static readonly MethodInfo _stoneHumidifierGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "StoneHumidifierOption"
    );
    private static readonly MethodInfo _neowsTalismanGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "NeowsTalismanOption"
    );
    private static readonly MethodInfo _pomanderGetter = AccessTools.PropertyGetter(
        typeof(Neow),
        "PomanderOption"
    );
    private static readonly MethodInfo _setEventStateMethod = AccessTools.Method(
        typeof(EventModel),
        "SetEventState",
        new[] { typeof(LocString), typeof(IEnumerable<EventOption>) }
    );

    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        var neow = (Neow)eventModel;
        return () => ShowRelicChoice(neow);
    }

    private static async Task ShowRelicChoice(Neow neow)
    {
        var tcs = new TaskCompletionSource();
        var options = BuildRelicChoiceOptions(neow, tcs);
        _setEventStateMethod.Invoke(neow, new object[] { neow.InitialDescription, options });
        await tcs.Task;
    }

    // Mirrors the no-modifier branch of Neow.GenerateInitialOptions.
    // Each returned option grants its relic and completes the TCS rather than calling
    // SetEventFinished, so OnModifierOptionSelected can continue the event flow afterward.
    private static List<EventOption> BuildRelicChoiceOptions(Neow neow, TaskCompletionSource tcs)
    {
        var curseOptions = (
            (IEnumerable<EventOption>)_curseOptionsGetter.Invoke(neow, null)!
        ).ToList();
        curseOptions.RemoveAll(r => r.Relic != null && !r.Relic.IsAllowedAtNeow(neow.Owner!));

        var curseChoice = neow.Rng.NextItem(curseOptions)!;

        var positiveOptions = (
            (IEnumerable<EventOption>)_positiveOptionsGetter.Invoke(neow, null)!
        ).ToList();

        if (curseChoice.Relic is CursedPearl)
            positiveOptions.RemoveAll(o => o.Relic is GoldenPearl);
        if (curseChoice.Relic is HeftyTablet)
            positiveOptions.RemoveAll(o => o.Relic is ArcaneScroll);
        if (curseChoice.Relic is LeafyPoultice)
            positiveOptions.RemoveAll(o => o.Relic is NewLeaf);
        if (curseChoice.Relic is PrecariousShears)
            positiveOptions.RemoveAll(o => o.Relic is PreciseScissors);

        if (curseChoice.Relic is not LargeCapsule)
        {
            positiveOptions.Add(
                neow.Rng.NextBool()
                    ? (EventOption)_lavaRockGetter.Invoke(neow, null)!
                    : (EventOption)_smallCapsuleGetter.Invoke(neow, null)!
            );
        }

        positiveOptions.Add(
            neow.Rng.NextBool()
                ? (EventOption)_nutritiousOysterGetter.Invoke(neow, null)!
                : (EventOption)_stoneHumidifierGetter.Invoke(neow, null)!
        );

        positiveOptions.Add(
            neow.Rng.NextBool()
                ? (EventOption)_neowsTalismanGetter.Invoke(neow, null)!
                : (EventOption)_pomanderGetter.Invoke(neow, null)!
        );

        positiveOptions.RemoveAll(r => r.Relic != null && !r.Relic.IsAllowedAtNeow(neow.Owner!));

        var picks = positiveOptions.UnstableShuffle(neow.Rng).Take(2).ToList();
        picks.Add(curseChoice);

        return picks.Select(opt => MakeRelicOption(neow, opt, tcs)).ToList();
    }

    private static EventOption MakeRelicOption(
        Neow neow,
        EventOption original,
        TaskCompletionSource tcs
    )
    {
        var relic = original.Relic!;
        return new EventOption(
            neow,
            async () =>
            {
                await RelicCmd.Obtain(relic, neow.Owner);
                tcs.SetResult();
            },
            original.Title,
            original.Description,
            original.TextKey,
            original.HoverTips
        ).WithRelic(relic);
    }

    protected override string IconPath =>
        nameof(NeowsBlessing).ToSnakeCasePng().ModifierImagePath();
}
