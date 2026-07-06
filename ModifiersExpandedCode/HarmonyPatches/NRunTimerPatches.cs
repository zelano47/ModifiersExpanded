using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
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

        public static void Postfix(NRunTimer __instance)
        {
            if (_timerLabelField?.GetValue(__instance) is not MegaLabel timerLabel)
            {
                return;
            }

            UpdateUrgencyTimerLabel(timerLabel);
            UpdateSpeedrunTimerLabel(timerLabel);
        }

        private static void UpdateUrgencyTimerLabel(MegaLabel timerLabel)
        {
            var urgency = GetActiveUrgencyModifier();
            if (urgency == null)
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
                return;
            }
            timerLabel.SetTextAutoSize(FormatCountdown(timerLabelValue));
            ((CanvasItem)timerLabel).SelfModulate = timerLabelValue < 0 ? Colors.Red : Colors.White;
        }

        private static void UpdateSpeedrunTimerLabel(MegaLabel timerLabel)
        {
            var speedrun = GetActiveSpeedrunModifier();
            if (speedrun == null)
            {
                return;
            }
            ((CanvasItem)timerLabel).SelfModulate =
                RunManager.Instance.RunTime > speedrun._timeLimit ? Colors.Red : Colors.White;
        }
    }
}
