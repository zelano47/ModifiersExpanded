using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace ModifiersExpanded.ModifiersExpandedCode.State;

/// <summary>
/// In-memory store for the modifier selection used on the most recent custom run.
/// </summary>
public static class PreviousRunModifiers
{
    /// <summary>The modifiers from the last custom run that was started.</summary>
    public static IReadOnlyList<ModifierModel>? Modifiers { get; set; }

    /// <summary>Reference to the injected "Last Run" button on the current custom run screen.</summary>
    internal static NButton? LastRunButton { get; set; }

    /// <summary>True while the screen is initialised in multiplayer-client mode.</summary>
    internal static bool IsClientMode { get; set; }
}
