using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

public class RelicSwap : ModifierModel
{
    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () => SwapForRareRelic(eventModel);
    }

    private static async Task SwapForRareRelic(EventModel eventModel)
    {
        Player? player = eventModel.Owner;
        if (player == null)
        {
            return;
        }
        var starterRelicId = player.Relics.FirstOrDefault(r => r.Rarity == RelicRarity.Starter)?.Id;
        if (starterRelicId == null)
        {
            return;
        }
        RelicModel? original = player.GetRelicById(starterRelicId);
        if (original == null)
        {
            return;
        }
        RelicModel replace =
            player.RelicGrabBag.PullFromBack(RelicRarity.Rare, _ => true, player.RunState)
            ?? ModelDb.Relic<IceCream>();

        await RelicCmd.Replace(original, replace.ToMutable());
    }

    protected override string IconPath => nameof(RelicSwap).ToSnakeCasePng().ModifierImagePath();
}
