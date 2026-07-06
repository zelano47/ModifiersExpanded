using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
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

    /// <summary>
    /// Force the run timer to be visible whenever a SpeedrunBase modifier is active,
    /// regardless of the user's ShowRunTimer preference or screen state.
    /// </summary>
    [HarmonyPatch(typeof(NRunTimer), nameof(NRunTimer.RefreshVisibility))]
    public static class NRunTimerRefreshVisibilityPatch
    {
        public static void Postfix(NRunTimer __instance)
        {
            if (GetActiveSpeedrunModifier() != null)
                ((CanvasItem)__instance).Visible = true;
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
                return;

            var modifier = GetActiveSpeedrunModifier();
            ((CanvasItem)timerLabel).SelfModulate =
                modifier != null && RunManager.Instance.RunTime > modifier._timeLimit
                    ? Colors.Red
                    : Colors.White;
        }
    }
}
