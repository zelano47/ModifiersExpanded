using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

[Pool(typeof(EventRelicPool))]
public class EmptyLargeCapsule : CustomRelicModel
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        List<CardPileAddResult> results =
        [
            await CardPileCmd.Add(
                Owner.RunState.CreateCard(GetBasicCard(Owner.Character, CardTag.Strike), Owner),
                PileType.Deck
            ),
            await CardPileCmd.Add(
                Owner.RunState.CreateCard(GetBasicCard(Owner.Character, CardTag.Defend), Owner),
                PileType.Deck
            ),
        ];
        CardCmd.PreviewCardPileAdd(results, 2f);
    }

    private static CardModel GetBasicCard(CharacterModel character, CardTag tag) =>
        character.CardPool.AllCards.First(card =>
            card.Rarity == CardRarity.Basic && card.Tags.Contains(tag)
        );

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath =>
        nameof(EmptyLargeCapsule).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(EmptyLargeCapsule).ToSnakeCasePng().BigRelicImagePath();
}
