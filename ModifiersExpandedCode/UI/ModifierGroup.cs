using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

public class ModifierGroup
{
    public ModifierGroup(bool isMutuallyExclusive = true)
    {
        Modifiers = new HashSet<ModifierModel>();
        IsMutuallyExclusive = isMutuallyExclusive;
        MutuallyExclusiveModifiers = new HashSet<Type>();
    }

    public ModifierGroup(LocString groupName, bool isMutuallyExclusive = true)
    {
        Modifiers = new HashSet<ModifierModel>();
        GroupName = groupName;
        IsMutuallyExclusive = isMutuallyExclusive;
        MutuallyExclusiveModifiers = new HashSet<Type>();
    }

    public HashSet<ModifierModel> Modifiers { get; }
    public LocString? GroupName { get; set; }
    public HashSet<Type> MutuallyExclusiveModifiers { get; }

    /// <summary>
    /// When true, ticking one modifier in this group automatically unticks all others.
    /// Enforced directly from the group's own tickbox connections so the mod no longer
    /// depends on <see cref="ModelDb.MutuallyExclusiveModifiers"/> at display time.
    /// Use MutuallyExclusiveModifiers to specify which modifier types are mutually exclusive within this group.
    /// </summary>
    public bool IsMutuallyExclusive { get; init; }
};
