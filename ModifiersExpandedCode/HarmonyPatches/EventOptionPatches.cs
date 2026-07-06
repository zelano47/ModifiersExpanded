using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class EventOptionPatches
{
    // Nonupeipe already guards BeautifulBracelet with Swift.CanEnchant >= 4.
    // Glitter is in the fixed pool with no guard — patch it to match the same pattern.
    [HarmonyPatch(typeof(Nonupeipe), "OptionPool", MethodType.Getter)]
    public static class NonupeipeOptionPoolPatch
    {
        public static void Postfix(Nonupeipe __instance, ref IEnumerable<EventOption> __result)
        {
            var cards = __instance.Owner?.Deck?.Cards;
            if (cards == null)
                return;
            if (cards.Count(ModelDb.Enchantment<Glam>().CanEnchant) == 0)
                __result = __result.Where(o => o.Relic is not Glitter);
        }
    }

    // Orobas has no guard for ElectricShrymp (Imbued), which uses CardSelectCmd.FromDeckForEnchantment.
    // Offering it with 0 Imbued-enchantable cards would cause a UI soft-lock.
    [HarmonyPatch(typeof(Orobas), "OptionPool1", MethodType.Getter)]
    public static class OrobasOptionPool1Patch
    {
        public static void Postfix(Orobas __instance, ref IEnumerable<EventOption> __result)
        {
            var cards = __instance.Owner?.Deck?.Cards;
            if (cards == null)
                return;
            if (cards.Count(ModelDb.Enchantment<Imbued>().CanEnchant) == 0)
                __result = __result.Where(o => o.Relic is not ElectricShrymp);
        }
    }

    // DrowningBeacon's Climb option costs max HP and awards FresnelLens (Nimble enchantment relic).
    // With Enchanter active, all card rewards are already enchanted — FresnelLens.CanEnchant returns
    // false for every card and the relic does nothing. Remove the Climb option so the player isn't
    // offered a worthless relic at the cost of max HP.
    [HarmonyPatch(typeof(DrowningBeacon), "GenerateInitialOptions")]
    public static class DrowningBeaconGenerateInitialOptionsPatch
    {
        public static void Postfix(
            DrowningBeacon __instance,
            ref IReadOnlyList<EventOption> __result
        )
        {
            var modifiers = __instance.Owner?.RunState?.Modifiers;
            if (modifiers == null || !modifiers.Any(m => m is Enchanter))
                return;
            __result = __result
                .Where(o => o.TextKey != "DROWNING_BEACON.pages.INITIAL.options.CLIMB")
                .ToList();
        }
    }
}
