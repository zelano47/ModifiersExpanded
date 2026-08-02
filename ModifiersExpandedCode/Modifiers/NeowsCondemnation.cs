using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.Relics;

public class NeowsCondemnation : ModifierModel
{
    private static readonly MethodInfo _setEventStateMethod = AccessTools.Method(
        typeof(EventModel),
        "SetEventState",
        new[] { typeof(LocString), typeof(IEnumerable<EventOption>) }
    );

    private static List<RelicModel> BuildPoolOne() =>
        [ModelDb.Relic<CursedCandy>().ToMutable(), ModelDb.Relic<CursedRod>().ToMutable()];

    private static List<RelicModel> BuildPoolTwo() => [];

    private static List<RelicModel> BuildPoolThree() => [];

    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        var neow = (Neow)eventModel;
        return () => ShowRelicChoice(neow);
    }

    private static async Task ShowRelicChoice(Neow neow)
    {
        if (neow.Owner == null)
            return;

        var completionSource = new TaskCompletionSource();
        var options = BuildRelicChoiceOptions(neow, completionSource);
        if (options.Count == 0)
            return;

        _setEventStateMethod.Invoke(neow, new object[] { neow.InitialDescription, options });
        if (neow.Node is NAncientEventLayout layout)
        {
            ((GodotObject)(object)layout).CallDeferred(
                NAncientEventLayout.MethodName.SetDialogueLineAndAnimate,
                Variant.From(0)
            );
        }

        await completionSource.Task;
    }

    private static List<EventOption> BuildRelicChoiceOptions(
        Neow neow,
        TaskCompletionSource completionSource
    )
    {
        List<List<RelicModel>> pools = [BuildPoolOne(), BuildPoolTwo(), BuildPoolThree()];

        return pools
            .Where(pool => pool.Count > 0)
            .Select(pool => neow.Rng.NextItem(pool)!)
            .Select(relic => MakeRelicOption(neow, relic, completionSource))
            .ToList();
    }

    private static EventOption MakeRelicOption(
        Neow neow,
        RelicModel relic,
        TaskCompletionSource completionSource
    )
    {
        var owner = neow.Owner ?? throw new InvalidOperationException("Neow has no event owner.");
        relic.Owner = owner;
        return EventOption.FromRelic(
            relic,
            neow,
            async () =>
            {
                await RelicCmd.Obtain(relic, owner);
                completionSource.SetResult();
            },
            $"NEOWS_CONDEMNATION.pages.INITIAL.options.{relic.Id.Entry}"
        );
    }

    protected override string IconPath =>
        nameof(NeowsCondemnation).ToSnakeCasePng().ModifierImagePath();
}
