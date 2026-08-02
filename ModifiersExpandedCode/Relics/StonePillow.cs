using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

[Pool(typeof(EventRelicPool))]
public class StonePillow : CustomRelicModel
{
    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (creature.Player != Owner && creature.PetOwner != Owner)
        {
            return amount;
        }

        return amount * 0.5m;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => nameof(StonePillow).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(StonePillow).ToSnakeCasePng().BigRelicImagePath();
}
