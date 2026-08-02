using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

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
