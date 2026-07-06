using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class NRunTimerPatches
{
    private static SpeedrunBase? GetActiveSpeedrunModifier()
    {
        if (!RunManager.Instance.IsInProgress)
            return null;
        return RunManager
            .Instance.DebugOnlyGetState()
            ?.Modifiers.OfType<SpeedrunBase>()
            .FirstOrDefault();
    }

    private static UrgencyBase? GetActiveUrgencyModifier()
    {
        if (!RunManager.Instance.IsInProgress)
            return null;
        return RunManager
            .Instance.DebugOnlyGetState()
            ?.Modifiers.OfType<UrgencyBase>()
            .FirstOrDefault();
    }

    private static string FormatCountdown(float remaining)
    {
        bool negative = remaining < 0;
        var t = TimeSpan.FromSeconds(Math.Abs(remaining));
        string formatted = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
        return negative ? $"-{formatted}" : formatted;
    }

    /// <summary>
    /// Force the run timer to be visible whenever a SpeedrunBase modifier is active,
    /// regardless of the user's ShowRunTimer preference or screen state.
    /// </summary>
    [HarmonyPatch(typeof(NRunTimer), nameof(NRunTimer.RefreshVisibility))]
    public static class NRunTimerRefreshVisibilityPatch
    {
        public static void Postfix(NRunTimer __instance)
        {
            var urgencyForVisibility = GetActiveUrgencyModifier();
            if (
                GetActiveSpeedrunModifier() != null
                || (urgencyForVisibility != null && !urgencyForVisibility.RoomExited)
                || RunManager.Instance.IsGameOver
            )
            {
                ((CanvasItem)__instance).Visible = true;
            }
        }
    }

    /// <summary>
    /// After the timer label text is updated each second, tint it red if the run time
    /// has exceeded the active SpeedrunBase modifier's time limit.
    /// </summary>
    [HarmonyPatch(typeof(NRunTimer), "OnTimerTimeout")]
    public static class NRunTimerOnTimerTimeoutPatch
    {
        private static readonly FieldInfo? _timerLabelField = AccessTools.Field(
            typeof(NRunTimer),
            "_timerLabel"
        );

        private static long? _gameOverRunTime;

        public static void Postfix(NRunTimer __instance)
        {
            if (_timerLabelField?.GetValue(__instance) is not MegaLabel timerLabel)
                return;

            // When all players are dead and speedrun is active, restore run time display.
            // The base OnTimerTimeout skips its body on IsGameOver, leaving stale text.
            // GetActiveSpeedrunModifier() also returns null on game over (IsInProgress is false),
            // so read the run state directly here.
            if (RunManager.Instance.IsGameOver)
            {
                _gameOverRunTime ??= RunManager.Instance.RunTime;
                var speedrun = RunManager
                    .Instance.DebugOnlyGetState()
                    ?.Modifiers.OfType<SpeedrunBase>()
                    .FirstOrDefault();
                if (speedrun != null)
                {
                    timerLabel.SetTextAutoSize(TimeFormatting.Format(_gameOverRunTime.Value));
                    ((CanvasItem)timerLabel).SelfModulate =
                        _gameOverRunTime.Value > speedrun._timeLimit ? Colors.Red : Colors.White;
                }
                return;
            }

            _gameOverRunTime = null;

            UpdateUrgencyTimerLabel(timerLabel);
            UpdateSpeedrunTimerLabel(timerLabel);
        }

        private static void UpdateUrgencyTimerLabel(MegaLabel timerLabel)
        {
            var urgency = GetActiveUrgencyModifier();
            if (urgency == null || RunManager.Instance.IsGameOver)
            {
                return;
            }
            float timerLabelValue;
            if (urgency.IsInCombat)
            {
                timerLabelValue =
                    urgency.TimeLimit - (RunManager.Instance.RunTime - urgency.StartTime);
            }
            else if (!urgency.RoomExited)
            {
                timerLabelValue = urgency.TimeLeft;
            }
            else
            {
                ((CanvasItem)timerLabel).SelfModulate = Colors.White;
                return;
            }
            timerLabel.SetTextAutoSize(FormatCountdown(timerLabelValue));
            ((CanvasItem)timerLabel).SelfModulate = timerLabelValue < 0 ? Colors.Red : Colors.White;
        }

        private static void UpdateSpeedrunTimerLabel(MegaLabel timerLabel)
        {
            var speedrun = GetActiveSpeedrunModifier();
            var urgency = GetActiveUrgencyModifier();
            if (speedrun == null)
            {
                return;
            }
            if (urgency != null && urgency.RoomExited)
            {
                ((CanvasItem)timerLabel).SelfModulate =
                    RunManager.Instance.RunTime > speedrun._timeLimit ? Colors.Red : Colors.White;
            }
        }
    }
}
