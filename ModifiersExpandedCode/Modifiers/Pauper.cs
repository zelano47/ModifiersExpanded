using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpandedCode.Modifiers;

public class Pauper : ModifierModel
{
    private int _relicCount = 0;
    private int _potionsCount = 0;

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions creationOptions
    )
    {
        int newCount = (int)Math.Ceiling((double)cardRewards.Count / 2);
        cardRewards.RemoveRange(newCount, cardRewards.Count - newCount);
        return true;
    }

    public override bool TryModifyRewardsLate(
        Player player,
        List<Reward> rewards,
        AbstractRoom? room
    )
    {
        if (room is not CombatRoom)
            return false;

        var replacement = new List<Reward>();
        foreach (var reward in rewards)
        {
            if (reward is RelicReward)
            {
                _relicCount++;
                if (_relicCount % 2 != 0)
                    replacement.Add(reward);
            }
            else if (reward is PotionReward)
            {
                _potionsCount++;
                if (_potionsCount % 2 != 0)
                    replacement.Add(reward);
            }
            else if (reward is GoldReward goldReward)
            {
                int halved = (int)Math.Ceiling(goldReward.Amount / 2.0);
                replacement.Add(new GoldReward(halved, player));
            }
            else
            {
                replacement.Add(reward);
            }
        }
        rewards.Clear();
        rewards.AddRange(replacement);
        return true;
    }

    protected override string IconPath => nameof(Pauper).ToSnakeCasePng().ModifierImagePath();
}
