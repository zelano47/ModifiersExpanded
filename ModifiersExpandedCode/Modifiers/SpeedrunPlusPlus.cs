namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class SpeedrunPlusPlus : SpeedrunBase
{
    private const float _timeLimitMinutes = 20f;

    public override float _timeLimit { get; set; } = 60f * _timeLimitMinutes;
}
