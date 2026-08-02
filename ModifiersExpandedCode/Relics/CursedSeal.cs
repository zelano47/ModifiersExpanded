using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

public class CursedSeal : CustomRelicModel
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained()
    {
        foreach (var card in Owner.Deck.Cards)
        {
            card.AddKeyword(CardKeyword.Eternal);
        }

        return Task.CompletedTask;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => nameof(CursedSeal).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(CursedSeal).ToSnakeCasePng().BigRelicImagePath();
}
