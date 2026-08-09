using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class PraiseSnecko : AzModifier
{
    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () => ObtainSnecko(eventModel);
    }

    private static async Task ObtainSnecko(EventModel eventModel)
    {
        var player = eventModel.Owner;
        if (player == null)
            return;
        await RelicCmd.Obtain(ModelDb.Relic<SneckoEye>().ToMutable(), player);
    }
}
