using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class HighRoller : ModifierModel
{
    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () => ObtainRewards(eventModel);
    }

    private static async Task ObtainRewards(EventModel eventModel)
    {
        var player = eventModel.Owner;
        if (player == null)
        {
            return;
        }
        List<CardPileAddResult> results = new List<CardPileAddResult>();
        var debt = ModelDb.Card<Debt>();
        var jackpot = ModelDb.Card<Jackpot>();

        var startingCards = new List<CardModel>()
        {
            player.RunState.CreateCard(debt, player),
            player.RunState.CreateCard(jackpot, player),
        };

        foreach (CardModel card in startingCards)
        {
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results);
        await Cmd.CustomScaledWait(0.6f, 1.2f);
    }

    protected override string IconPath => nameof(HighRoller).ToSnakeCasePng().ModifierImagePath();
}
