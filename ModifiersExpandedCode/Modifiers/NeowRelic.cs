using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class NeowRelic : ModifierModel
{
    // public override LocString Title => new LocString("Neow Relic", base.Id.Entry + ".title");
    // public override LocString Description =>
    //     new LocString("Grants neow relic rewards", base.Id.Entry + ".description");

    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () =>
        {
            ArgumentNullException.ThrowIfNull(eventModel);
            return OfferNeowRelics(eventModel.Owner);
        };
    }

    public async Task OfferNeowRelics(Player player)
    {
        await Task.FromResult(false);
    }

    protected override string IconPath => "NEOW_RELIC.png".ModifierImagePath();
}
