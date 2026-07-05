using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class RunicDomePatches
{
    // RunicDome relic: hide intent visuals after each visual update.
    [HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
    public static class RunicDomeHideIntentVisualsPatch
    {
        private static readonly FieldInfo _intentHolderField = AccessTools.Field(
            typeof(NIntent),
            "_intentHolder"
        );

        private static readonly FieldInfo _ownerField = AccessTools.Field(
            typeof(NIntent),
            "_owner"
        );

        public static void Postfix(NIntent __instance)
        {
            var owner = _ownerField.GetValue(__instance) as Creature;
            if (owner?.CombatState == null)
                return;

            var localPlayer = LocalContext.GetMe(owner.CombatState);
            if (localPlayer == null)
                return;

            if (!localPlayer.RunState.Modifiers.Any(m => m is RunicDome))
                return;

            if (_intentHolderField.GetValue(__instance) is CanvasItem intentHolder)
                intentHolder.Modulate = Colors.Transparent;
        }
    }

    // RunicDome relic: suppress hover tips on hidden intents.
    [HarmonyPatch(typeof(NIntent), "OnHovered")]
    public static class RunicDomeHideIntentTipPatch
    {
        private static readonly FieldInfo _ownerField = AccessTools.Field(
            typeof(NIntent),
            "_owner"
        );

        public static bool Prefix(NIntent __instance)
        {
            var owner = _ownerField.GetValue(__instance) as Creature;
            if (owner?.CombatState == null)
                return true;

            var localPlayer = LocalContext.GetMe(owner.CombatState);
            if (localPlayer == null)
                return true;

            // Return false (skip original) when RunicDome is active.
            return !localPlayer.RunState.Modifiers.Any(m => m is RunicDome);
        }
    }

    // RunicDome relic: strip intent tips from creature hover tips shown when hovering the enemy sprite.
    [HarmonyPatch(typeof(Creature), "get_HoverTips")]
    public static class RunicDomeHideCreatureIntentTipsPatch
    {
        public static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!__instance.IsMonster)
                return;

            var localPlayer = LocalContext.GetMe(__instance.CombatState);
            if (localPlayer == null || !localPlayer.RunState.Modifiers.Any(m => m is RunicDome))
                return;

            // Intent tips are prepended before power tips; count and skip them.
            int intentTipCount =
                __instance.Monster?.NextMove?.Intents?.Count(i => i.HasIntentTip) ?? 0;
            __result = __result.Skip(intentTipCount);
        }
    }
}
