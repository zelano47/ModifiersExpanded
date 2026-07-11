using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.State;

public class EnemyScaling : ModifierModel
{
    EnemyScalingState State { get; } = EnemyScalingState.Instance;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (dealer != null && dealer.IsEnemy && target != null && target.IsPlayer)
        {
            return (decimal)State.Damage;
        }
        return 1m;
    }

    protected override string IconPath => this.GetType().Name.ToSnakeCasePng().ModifierImagePath();
}
