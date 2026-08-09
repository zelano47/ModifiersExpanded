using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

[Pool(typeof(EventRelicPool))]
public class CursedHumidifier : AzRelic
{
    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner)
        {
            return;
        }

        Flash();
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            5m,
            isFromCard: false
        );
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
}
