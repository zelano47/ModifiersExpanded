using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

public class InertEctoplasm : CustomRelicModel
{
    public override decimal ModifyGoldGained(Player player, decimal amount) =>
        player == Owner ? 0m : amount;

    public override Task AfterModifyingGoldGained(Player player, decimal amount)
    {
        if (player == Owner)
        {
            Flash();
        }

        return Task.CompletedTask;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override string PackedIconPath =>
        nameof(InertEctoplasm).ToSnakeCasePng().RelicImagePath();
    protected override string BigIconPath =>
        nameof(InertEctoplasm).ToSnakeCasePng().BigRelicImagePath();
}
