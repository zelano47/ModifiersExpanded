using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public static class PotionPatches
{
    [HarmonyPatch(typeof(NPotionContainer), "GrowPotionHolders")]
    public static class ResizePotionHoldersPatch
    {
        public static void Prefix(int newMaxPotionSlots, List<NPotionHolder> ____holders)
        {
            while (____holders.Count > newMaxPotionSlots)
            {
                var holder = ____holders[^1];
                ____holders.RemoveAt(____holders.Count - 1);
                holder.GetParent()?.RemoveChild(holder);
                holder.QueueFree();
            }
        }
    }
}
