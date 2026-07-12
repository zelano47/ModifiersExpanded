using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ModifiersExpanded.ModifiersExpandedCode.State;

public class MainMenuPatches
{
    [HarmonyPatch(typeof(NMainMenu), "_Ready")]
    public static class NMainMenuReadyPatch
    {
        public static void Postfix(NMainMenu __instance)
        {
            TimerState.Reset();
        }
    }
}
