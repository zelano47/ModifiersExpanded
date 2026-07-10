using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ModifiersExpanded.ModifiersExpandedCode.UI;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

/// <summary>
/// Replaces the flat <see cref="NCustomRunModifiersList"/> layout with a grouped accordion
/// UI. Modifiers that belong to a mutual-exclusion set are collapsed into an expandable
/// section; standalone modifiers are displayed directly as before.
///
/// The patch is a Postfix on _Ready: the original method still creates all
/// <see cref="NRunModifierTickbox"/> instances and registers its signal connections. We
/// only reorganise how those tickboxes are parented in the scene tree — all existing
/// mutual-exclusion logic and the ModifiersChanged signal remain untouched.
/// </summary>
public class ModifiersListPatches
{
    private static readonly FieldInfo? _containerField = AccessTools.Field(
        typeof(NCustomRunModifiersList),
        "_container"
    );

    private static readonly FieldInfo? _modifierTickboxesField = AccessTools.Field(
        typeof(NCustomRunModifiersList),
        "_modifierTickboxes"
    );

    // ── Patch ────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(NCustomRunModifiersList), "_Ready")]
    public static class NCustomRunModifiersListReadyPatch
    {
        public static void Postfix(NCustomRunModifiersList __instance)
        {
            if (_containerField?.GetValue(__instance) is not Control container)
            {
                return;
            }
            if (
                _modifierTickboxesField?.GetValue(__instance)
                is not List<NRunModifierTickbox> tickboxes
            )
            {
                return;
            }
            __instance.OffsetLeft = 24;
            ModifierList.RebuildWithAccordionGroups(container, tickboxes);
        }
    }
}
