using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class PraiseSnecko : ModifierModel
{
    protected override void AfterRunCreated(RunState runState)
    {
        foreach (Player player in runState.Players)
        {
            var sneckoEye = ModelDb.Relic<SneckoEye>().ToMutable();
            player.AddRelicInternal(sneckoEye);
            player.RelicGrabBag.Remove<SneckoEye>();
            runState.SharedRelicGrabBag.Remove<SneckoEye>();
        }
    }

    protected override string IconPath => nameof(PraiseSnecko).ToSnakeCasePng().ModifierImagePath();
}
