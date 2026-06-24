using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;
using ModifiersExpanded.ModifiersExpandedCode.Map;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Marathon : ModifierModel
{
    public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
    {
        bool isMultiplayer = runState.Players.Count > 1;
        bool replaceTreasureWithElites =
            map is StandardActMap sam && sam.ShouldReplaceTreasureWithElites;
        var mapRng = new Rng(runState.Rng.Seed, $"act_{actIndex + 1}_map");
        return new MarathonActMap(
            mapRng,
            runState.Act,
            isMultiplayer,
            replaceTreasureWithElites,
            runState.Act.HasSecondBoss
        );
    }

    protected override string IconPath => nameof(Marathon).ToSnakeCasePng().ModifierImagePath();
}
