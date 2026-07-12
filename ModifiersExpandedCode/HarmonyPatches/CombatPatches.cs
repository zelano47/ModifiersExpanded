using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Runs;
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

    // Apply EnemyScaling enemy->player damage multiplier via Hook.ModifyDamageInternal
    // so we do not rely on branch-specific AbstractModel override signatures.
    [HarmonyPatch]
    public static class EnemyScalingDamagePatch
    {
        public static MethodBase? TargetMethod() =>
            AccessTools.FirstMethod(
                typeof(Hook),
                m =>
                {
                    if (m.Name != "ModifyDamageInternal" || m.ReturnType != typeof(decimal))
                        return false;

                    var parameters = m.GetParameters();
                    if (parameters.Length != 9 && parameters.Length != 10)
                        return false;

                    // Shared prefix across main and beta signatures.
                    if (parameters[0].ParameterType != typeof(IRunState))
                        return false;
                    if (parameters[1].ParameterType != typeof(ICombatState))
                        return false;
                    if (parameters[2].ParameterType != typeof(Creature))
                        return false;
                    if (parameters[3].ParameterType != typeof(Creature))
                        return false;
                    if (parameters[4].ParameterType != typeof(decimal))
                        return false;
                    if (parameters[5].ParameterType != typeof(ValueProp))
                        return false;
                    if (parameters[6].ParameterType != typeof(CardModel))
                        return false;

                    // Main: ... cardSource, ModifyDamageHookType, out List<AbstractModel>
                    // Beta: ... cardSource, CardPlay?, ModifyDamageHookType, out List<AbstractModel>
                    int hookTypeParamIndex = parameters.Length == 9 ? 7 : 8;
                    if (
                        parameters[hookTypeParamIndex].ParameterType != typeof(ModifyDamageHookType)
                    )
                        return false;

                    var outModifiersParam = parameters[^1];
                    return outModifiersParam.IsOut
                        && outModifiersParam.ParameterType
                            == typeof(List<AbstractModel>).MakeByRefType();
                }
            );

        public static void Postfix(
            IRunState runState,
            ICombatState? combatState,
            Creature? target,
            Creature? dealer,
            ModifyDamageHookType modifyDamageHookType,
            ref decimal __result,
            ref List<AbstractModel> modifiers
        )
        {
            if (!modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
                return;

            foreach (AbstractModel model in runState.IterateHookListeners(combatState))
            {
                if (model is not EnemyScaling enemyScaling)
                    continue;

                decimal multiplier = enemyScaling.GetEnemyToPlayerDamageMultiplier(target, dealer);
                if (multiplier == 1m)
                    continue;

                __result *= multiplier;
                modifiers.Add(model);
            }
        }
    }
}
