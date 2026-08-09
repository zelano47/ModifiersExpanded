using BaseLib.Abstracts;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Relics;

public abstract class AzRelic : CustomRelicModel
{
    public override string PackedIconPath => GetType().Name.ToSnakeCasePng().RelicImagePath();

    protected override string PackedIconOutlinePath =>
        $"{GetType().Name}_outline".ToSnakeCasePng().RelicImagePath();

    protected override string BigIconPath => GetType().Name.ToSnakeCasePng().BigRelicImagePath();
}
