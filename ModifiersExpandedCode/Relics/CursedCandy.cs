using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

public class CursedCandy : CustomRelicModel
{
    public int CombatsSeen { get; set; }

    public override int DisplayAmount => CombatsSeen % _invokeAfterNumCombats;
    public override bool ShowCounter => true;
    private const int _invokeAfterNumCombats = 5;
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (room.Encounter.RoomType != RoomType.Monster)
        {
            return Task.CompletedTask;
        }
        CombatsSeen++;
        return Task.CompletedTask;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions creationOptions
    )
    {
        if (
            Owner != player
            || creationOptions.Source != CardCreationSource.Encounter
            || CombatsSeen == 0
            || CombatsSeen % _invokeAfterNumCombats != 0
        )
        {
            return false;
        }
        Flash();
        var availableCurses = ModelDb
            .CardPool<CurseCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.CanBeGeneratedByModifiers)
            .ToList();
        if (availableCurses.Count < cardRewards.Count)
            return false;

        foreach (CardCreationResult cardReward in cardRewards)
        {
            CardModel curse = player.RunState.Rng.Niche.NextItem(availableCurses)!;
            availableCurses.Remove(curse);
            cardReward.ModifyCard(player.RunState.CreateCard(curse, player), this);
        }

        return true;
    }

    public override string PackedIconPath => nameof(CursedCandy).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(CursedCandy).ToSnakeCasePng().BigRelicImagePath();
}
