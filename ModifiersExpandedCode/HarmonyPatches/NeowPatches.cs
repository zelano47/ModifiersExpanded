using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class NeowPatches
{
    // NeowsBlessing.GenerateNeowOption removes cards from the starting deck (via relics such as
    // WearyTraveler). If another modifier's GenerateNeowOption replaces the starting deck AFTER
    // NeowsBlessing has already pruned it, the replacement deck is never pruned.
    // Fix: ensure NeowsBlessing is always the last modifier in RunState.Modifiers so Neow
    // processes it after all deck-replacement modifiers.
    [HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
    public static class NeowsBlessingLastPatch
    {
        public static void Prefix(ref IReadOnlyList<ModifierModel> modifiers)
        {
            if (modifiers.Count == 0 || modifiers[^1] is NeowsBlessing)
                return;

            var neowsBlessing = modifiers.FirstOrDefault(m => m is NeowsBlessing);
            if (neowsBlessing == null)
                return;

            modifiers = modifiers.Where(m => m is not NeowsBlessing).Append(neowsBlessing).ToList();
        }
    }
}
