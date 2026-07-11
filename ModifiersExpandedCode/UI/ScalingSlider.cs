using Godot;
using ModifiersExpanded.ModifiersExpandedCode.State;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// A horizontal slider pre-configured for enemy damage scaling. Emits the new value via
/// the provided callback each time the slider moves.
/// </summary>
public partial class ScalingSlider : HSlider
{
    public ScalingSlider(Action<float> onValueChanged)
    {
        MinValue = 1.0;
        MaxValue = 5.0;
        Step = 0.25;
        Value = EnemyScalingState.Instance.Damage;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, 32);

        // ValueChanged += v => EnemyScaling.Instance.Damage = (float)v;
        ValueChanged += v => onValueChanged?.Invoke((float)v);
    }
}
