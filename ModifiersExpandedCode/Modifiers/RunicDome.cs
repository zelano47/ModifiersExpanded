using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class RunicDome : ModifierModel
{
    protected override void AfterRunCreated(RunState runState)
    {
        foreach (Player player in runState.Players)
        {
            player.MaxEnergy += 1;
        }
    }

    protected override string IconPath => nameof(RunicDome).ToSnakeCasePng().ModifierImagePath();
}
