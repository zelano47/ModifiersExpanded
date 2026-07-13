using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public static class TimerState
{
    public sealed class UrgencySnapshot
    {
        public float StartTime { get; set; }
        public bool IsInCombat { get; set; }
        public float TimeLeft { get; set; }
        public bool RoomExited { get; set; }
    }

    public static SpeedrunBase? SpeedrunModifierInstance { get; set; }
    public static UrgencyBase? UrgencyModifierInstance { get; set; }

    private static UrgencySnapshot? PendingUrgencySnapshot { get; set; }

    public static UrgencySnapshot? CaptureUrgencySnapshot()
    {
        var urgency = UrgencyModifierInstance;
        if (urgency == null)
        {
            return null;
        }

        return new UrgencySnapshot
        {
            StartTime = urgency.StartTime,
            IsInCombat = urgency.IsInCombat,
            TimeLeft = urgency.TimeLeft,
            RoomExited = urgency.RoomExited,
        };
    }

    public static void SetPendingUrgencySnapshot(UrgencySnapshot? snapshot)
    {
        PendingUrgencySnapshot = snapshot;
        TryApplyPendingUrgencySnapshot();
    }

    public static void TryApplyPendingUrgencySnapshot()
    {
        var urgency = UrgencyModifierInstance;
        var pendingSnapshot = PendingUrgencySnapshot;
        if (urgency == null || pendingSnapshot == null)
        {
            return;
        }

        urgency.StartTime = pendingSnapshot.StartTime;
        urgency.IsInCombat = pendingSnapshot.IsInCombat;
        urgency.TimeLeft = pendingSnapshot.TimeLeft;
        urgency.RoomExited = pendingSnapshot.RoomExited;
        PendingUrgencySnapshot = null;
    }

    public static void Reset()
    {
        UrgencyModifierInstance?.Reset();
        SpeedrunModifierInstance = null;
        UrgencyModifierInstance = null;
        PendingUrgencySnapshot = null;
    }
}
