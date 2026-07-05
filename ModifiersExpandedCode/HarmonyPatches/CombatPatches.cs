using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.ValueProps;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class CombatPatches
{
    // Scale enemy block gains as if 2 players are present when LoneWolf is active.
    [HarmonyPatch(
        typeof(MultiplayerScalingModel),
        nameof(MultiplayerScalingModel.ModifyBlockMultiplicative)
    )]
    public static class LoneWolfBlockScalingPatch
    {
        public static void Postfix(Creature target, ValueProp props, ref decimal __result)
        {
            if (__result != 1m)
                return; // already multiplayer-scaled
            if (target == null || (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy))
                return;
            if (!props.IsPoweredCardOrMonsterMoveBlock())
                return;
            var runState = target.CombatState?.RunState;
            if (runState == null || runState.Players.Count != 1)
                return;
            if (!runState.Modifiers.Any(m => m is LoneWolf))
                return;
            __result =
                2m
                * MultiplayerScalingModel.GetMultiplayerScaling(
                    target.CombatState?.Encounter,
                    runState.CurrentActIndex
                );
        }
    }

    // Hubris sets max HP to 1. With WearyTraveler, Neow heals 80% of max HP (0.8), which
    // truncates to 0 via (int) cast in SetCurrentHpInternal. Ensure a positive heal from 0 HP
    // always results in at least 1 HP when Hubris is active.
    [HarmonyPatch(typeof(Creature), nameof(Creature.HealInternal))]
    public static class HubrisMinHpPatch
    {
        public static void Prefix(Creature __instance, ref decimal amount)
        {
            if (amount <= 0 || __instance.Player == null)
                return;
            if ((int)(__instance.CurrentHp + amount) != 0)
                return;
            if (__instance.Player.RunState.Modifiers.Any(m => m is Hubris))
                amount = 1m - __instance.CurrentHp;
        }
    }
}
