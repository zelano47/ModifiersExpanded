using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using ModifiersExpanded.ModifiersExpandedCode.Utils;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class NRunTimerPatches
{
    /// <summary>
    /// Force the run timer to be visible whenever the active modifiers require it,
    /// regardless of the user's ShowRunTimer preference or screen state.
    /// </summary>
    [HarmonyPatch(typeof(NRunTimer), nameof(NRunTimer.RefreshVisibility))]
    public static class NRunTimerRefreshVisibilityPatch
    {
        public static void Postfix(NRunTimer __instance)
        {
            if (TimerController.ShouldForceVisible())
                ((CanvasItem)__instance).Visible = true;
        }
    }

    /// <summary>
    /// After the timer label is updated each second, apply modifier-specific text and
    /// color based on the current <see cref="TimerDisplayState"/>.
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
            if (_timerLabelField?.GetValue(__instance) is MegaLabel timerLabel)
                TimerController.Apply(timerLabel);
        }
    }
}
