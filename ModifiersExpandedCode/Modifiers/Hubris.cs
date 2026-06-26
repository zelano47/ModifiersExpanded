using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

public class Hubris : ModifierModel
{
    protected override void AfterRunCreated(RunState runState)
    {
        foreach (Player player in runState.Players)
        {
            CreatureCmd.SetMaxAndCurrentHp(player.Creature, 1m);
        }
    }

    protected override string IconPath => nameof(Hubris).ToSnakeCasePng().ModifierImagePath();
}
