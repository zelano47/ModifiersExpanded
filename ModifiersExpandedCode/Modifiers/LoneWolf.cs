using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Rooms;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpandedCode.Modifiers;

public class LoneWolf : ModifierModel
{
    // Scale initial enemies when entering a combat room.
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
            return Task.CompletedTask;

        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            creature.ScaleMonsterHpForMultiplayer(
                combatRoom.CombatState.Encounter,
                2,
                combatRoom.CombatState.RunState.CurrentActIndex
            );
        }

        return Task.CompletedTask;
    }

    // Scale enemies spawned mid-combat (e.g. summons).
    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!creature.IsEnemy)
            return Task.CompletedTask;

        creature.ScaleMonsterHpForMultiplayer(
            creature.CombatState?.Encounter,
            2,
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
            || target == null
            || (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy)
        )
        {
            modifiedAmount = amount;
            return false;
        }

        ICombatState? combatState = target.CombatState;
        if (combatState == null || combatState.Players.Count != 1)
        {
            modifiedAmount = amount;
            return false;
        }

        modifiedAmount = ScaleAmountForTwoPlayers(canonicalPower, combatState, amount);
        return true;
    }

    // Replicates each power's GetScaledAmountForMultiplayer formula using playerCount = 2.
    private static decimal ScaleAmountForTwoPlayers(
        PowerModel power,
        ICombatState combatState,
        decimal amount
    )
    {
        if (power is PlatingPower or BufferPower)
            return 3m * amount; // ((2-1)*2+1) = 3

        if (power is ArtifactPower)
            return amount + 1m; // amount + playerCount - 1 = amount + 1

        if (power is SlipperyPower)
            return 2m * amount; // amount * playerCount

        if (power is SkittishPower)
            return amount * 1.5m; // amount * (1 + (playerCount-1)*0.5)

        // Default base PowerModel formula: amount * playerCount * GetMultiplayerScaling
        return amount
            * 2m
            * MultiplayerScalingModel.GetMultiplayerScaling(
                combatState.Encounter,
                combatState.RunState.CurrentActIndex
            );
    }

    protected override string IconPath => nameof(LoneWolf).ToSnakeCasePng().ModifierImagePath();
}
