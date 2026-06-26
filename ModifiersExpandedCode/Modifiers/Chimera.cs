using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Random;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Chimera : ModifierModel
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
        var startingCards = new List<CardModel>();
        //clad
        var cladDefend = ModelDb.Card<DefendIronclad>();
        var cladStrike = ModelDb.Card<StrikeIronclad>();
        var bash = ModelDb.Card<Bash>();

        //silent
        var silentDefend = ModelDb.Card<DefendSilent>();
        var silentStrike = ModelDb.Card<StrikeSilent>();
        var neutralize = ModelDb.Card<Neutralize>();
        var survivor = ModelDb.Card<Survivor>();

        //defect
        var defectDefend = ModelDb.Card<DefendDefect>();
        var defectStrike = ModelDb.Card<StrikeDefect>();
        var zap = ModelDb.Card<Zap>();
        var dualcast = ModelDb.Card<Dualcast>();

        //regent
        var regentDefend = ModelDb.Card<DefendRegent>();
        var regentStrike = ModelDb.Card<StrikeRegent>();
        var venerate = ModelDb.Card<Venerate>();
        var fallingStar = ModelDb.Card<FallingStar>();

        //necrobinder
        var necroDefend = ModelDb.Card<DefendNecrobinder>();
        var necroStrike = ModelDb.Card<StrikeNecrobinder>();
        var bodyguard = ModelDb.Card<Bodyguard>();
        var unleash = ModelDb.Card<Unleash>();

        // strike
        startingCards.Add(player.RunState.CreateCard(cladStrike, player));
        startingCards.Add(player.RunState.CreateCard(silentStrike, player));
        startingCards.Add(player.RunState.CreateCard(defectStrike, player));
        startingCards.Add(player.RunState.CreateCard(regentStrike, player));
        startingCards.Add(player.RunState.CreateCard(necroStrike, player));

        // defend
        startingCards.Add(player.RunState.CreateCard(cladDefend, player));
        startingCards.Add(player.RunState.CreateCard(silentDefend, player));
        startingCards.Add(player.RunState.CreateCard(defectDefend, player));
        startingCards.Add(player.RunState.CreateCard(regentDefend, player));
        startingCards.Add(player.RunState.CreateCard(necroDefend, player));

        // attack
        startingCards.Add(player.RunState.CreateCard(bash, player));
        startingCards.Add(player.RunState.CreateCard(fallingStar, player));
        startingCards.Add(player.RunState.CreateCard(unleash, player));

        // skills
        startingCards.Add(player.RunState.CreateCard(neutralize, player));
        startingCards.Add(player.RunState.CreateCard(survivor, player));
        startingCards.Add(player.RunState.CreateCard(zap, player));
        startingCards.Add(player.RunState.CreateCard(dualcast, player));
        startingCards.Add(player.RunState.CreateCard(venerate, player));
        startingCards.Add(player.RunState.CreateCard(bodyguard, player));

        foreach (CardModel card in startingCards)
        {
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results);
        await Cmd.CustomScaledWait(0.6f, 1.2f);
    }

    protected override string IconPath => nameof(Chimera).ToSnakeCasePng().ModifierImagePath();
}
