using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Polymath : ModifierModel
{
    protected override void AfterRunCreated(RunState runState)
    {
        foreach (Player player in runState.Players)
        {
            if (player.Character is not Ironclad)
            {
                AddRelicToPlayer(player, ModelDb.Relic<BurningBlood>());
            }

            if (player.Character is not Silent)
            {
                AddRelicToPlayer(player, ModelDb.Relic<RingOfTheSnake>());
            }

            if (player.Character is not Defect)
            {
                AddRelicToPlayer(player, ModelDb.Relic<CrackedCore>());
            }

            if (player.Character is not Necrobinder)
            {
                AddRelicToPlayer(player, ModelDb.Relic<BoundPhylactery>());
            }

            if (player.Character is not Regent)
            {
                AddRelicToPlayer(player, ModelDb.Relic<DivineRight>());
            }
        }
        runState.SharedRelicGrabBag.Remove<BurningBlood>();
        runState.SharedRelicGrabBag.Remove<RingOfTheSnake>();
        runState.SharedRelicGrabBag.Remove<CrackedCore>();
        runState.SharedRelicGrabBag.Remove<BoundPhylactery>();
        runState.SharedRelicGrabBag.Remove<DivineRight>();
    }

    private void AddRelicToPlayer(Player player, RelicModel relicModel)
    {
        var mutableRelic = relicModel.ToMutable();
        player.AddRelicInternal(mutableRelic);
        player.RelicGrabBag.Remove(relicModel);
    }

    protected override string IconPath => nameof(Polymath).ToSnakeCasePng().ModifierImagePath();
}
