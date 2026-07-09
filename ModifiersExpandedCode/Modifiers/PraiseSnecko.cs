using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.Utils;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class PraiseSnecko : ModifierModel
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

    protected override string IconPath => nameof(PraiseSnecko).ToSnakeCasePng().ModifierImagePath();
}
