using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;
using ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class HarmonyPatches
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
    public static class GoodModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            MainFile.Logger.Info(
                MainFile.CreateLogMessage(
                    "Patching ModelDb.GoodModifiers to add Neow's Blessing, Enchanter, and Colorless Cards"
                )
            );
            var patched = new List<ModifierModel>(__result)
            {
                ModelDb.Modifier<ColorlessCards>(),
                ModelDb.Modifier<NeowsBlessing>(),
                ModelDb.Modifier<Enchanter>(),
            };
            __result = patched;
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.BadModifiers), MethodType.Getter)]
    public static class BadModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            MainFile.Logger.Info(MainFile.CreateLogMessage("Patching ModelDb.BadModifiers"));
            var patched = new List<ModifierModel>(__result)
            {
                ModelDb.Modifier<UnmovableMonsters>(),
                ModelDb.Modifier<Marathon>(),
                ModelDb.Modifier<Pauper>(),
            };
            __result = patched;
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.MutuallyExclusiveModifiers), MethodType.Getter)]
    public static class MutuallyExclusiveModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
        {
            MainFile.Logger.Info(
                MainFile.CreateLogMessage("Patching ModelDb.MutuallyExclusiveModifiers")
            );
            var patched = new List<IReadOnlySet<ModifierModel>>(__result);
            var existingSet = new HashSet<ModifierModel>(patched[0])
            {
                ModelDb.Modifier<NeowsBlessing>(),
            };
            patched[0] = existingSet;
            __result = patched;
        }
    }

    [HarmonyPatch(typeof(Neow), "GenerateInitialOptions")]
    public static class NeowAlwaysNormalOptionsPatch
    {
        // Extends `if (Modifiers.Count <= 0)` to also pass when NeowRelic is active:
        //   if (Modifiers.Count <= 0 || Modifiers.Any(m => m is NeowRelic))
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator gen
        )
        {
            MainFile.Logger.Info(
                MainFile.CreateLogMessage(
                    "Patching Neow.GenerateInitialOptions to offer normal options when NeowRelic is present"
                )
            );
            var codes = new List<CodeInstruction>(instructions);
            var hasNeowRelic = AccessTools.Method(
                typeof(NeowAlwaysNormalOptionsPatch),
                nameof(HasNeowRelic)
            );

            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode != OpCodes.Ldc_I4_0)
                    continue;

                // Form A: ldc.i4.0; bgt ELSE  — fall-through is normal path, branch is else
                if (codes[i + 1].opcode == OpCodes.Bgt || codes[i + 1].opcode == OpCodes.Bgt_S)
                {
                    var elseLabel = (Label)codes[i + 1].operand;
                    var normalLabel = gen.DefineLabel();

                    // ble NORMAL branches when count <= 0 (same as the original condition)
                    codes[i + 1] = new CodeInstruction(OpCodes.Ble, normalLabel);
                    codes.InsertRange(
                        i + 2,
                        new[]
                        {
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Call, hasNeowRelic),
                            new CodeInstruction(OpCodes.Brtrue, normalLabel),
                            new CodeInstruction(OpCodes.Br, elseLabel),
                        }
                    );
                    // The original fall-through (now shifted by 4) is the normal-options code
                    codes[i + 6].labels.Add(normalLabel);
                    break;
                }

                // Form B: ldc.i4.0; ble NORMAL — branch is normal path, fall-through is else
                if (codes[i + 1].opcode == OpCodes.Ble || codes[i + 1].opcode == OpCodes.Ble_S)
                {
                    var normalLabel = (Label)codes[i + 1].operand;
                    codes.InsertRange(
                        i + 2,
                        new[]
                        {
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Call, hasNeowRelic),
                            new CodeInstruction(OpCodes.Brtrue, normalLabel),
                        }
                    );
                    break;
                }
            }

            return codes;
        }

        private static bool HasNeowRelic(Neow neow) =>
            neow.Owner?.RunState?.Modifiers?.Any(m => m is NeowsBlessing) ?? false;
    }

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

    // TESTING ONLY: cycle event rooms through DrowningBeacon → Orobas → Nonupeipe. Delete before shipping.
    // [HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
    // public static class ForceEventCyclePatch
    // {
    //     private static int _index = 0;
    //     private static readonly EventModel[] _events = new EventModel[]
    //     {
    //         ModelDb.Event<Orobas>(),
    //         ModelDb.Event<Nonupeipe>(),
    //     };

    //     public static void Postfix(ref EventModel __result)
    //     {
    //         __result = _events[_index % _events.Length];
    //         _index++;
    //     }
    // }
}
