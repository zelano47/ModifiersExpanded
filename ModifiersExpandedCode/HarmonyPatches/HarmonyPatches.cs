using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class HarmonyPatches
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
    public static class GoodModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            MainFile.Logger.Info(
                "Patching ModelDb.GoodModifiers to add Neow's Blessing and Enchanter"
            );
            var patched = new List<ModifierModel>(__result)
            {
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
            MainFile.Logger.Info("Patching ModelDb.BadModifiers to add Unmovable Monsters");
            var patched = new List<ModifierModel>(__result)
            {
                ModelDb.Modifier<UnmovableMonsters>(),
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
                "Patching ModelDb.MutuallyExclusiveModifiers to add Neow's Blessing"
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
                "Patching Neow.GenerateInitialOptions to offer normal options when NeowRelic is present"
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
}
