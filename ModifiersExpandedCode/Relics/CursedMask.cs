using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

[Pool(typeof(EventRelicPool))]
public class CursedMask : CustomRelicModel
{
    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState
    )
    {
        if (player != Owner || Owner.PlayerCombatState.TurnNumber > 1)
        {
            return;
        }

        List<CardModel> powers = PileType
            .Draw.GetPile(player)
            .Cards.Where(card => card.Type == CardType.Power)
            .ToList();
        if (powers.Count == 0)
        {
            return;
        }

        CardModel power = player.RunState.Rng.CombatCardSelection.NextItem(powers)!;
        Flash();
        power.EnergyCost.AddThisTurnOrUntilPlayed(2);
        power.InvokeEnergyCostChanged();
        await CardPileCmd.Add(power, PileType.Hand);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => nameof(CursedMask).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(CursedMask).ToSnakeCasePng().BigRelicImagePath();
}
