using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Hubris : AzModifier
{
    protected override void AfterRunCreated(RunState runState)
    {
        foreach (Player player in runState.Players)
        {
            CreatureCmd.SetMaxAndCurrentHp(player.Creature, 1m);
        }
    }
}
