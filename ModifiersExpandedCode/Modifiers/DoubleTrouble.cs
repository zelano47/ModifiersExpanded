using MegaCrit.Sts2.Core.Runs;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class DoubleTrouble : AzModifier
{
    protected override void AfterRunLoaded(RunState runState)
    {
        EnsureSecondBosses(runState);
    }

    internal static void EnsureSecondBosses(RunState runState)
    {
        foreach (var act in runState.Acts.Where(act => !act.HasSecondBoss))
        {
            var secondBoss = runState.Rng.UpFront.NextItem(
                act.AllBossEncounters.Where(encounter => encounter.Id != act.BossEncounter.Id)
            );
            act.SetSecondBossEncounter(secondBoss);
        }
    }
}
