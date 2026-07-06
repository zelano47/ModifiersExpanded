using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class SaveManagerPatches
{
    /// <summary>
    /// Skip the post-combat save if all players died during a modifier's AfterCombatEnd hook.
    ///
    /// CombatManager.EndCombatInternal calls SaveRun unconditionally after all AfterCombatEnd
    /// hooks return. If a modifier (e.g. UrgencyBase) deals lethal penalty damage inside that
    /// hook, CreatureCmd.Damage internally calls Kill, which calls RunManager.OnEnded and
    /// DeleteCurrentRun to remove the save file. Without this patch, the subsequent SaveRun
    /// call recreates the save as "in-progress", making the Continue button appear in the
    /// main menu and soft-locking the game if used.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.SaveRun),
        new[] { typeof(AbstractRoom), typeof(bool) }
    )]
    public static class SkipSaveRunWhenGameOverPatch
    {
        public static bool Prefix(ref Task __result)
        {
            if (RunManager.Instance?.IsGameOver == true)
            {
                __result = Task.CompletedTask;
                return false;
            }
            return true;
        }
    }
}
