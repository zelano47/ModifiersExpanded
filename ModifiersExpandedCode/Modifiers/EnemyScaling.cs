using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.State;
using ModifiersExpanded.ModifiersExpandedCode.Utils;

public class EnemyScaling : ModifierModel
{
    EnemyScalingState State { get; } = EnemyScalingState.Instance;

    public decimal GetEnemyToPlayerDamageMultiplier(Creature? target, Creature? dealer)
    {
        if (dealer != null && dealer.IsEnemy && target != null && target.IsPlayer)
        {
            decimal baseMultiplier = (decimal)State.DamageMultiplier;
            if (baseMultiplier == 1m)
                return 1m;

            if (!EncounterPoolUtils.IsEasyPoolEncounter(dealer.CombatState?.Encounter))
                return baseMultiplier;

            decimal easyPoolScalingRatio = (decimal)Math.Clamp(
                State.EasyPoolScalingPercent / 100.0f,
                0.0f,
                1.0f
            );

            // Interpolate from 1.0 to the configured multiplier for easy-pool encounters.
            // Example: base 2.0x with 50% easy-pool scaling => 1.5x.
            return 1m + (baseMultiplier - 1m) * easyPoolScalingRatio;
        }
        return 1m;
    }

    // Scale initial enemies when entering a combat room.
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
            return Task.CompletedTask;

        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            if (State.NumAdditionalPlayers >= 0)
            {
                creature.ScaleMonsterHpForMultiplayer(
                    combatRoom.CombatState.Encounter,
                    GetNumPlayers(combatRoom.CombatState),
                    combatRoom.CombatState.RunState.CurrentActIndex
                );
            }
        }

        return Task.CompletedTask;
    }

    // Scale enemies spawned mid-combat (e.g. summons).
    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!creature.IsEnemy || creature.CombatState == null)
            return Task.CompletedTask;

        creature.ScaleMonsterHpForMultiplayer(
            creature.CombatState?.Encounter,
            GetNumPlayers(creature.CombatState!),
            creature.CombatState!.RunState.CurrentActIndex
        );

        return Task.CompletedTask;
    }

    // Scale enemy powers that use multiplayer scaling (e.g. Plating, Regen, Artifact).
    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount
    )
    {
        if (
            !canonicalPower.ShouldScaleInMultiplayer
            || amount <= 0
            || target == null
            || (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
        )
        {
            modifiedAmount = amount;
            return false;
        }

        ICombatState? combatState = target.CombatState;
        if (combatState == null)
        {
            modifiedAmount = amount;
            return false;
        }

        modifiedAmount = ScaleAmountForAdditionalPlayers(
            canonicalPower,
            combatState,
            amount,
            GetNumPlayers(combatState)
        );
        return true;
    }

    // Replicates each power's GetScaledAmountForMultiplayer formula using the specified number of players.
    private static decimal ScaleAmountForAdditionalPlayers(
        PowerModel power,
        ICombatState combatState,
        decimal amount,
        int numPlayers
    )
    {
        if (power is PlatingPower or BufferPower)
            return (numPlayers - 1) * 2m * amount + amount; // ((numPlayers-1)*2+1) * amount

        if (power is ArtifactPower)
            return amount + (numPlayers - 1); // amount + playerCount - 1

        if (power is SlipperyPower)
            return numPlayers * amount; // amount * playerCount

        if (power is SkittishPower)
            return amount * (1m + (numPlayers - 1) * 0.5m); // amount * (1 + (playerCount-1)*0.5)

        // Default base PowerModel formula: amount * playerCount * GetMultiplayerScaling
        return amount
            * numPlayers
            * MultiplayerScalingModel.GetMultiplayerScaling(
                combatState.Encounter,
                combatState.RunState.CurrentActIndex
            );
    }

    private int GetNumPlayers(ICombatState combatState) =>
        combatState.Players.Count + State.NumAdditionalPlayers;

    protected override string IconPath => this.GetType().Name.ToSnakeCasePng().ModifierImagePath();
}
