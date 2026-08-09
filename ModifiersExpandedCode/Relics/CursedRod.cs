using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

[Pool(typeof(EventRelicPool))]
public class CursedRod : AzRelic
{
    public int CombatsSeen { get; set; }
    public override int DisplayAmount => CombatsSeen % _invokeAfterNumCombats;
    public override bool ShowCounter => true;
    private const int _invokeAfterNumCombats = 5;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (room.Encounter.RoomType != RoomType.Monster)
        {
            return Task.CompletedTask;
        }
        CombatsSeen++;
        InvokeDisplayAmountChanged();
        if (CombatsSeen % _invokeAfterNumCombats == 0)
        {
            Flash();
            IEnumerable<CardModel> items = PileType
                .Deck.GetPile(base.Owner)
                .Cards.Where((CardModel c) => c.IsUpgraded);
            CardModel cardModel = base.Owner.RunState.Rng.Niche.NextItem(items);
            if (cardModel != null)
            {
                CardCmd.Downgrade(cardModel);
            }
        }
        return Task.CompletedTask;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
}
