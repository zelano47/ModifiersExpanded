namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Speedrun : SpeedrunBase
{
    private const float _timeLimitMinutes = 40f;

    public override float _timeLimit { get; set; } = 60f * _timeLimitMinutes;
}
