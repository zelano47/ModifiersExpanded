using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.Map;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Sprint : ModifierModel
{
    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        return new SprintMap(runState.Act.HasSecondBoss);
    }

    protected override string IconPath => nameof(Sprint).ToSnakeCasePng().ModifierImagePath();
}
