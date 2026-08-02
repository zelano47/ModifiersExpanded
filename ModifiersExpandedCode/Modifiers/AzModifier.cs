using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public abstract class AzModifier : ModifierModel, ICustomModel
{
    protected override string IconPath => GetType().Name.ToSnakeCasePng().ModifierImagePath();
}
