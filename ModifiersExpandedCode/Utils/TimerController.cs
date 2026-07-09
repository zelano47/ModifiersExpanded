using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.Utils;

public enum TimerDisplayState
{
    Normal, // No modifier active; base NRunTimer owns the display
    Speedrun, // SpeedrunBase active; show run time tinted by limit
    UrgencyCountdown, // UrgencyBase active, in combat; show live countdown
    UrgencyFrozen, // UrgencyBase active, combat just ended; show frozen TimeLeft
    GameOver, // All players dead; show frozen run time
}

public static class TimerController
{
    private static long? _gameOverRunTime;

    // ── State resolution ────────────────────────────────────────────────────

    public static TimerDisplayState GetState()
    {
        // IsGameOver internally checks IsInProgress; returns false when State is null.
        if (RunManager.Instance.IsGameOver)
            return TimerDisplayState.GameOver;

        if (!RunManager.Instance.IsInProgress)
            return TimerDisplayState.Normal;

        var urgency = GetActiveUrgencyModifier();
        if (urgency is { IsInCombat: true })
            return TimerDisplayState.UrgencyCountdown;
        if (urgency is { RoomExited: false })
            return TimerDisplayState.UrgencyFrozen;

        if (GetActiveSpeedrunModifier() != null)
            return TimerDisplayState.Speedrun;

        return TimerDisplayState.Normal;
    }

    /// <summary>Returns true when the timer should be forced visible.</summary>
    public static bool ShouldForceVisible() => GetState() != TimerDisplayState.Normal;

    // ── Display application ─────────────────────────────────────────────────

    /// <summary>
    /// Called from the NRunTimer.OnTimerTimeout Postfix. Sets label text and color
    /// based on the current state. The base NRunTimer already set the text when the
    /// run is in progress and not game-over; we override only when needed.
    /// </summary>
    public static void Apply(MegaLabel timerLabel)
    {
        var state = GetState();

        // Clear the game-over time latch whenever we leave the GameOver state.
        if (state != TimerDisplayState.GameOver)
            _gameOverRunTime = null;

        switch (state)
        {
            case TimerDisplayState.GameOver:
                ApplyGameOver(timerLabel);
                break;
            case TimerDisplayState.UrgencyCountdown:
                ApplyUrgencyCountdown(timerLabel);
                break;
            case TimerDisplayState.UrgencyFrozen:
                ApplyUrgencyFrozen(timerLabel);
                break;
            case TimerDisplayState.Speedrun:
                ApplySpeedrun(timerLabel);
                break;
            default: // Normal
                // Text already set by base NRunTimer. Reset color in case a previous
                // state left it tinted.
                ((CanvasItem)timerLabel).SelfModulate = Colors.White;
                break;
        }
    }

    // ── Per-state display logic ─────────────────────────────────────────────

    private static void ApplyGameOver(MegaLabel timerLabel)
    {
        // Latch the run time at the first game-over tick so the display freezes.
        // The base OnTimerTimeout body is skipped on IsGameOver, so we own the text.
        _gameOverRunTime ??= RunManager.Instance.RunTime;

        // Read speedrun modifier directly; GetActiveSpeedrunModifier() guards on
        // IsInProgress which is false after game over.
        var speedrun = TimerState.SpeedrunModifierInstance;

        if (speedrun == null)
            return;

        timerLabel.SetTextAutoSize(TimeFormatting.Format(_gameOverRunTime.Value));
        ((CanvasItem)timerLabel).SelfModulate =
            _gameOverRunTime.Value > speedrun._timeLimit ? Colors.Red : Colors.White;
    }

    private static void ApplyUrgencyCountdown(MegaLabel timerLabel)
    {
        var urgency = GetActiveUrgencyModifier()!;
        float remaining = urgency.TimeLimit - (RunManager.Instance.RunTime - urgency.StartTime);
        timerLabel.SetTextAutoSize(FormatCountdown(remaining));
        ((CanvasItem)timerLabel).SelfModulate = remaining < 0 ? Colors.Red : Colors.White;
    }

    private static void ApplyUrgencyFrozen(MegaLabel timerLabel)
    {
        var urgency = GetActiveUrgencyModifier()!;
        timerLabel.SetTextAutoSize(FormatCountdown(urgency.TimeLeft));
        ((CanvasItem)timerLabel).SelfModulate = urgency.TimeLeft < 0 ? Colors.Red : Colors.White;
    }

    private static void ApplySpeedrun(MegaLabel timerLabel)
    {
        // Text already set by base NRunTimer. Apply the over-limit tint.
        var speedrun = GetActiveSpeedrunModifier()!;
        ((CanvasItem)timerLabel).SelfModulate =
            RunManager.Instance.RunTime > speedrun._timeLimit ? Colors.Red : Colors.White;
    }

    // ── Modifier helpers ────────────────────────────────────────────────────

    private static SpeedrunBase? GetActiveSpeedrunModifier()
    {
        return TimerState.SpeedrunModifierInstance;
    }

    private static UrgencyBase? GetActiveUrgencyModifier()
    {
        return TimerState.UrgencyModifierInstance;
    }

    // ── Formatting ──────────────────────────────────────────────────────────

    private static string FormatCountdown(float remaining)
    {
        bool negative = remaining < 0;
        var t = TimeSpan.FromSeconds(Math.Abs(remaining));
        string formatted = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
        return negative ? $"-{formatted}" : formatted;
    }
}
