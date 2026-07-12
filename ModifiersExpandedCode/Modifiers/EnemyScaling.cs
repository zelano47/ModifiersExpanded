using MegaCrit.Sts2.Core.Models;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.State;

public class EnemyScaling : ModifierModel
{
    EnemyScalingState State { get; } = EnemyScalingState.Instance;
    protected override string IconPath => this.GetType().Name.ToSnakeCasePng().ModifierImagePath();
}
