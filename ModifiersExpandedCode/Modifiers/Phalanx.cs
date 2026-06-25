using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Phalanx : ModifierModel
{
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom combatRoom)
            return;

        foreach (Creature creature in combatRoom.CombatState.Enemies)
        {
            await ApplyPowers(creature, GetPlatingAmount(creature));
        }
    }

    // Handles enemies spawned mid-combat (e.g. summons)
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (!creature.IsEnemy)
            return;

        await ApplyPowers(creature, GetPlatingAmount(creature));
    }

    private int GetPlatingAmount(Creature creature)
    {
        int actIndex = creature.CombatState!.RunState.CurrentActIndex;
        return 4 + actIndex * 2;
    }

    private static async Task ApplyPowers(Creature creature, decimal platingAmount)
    {
        await PowerCmd.Apply<PlatingPower>(
            new ThrowingPlayerChoiceContext(),
            creature,
            platingAmount,
            creature,
            null
        );
        await PowerCmd.Apply<BarricadePower>(
            new ThrowingPlayerChoiceContext(),
            creature,
            1m,
            creature,
            null
        );
    }

    protected override string IconPath => nameof(Phalanx).ToSnakeCasePng().ModifierImagePath();
}
