using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Speedrun : ModifierModel
{
    private const float FloorTimeLimit = 45f;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        return Task.CompletedTask;
    }

    public virtual Task AfterCombatEnd(CombatRoom room)
    {
        return Task.CompletedTask;
    }
}
