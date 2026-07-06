using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public abstract class UrgencyBase : ModifierModel
{
    public virtual float _timeLimit { get; set; }
    protected float _startTime { get; set; }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }
        _startTime = RunManager.Instance.RunTime;
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        float elapsedTime = RunManager.Instance.RunTime - _startTime;
        if (elapsedTime > _timeLimit)
        {
            int damage = (int)Math.Round(elapsedTime - _timeLimit);
            foreach (var player in RunState.Players)
            {
                await CreatureCmd.Damage(
                    new BlockingPlayerChoiceContext(),
                    player.Creature,
                    damage,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    null,
                    null
                );
            }
        }
    }

    protected override string IconPath => this.GetType().Name.ToSnakeCasePng().ModifierImagePath();
}
