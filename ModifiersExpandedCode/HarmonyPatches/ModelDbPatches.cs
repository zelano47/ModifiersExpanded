using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class ModelDbPatches
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
    public static class GoodModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            MainFile.Logger.Info(MainFile.CreateLogMessage("Patching ModelDb.GoodModifiers"));
            var patched = new List<ModifierModel>(__result)
            {
                ModelDb.Modifier<ColorlessCards>(),
                ModelDb.Modifier<NeowsBlessing>(),
                ModelDb.Modifier<Enchanter>(),
                ModelDb.Modifier<BodyDouble>(),
                ModelDb.Modifier<PraiseSnecko>(),
                ModelDb.Modifier<Polymath>(),
                ModelDb.Modifier<Chimera>(),
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
                ModelDb.Modifier<Phalanx>(),
                ModelDb.Modifier<Marathon>(),
                ModelDb.Modifier<Pauper>(),
                ModelDb.Modifier<LoneWolf>(),
                ModelDb.Modifier<Hubris>(),
                ModelDb.Modifier<Ephemeral>(),
                ModelDb.Modifier<RunicDome>(),
                ModelDb.Modifier<Speedrun>(),
                ModelDb.Modifier<SpeedrunPlus>(),
                ModelDb.Modifier<Urgency>(),
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
                ModelDb.Modifier<BodyDouble>(),
                ModelDb.Modifier<Chimera>(),
            };
            patched[0] = existingSet;
            __result = patched;
        }
    }
}
