using System.Text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// Display-text helpers for modifier names and collapsed-group labels.
/// </summary>
public static class ModifierGroupControls
{
    /// <summary>
    /// Returns the localised display name of a modifier, or "?" when the modifier is null.
    /// </summary>
    public static string ModifierDisplayName(ModifierModel? modifier)
    {
        if (modifier == null)
            return "?";
        return modifier.Title.GetFormattedText();
    }

    /// <summary>
    /// Returns a comma-joined string of display names for the given tickboxes, used as
    /// the collapsed header text when no group name is available.
    /// </summary>
    public static string ModifierGroupCollapsedText(NRunModifierTickbox[] tickboxes)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(", ", tickboxes.Select(t => ModifierDisplayName(t.Modifier))));
        return sb.ToString();
    }
}
