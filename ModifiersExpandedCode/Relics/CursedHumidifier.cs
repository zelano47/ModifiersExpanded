using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

public class CursedHumidifier : CustomRelicModel
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
    public override string PackedIconPath =>
        nameof(CursedHumidifier).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(CursedHumidifier).ToSnakeCasePng().BigRelicImagePath();
}
