using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Enchanter : ModifierModel
{
    private static readonly Dictionary<Type, int> EnchantmentAmounts = new()
    {
        [typeof(Adroit)] = 3,
        [typeof(Corrupted)] = 1,
        [typeof(Glam)] = 1,
        [typeof(Goopy)] = 1,
        [typeof(Imbued)] = 1,
        [typeof(Inky)] = 1,
        [typeof(Instinct)] = 1,
        [typeof(Momentum)] = 5,
        [typeof(Nimble)] = 2,
        [typeof(PerfectFit)] = 1,
        [typeof(RoyallyApproved)] = 1,
        [typeof(Sharp)] = 2,
        [typeof(Slither)] = 1,
        [typeof(SlumberingEssence)] = 1,
        [typeof(SoulsPower)] = 1,
        [typeof(Sown)] = 1,
        [typeof(Spiral)] = 1,
        [typeof(Steady)] = 1,
        [typeof(Swift)] = 2,
        [typeof(Vigorous)] = 8,
    };

    public override bool TryModifyCardRewardOptionsLate(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions options
    )
    {
        if (!options.Flags.HasFlag(CardCreationFlags.IsCardReward))
        {
            return false;
        }
        var enchantments = ModelDb
            .DebugEnchantments.Where(e => EnchantmentAmounts.ContainsKey(e.GetType()))
            .ToArray();
        var enchanmentIndexes = Enumerable.Range(0, enchantments.Length).ToArray();
        for (int i = enchanmentIndexes.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (enchanmentIndexes[i], enchanmentIndexes[j]) = (
                enchanmentIndexes[j],
                enchanmentIndexes[i]
            );
        }
        int currEnchantmentIndex = 0;
        foreach (CardCreationResult cardReward in cardRewards)
        {
            CardModel card = cardReward.Card;
            for (int i = currEnchantmentIndex; i < enchanmentIndexes.Length; i++)
            {
                var enchantment = enchantments[enchanmentIndexes[i]];
                if (enchantment.CanEnchant(card))
                {
                    CardModel card2 = player.RunState.CloneCard(card);
                    decimal amount = EnchantmentAmounts[enchantment.GetType()];
                    CardCmd.Enchant(enchantment.ToMutable(), card2, amount);
                    cardReward.ModifyCard(card2);
                    currEnchantmentIndex = i + 1;
                    break;
                }
            }
        }
        return true;
    }

    protected override string IconPath => "neows_blessing.png".ModifierImagePath();
}
