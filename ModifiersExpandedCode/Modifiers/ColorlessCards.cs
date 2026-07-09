using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class ColorlessCards : ModifierModel
{
    protected override void AfterRunCreated(RunState runState)
    {
        foreach (Player player in runState.Players)
        {
            player.RelicGrabBag.Remove<DingyRug>();
        }
        runState.SharedRelicGrabBag.Remove<DingyRug>();
    }

    public override IEnumerable<CardModel> ModifyMerchantCardPool(
        Player player,
        IEnumerable<CardModel> options
    )
    {
        CardPoolModel cardPool = player.Character.CardPool;
        List<CardModel> cardModels = options.ToList();
        if (!cardModels.Any(c => c.Pool == cardPool))
        {
            return cardModels;
        }
        cardModels.AddRange(
            ModelDb
                .CardPool<ColorlessCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
        );
        return cardModels;
    }

    public override CardCreationOptions ModifyCardRewardCreationOptions(
        Player player,
        CardCreationOptions options
    )
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
        {
            return options;
        }
        if (!options.Flags.HasFlag(CardCreationFlags.IsCardReward))
        {
            return options;
        }
        if (options.CardPools.Contains(ModelDb.CardPool<ColorlessCardPool>()))
        {
            return options;
        }

        // sts2-main: CharacterCards converts the reward pool to a flat CustomCardPool via
        // WithCustomPool(), which clears CardPools. Detect this and extend the flat list
        // directly rather than adding a pool to the now-empty CardPools list.
        var customPool = options.GetCustomCardPool();
        if (customPool != null)
        {
            var colorlessCards = ModelDb
                .CardPool<ColorlessCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
            return options.WithExtendedCustomPool(colorlessCards);
        }

        return options.WithCardPoolsAndFilter(
            options
                .CardPools.ToList()
                .Concat(new List<CardPoolModel>() { ModelDb.CardPool<ColorlessCardPool>() }),
            options.CardPoolFilter
        );
    }

    protected override string IconPath =>
        nameof(ColorlessCards).ToSnakeCasePng().ModifierImagePath();
}
