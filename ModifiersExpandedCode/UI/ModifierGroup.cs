using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

public class ModifierGroup
{
    public ModifierGroup(bool isMutuallyExclusive = true)
    {
        Modifiers = new HashSet<ModifierModel>();
        IsMutuallyExclusive = isMutuallyExclusive;
    }

    public ModifierGroup(LocString groupName, bool isMutuallyExclusive = true)
    {
        Modifiers = new HashSet<ModifierModel>();
        GroupName = groupName;
        IsMutuallyExclusive = isMutuallyExclusive;
    }

    public HashSet<ModifierModel> Modifiers { get; }
    public LocString? GroupName { get; set; }

    /// <summary>
    /// When true, ticking one modifier in this group automatically unticks all others.
    /// Enforced directly from the group's own tickbox connections so the mod no longer
    /// depends on <see cref="ModelDb.MutuallyExclusiveModifiers"/> at display time.
    /// </summary>
    public bool IsMutuallyExclusive { get; init; }
};
