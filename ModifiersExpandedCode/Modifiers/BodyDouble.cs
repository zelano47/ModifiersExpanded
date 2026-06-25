using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class BodyDouble : ModifierModel
{
    public override bool ClearsPlayerDeck => true;

    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () => ObtainCards(eventModel.Owner, eventModel.Rng);
    }

    private static async Task ObtainCards(Player? player, Rng rng)
    {
        if (player == null)
        {
            return;
        }
        List<CardPileAddResult> results = new List<CardPileAddResult>();
        List<CardModel> startingCards = new List<CardModel>();
        foreach (CardModel card in player.Character.StartingDeck)
        {
            if (card.Type != CardType.Attack)
            {
                var c = player.RunState.CreateCard(card, player);
                startingCards.Add(c);
            }
        }
        var cloneEnchantment = ModelDb.Enchantment<Clone>();
        var bodySlamCard = player.RunState.CreateCard<BodySlam>(player);
        CardCmd.Upgrade(bodySlamCard, CardPreviewStyle.None);
        CardCmd.Enchant(cloneEnchantment.ToMutable(), bodySlamCard, 1);
        startingCards.Add(bodySlamCard);

        foreach (CardModel card in startingCards)
        {
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results);
        await Cmd.CustomScaledWait(0.6f, 1.2f);
    }

    public override bool TryModifyRestSiteOptions(
        Player player,
        ICollection<RestSiteOption> options
    )
    {
        options.Add(new CloneRestSiteOption(player));
        return true;
    }

    public override IEnumerable<CardModel> ModifyMerchantCardPool(
        Player player,
        IEnumerable<CardModel> options
    )
    {
        CardPoolModel cardPool = player.Character.CardPool;
        List<CardModel> cardModels = options.ToList();
        cardModels.RemoveAll((CardModel c) => c.Type == CardType.Attack);
        return cardModels;
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(
        Player player,
        CardCreationOptions options
    )
    {
        Func<CardModel, bool> filter = (CardModel c) => c.Type != CardType.Attack;
        return options.WithCardPools(options.CardPools.ToList(), filter);
    }

    protected override string IconPath => nameof(BodyDouble).ToSnakeCasePng().ModifierImagePath();
}
