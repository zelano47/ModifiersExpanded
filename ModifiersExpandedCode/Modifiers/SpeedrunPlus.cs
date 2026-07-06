namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class SpeedrunPlus : SpeedrunBase
{
    private const float _timeLimitMinutes = 30f;

    public override float _timeLimit { get; set; } = 60f * _timeLimitMinutes;
}
