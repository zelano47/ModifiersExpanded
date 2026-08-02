using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

public class SmallHolster : CustomRelicModel
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() => PlayerCmd.LoseMaxPotionCount(1, Owner);

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath => nameof(SmallHolster).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(SmallHolster).ToSnakeCasePng().BigRelicImagePath();
}
