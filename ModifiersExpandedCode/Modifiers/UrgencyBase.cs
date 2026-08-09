using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public abstract class UrgencyBase : AzModifier
{
    public virtual float TimeLimit { get; set; }
    public float StartTime { get; set; }
    public bool IsInCombat { get; set; }
    public float TimeLeft { get; set; }
    public bool RoomExited { get; set; }

    public void Reset()
    {
        TimeLeft = TimeLimit;
        StartTime = 0f;
        IsInCombat = false;
        RoomExited = true;
    }

    protected override void AfterRunLoaded(RunState runState)
    {
        TimerState.UrgencyModifierInstance = this;
        TimerState.TryApplyPendingUrgencySnapshot();
    }

    protected override void AfterRunCreated(RunState runState)
    {
        TimerState.UrgencyModifierInstance = this;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }
        RoomExited = false;
        StartTime = RunManager.Instance.RunTime;
        TimeLeft = TimeLimit;
        IsInCombat = true;
        NRun.Instance?.GlobalUi.TopBar.Timer.RefreshVisibility();
        return Task.CompletedTask;
    }

    public override Task AfterRewardTaken(Player player, Reward reward)
    {
        RoomExited = true;
        return Task.CompletedTask;
    }

    public override Task AfterActEntered()
    {
        RoomExited = true;
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        IsInCombat = false;
        float combatElapsedTime = RunManager.Instance.RunTime - StartTime;
        TimeLeft = TimeLimit - combatElapsedTime;
        if (combatElapsedTime > TimeLimit)
        {
            int damage = (int)Math.Round(combatElapsedTime - TimeLimit);
            foreach (var player in RunState.Players)
            {
                if (player.Creature.CurrentHp <= 0)
                    continue;

                await CreatureCmdCompat.DamageNoSource(
                    new BlockingPlayerChoiceContext(),
                    player.Creature,
                    damage,
                    ValueProp.Unblockable | ValueProp.Unpowered
                );
            }
        }
    }
}
