using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public abstract class SpeedrunBase : AzModifier
{
    public virtual float _timeLimit { get; set; }

    protected override void AfterRunCreated(RunState runState)
    {
        TimerState.SpeedrunModifierInstance = this;
    }

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

    protected void CheckTimeLimit()
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
