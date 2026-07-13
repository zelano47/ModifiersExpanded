using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using ModifiersExpanded.ModifiersExpandedCode.State;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class RunSaveManagerPatches
{
    [HarmonyPatch(
        typeof(RunSaveManager),
        nameof(RunSaveManager.SaveRun),
        new[] { typeof(SerializableRun), typeof(bool) }
    )]
    public static class SaveRunSidecarPatch
    {
        public static void Postfix(bool isMultiplayer)
        {
            RunSidecarStateStore.Save(isMultiplayer);
        }
    }

    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))]
    public static class LoadRunSaveSidecarPatch
    {
        public static void Postfix(ReadSaveResult<SerializableRun> __result)
        {
            RunSidecarStateStore.Load(isMultiplayer: false, runSaveLoadSucceeded: __result.Success);
        }
    }

    [HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadMultiplayerRunSave))]
    public static class LoadMultiplayerRunSaveSidecarPatch
    {
        public static void Postfix(ReadSaveResult<SerializableRun> __result)
        {
            RunSidecarStateStore.Load(isMultiplayer: true, runSaveLoadSucceeded: __result.Success);
        }
    }
}
