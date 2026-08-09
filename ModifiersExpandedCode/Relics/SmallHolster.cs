using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

[Pool(typeof(EventRelicPool))]
public class SmallHolster : AzRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() => PlayerCmd.LoseMaxPotionCount(1, Owner);

    public override RelicRarity Rarity => RelicRarity.Ancient;
}
