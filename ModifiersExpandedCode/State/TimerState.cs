using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public static class TimerState
{
    public static SpeedrunBase? SpeedrunModifierInstance { get; set; }
    public static UrgencyBase? UrgencyModifierInstance { get; set; }

    public static void Reset()
    {
        UrgencyModifierInstance?.Reset();
    }
}
