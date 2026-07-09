using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Polymath : ModifierModel
{
    public override Func<Task> GenerateNeowOption(EventModel eventModel)
    {
        return () => ObtainRelics(eventModel);
    }

    private static async Task ObtainRelics(EventModel eventModel)
    {
        var player = eventModel.Owner;
        if (player == null)
            return;
        if (player.Character is not Ironclad)
        {
            await RelicCmd.Obtain(ModelDb.Relic<BurningBlood>().ToMutable(), player);
        }

        if (player.Character is not Silent)
        {
            await RelicCmd.Obtain(ModelDb.Relic<RingOfTheSnake>().ToMutable(), player);
        }

        if (player.Character is not Defect)
        {
            await RelicCmd.Obtain(ModelDb.Relic<CrackedCore>().ToMutable(), player);
        }

        if (player.Character is not Necrobinder)
        {
            await RelicCmd.Obtain(ModelDb.Relic<BoundPhylactery>().ToMutable(), player);
        }

        if (player.Character is not Regent)
        {
            await RelicCmd.Obtain(ModelDb.Relic<DivineRight>().ToMutable(), player);
        }
    }

    protected override string IconPath => nameof(Polymath).ToSnakeCasePng().ModifierImagePath();
}
