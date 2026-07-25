using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// A collapsible accordion section that displays a group of modifier tickboxes under a
/// clickable header. Enforces mutual-exclusion rules among tickboxes when they are toggled.
/// </summary>
public class ModifierCheckboxSection : CollapsibleSection
{
    private readonly ModifierGroup _group;
    private readonly List<NRunModifierTickbox> _tickboxes;

    /// <summary>
    /// Initialises a new accordion section for the given modifier group.
    /// </summary>
    /// <param name="group">The modifier group whose tickboxes are displayed.</param>
    /// <param name="tickboxes">Ordered list of tickboxes belonging to this group.</param>
    /// <param name="startExpanded">Whether the body is visible on first render.</param>
    public ModifierCheckboxSection(
        ModifierGroup group,
        List<NRunModifierTickbox> tickboxes,
        bool startExpanded
    )
        : base(startExpanded)
    {
        _group = group;
        _tickboxes = tickboxes;

        var vbox = new VBoxContainer();
        _body.AddChild(vbox);
        _body.AddThemeConstantOverride("separation", 0);

        foreach (var tickbox in tickboxes)
            vbox.AddChild((Node)(object)tickbox);

        foreach (var tickbox in tickboxes)
        {
            ((GodotObject)(object)tickbox).Connect(
                NTickbox.SignalName.Toggled,
                Callable.From<NRunModifierTickbox>(OnTickboxToggled),
                0u
            );
        }

        Refresh();
    }

    /// <inheritdoc/>
    protected override bool IsSelected() => _tickboxes.Any(t => t.IsTicked);

    /// <summary>Refreshes both the header label text and the panel style.</summary>
    public override void Refresh()
    {
        ApplyStyle();
        RefreshHeaderText();
    }

    private void RefreshHeaderText()
    {
        string groupName =
            _group.GroupName != null
                ? _group.GroupName.GetFormattedText()
                : ModifierGroupControls.ModifierGroupCollapsedText(_tickboxes.ToArray());

        string suffix = string.Empty;
        if (!_body.Visible)
        {
            var selected = _tickboxes.Where(t => t.IsTicked).ToArray();
            if (selected.Length > 0)
                suffix = $": {ModifierGroupControls.ModifierGroupCollapsedText(selected)}";
        }

        _headerLabel.Text = Arrow + groupName + suffix;
    }

    private void OnTickboxToggled(NRunModifierTickbox toggled)
    {
        Refresh();
        if (_group.IsMutuallyExclusive && toggled.IsTicked)
        {
            foreach (var other in _tickboxes)
            {
                if (other != toggled)
                    UntickAndEmit(other);
            }
        }
        else if (
            toggled.Modifier != null
            && toggled.IsTicked
            && _group.MutuallyExclusiveModifiers.Contains(toggled.Modifier.GetType())
        )
        {
            foreach (var other in _tickboxes)
            {
                if (
                    other.Modifier != null
                    && other != toggled
                    && _group.MutuallyExclusiveModifiers.Contains(other.Modifier.GetType())
                )
                    UntickAndEmit(other);
            }
        }
    }

    // Programmatic IsTicked updates do not fire NTickbox.Toggled, so emit explicitly
    // to keep NCustomRunModifiersList's selected-modifier state synchronized.
    private static void UntickAndEmit(NRunModifierTickbox tickbox)
    {
        if (!tickbox.IsTicked)
            return;

        tickbox.IsTicked = false;
        ((GodotObject)(object)tickbox).EmitSignal(
            NTickbox.SignalName.Toggled,
            new Variant[] { Variant.From<GodotObject>((GodotObject)(object)tickbox) }
        );
    }
}
