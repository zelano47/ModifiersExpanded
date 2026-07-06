using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Speedrun : ModifierModel
{
    // private const float _timeLimit = 60f * 40f;
    private const float _timeLimit = 60f * 1f;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        CheckTimeLimit();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CheckTimeLimit();
        return Task.CompletedTask;
    }

    private void CheckTimeLimit()
    {
        long currentRunTime = RunManager.Instance.RunTime;
        if (currentRunTime > _timeLimit)
        {
            foreach (var player in RunState.Players)
            {
                CreatureCmd.Kill(player.Creature);
            }
        }
    }
}
