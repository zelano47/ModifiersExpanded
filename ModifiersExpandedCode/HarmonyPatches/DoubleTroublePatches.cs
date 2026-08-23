using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class DoubleTroublePatches
{
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
    public static class GenerateRoomsPatch
    {
        public static void Postfix(RunManager __instance)
        {
            var runState = __instance.DebugOnlyGetState();
            if (runState?.Modifiers.Any(modifier => modifier is DoubleTrouble) == true)
                DoubleTrouble.EnsureSecondBosses(runState);
        }
    }
}