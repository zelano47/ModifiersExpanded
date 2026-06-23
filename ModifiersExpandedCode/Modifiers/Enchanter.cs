using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
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

    private static readonly Dictionary<Type, int> EnchantmentWeights = new()
    {
        [typeof(Adroit)] = 2,
        [typeof(Corrupted)] = 1,
        [typeof(Glam)] = 1,
        [typeof(Goopy)] = 2,
        [typeof(Imbued)] = 1,
        [typeof(Instinct)] = 1,
        [typeof(Momentum)] = 2,
        [typeof(Nimble)] = 3,
        [typeof(PerfectFit)] = 2,
        [typeof(RoyallyApproved)] = 2,
        [typeof(Sharp)] = 3,
        [typeof(Slither)] = 2,
        [typeof(SlumberingEssence)] = 2,
        [typeof(SoulsPower)] = 2,
        [typeof(Sown)] = 2,
        [typeof(Spiral)] = 1,
        [typeof(Steady)] = 2,
        [typeof(Swift)] = 2,
        [typeof(Vigorous)] = 3,
    };

    protected override void AfterRunCreated(RunState runState)
    {
        runState.SharedRelicGrabBag.Remove<Kifuda>();
        runState.SharedRelicGrabBag.Remove<GnarledHammer>();
        runState.SharedRelicGrabBag.Remove<PunchDagger>();
        runState.SharedRelicGrabBag.Remove<RoyalStamp>();
        runState.SharedRelicGrabBag.Remove<WingCharm>();

        /* Events to remove
        * Drowning Beacon (Fresnel Lens)
        */
    }

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
        foreach (CardCreationResult cardReward in cardRewards)
        {
            CardModel card = cardReward.Card;
            var eligible = enchantments.Where(e => e.CanEnchant(card)).ToArray();
            if (eligible.Length == 0)
                continue;

            int totalWeight = eligible.Sum(e =>
                EnchantmentWeights.GetValueOrDefault(e.GetType(), 1)
            );
            int roll = Random.Shared.Next(totalWeight);
            int cumulative = 0;
            foreach (var enchantment in eligible)
            {
                cumulative += EnchantmentWeights.GetValueOrDefault(enchantment.GetType(), 1);
                if (roll < cumulative)
                {
                    CardModel card2 = player.RunState.CloneCard(card);
                    decimal amount = EnchantmentAmounts[enchantment.GetType()];
                    CardCmd.Enchant(enchantment.ToMutable(), card2, amount);
                    cardReward.ModifyCard(card2);
                    break;
                }
            }
        }
        return true;
    }

    protected override string IconPath => "enchanter.png".ModifierImagePath();
}
