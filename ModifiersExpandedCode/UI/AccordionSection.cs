using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// A collapsible accordion section that displays a group of modifier tickboxes under a
/// clickable header. Manages header styling based on hover and selection state, and
/// enforces mutual-exclusion rules among tickboxes when they are toggled.
/// </summary>
public class AccordionSection
{
    private readonly ModifierGroup _group;
    private readonly List<NRunModifierTickbox> _tickboxes;
    private readonly PanelContainer _headerPanel;
    private readonly Label _headerLabel;
    private readonly MarginContainer _body;
    private readonly StyleBoxFlat _normalBase;
    private readonly StyleBoxFlat _normalHover;
    private readonly StyleBoxFlat _selectedBase;
    private readonly StyleBoxFlat _selectedHover;
    private bool _isHovering;

    /// <summary>The root container node to add to the scene tree.</summary>
    public VBoxContainer Root { get; }

    /// <summary>
    /// Initialises a new accordion section for the given modifier group.
    /// </summary>
    /// <param name="group">The modifier group whose tickboxes are displayed.</param>
    /// <param name="tickboxes">Ordered list of tickboxes belonging to this group.</param>
    /// <param name="startExpanded">Whether the body is visible on first render.</param>
    public AccordionSection(
        ModifierGroup group,
        List<NRunModifierTickbox> tickboxes,
        bool startExpanded
    )
    {
        _group = group;
        _tickboxes = tickboxes;

        Root = new VBoxContainer();
        Root.AddThemeConstantOverride("separation", 0);

        (_headerPanel, _headerLabel) = HeaderPanelStyles.CreateHeaderPanel();

        _body = new MarginContainer();
        _body.AddThemeConstantOverride("margin_top", 8);
        var vbox = new VBoxContainer();
        _body.AddChild(vbox);
        _body.AddThemeConstantOverride("separation", 0);
        _body.Visible = startExpanded;

        foreach (var tickbox in tickboxes)
            vbox.AddChild((Node)(object)tickbox);

        _normalBase = HeaderPanelStyles.BuildNormalStyle(selected: false);
        _normalHover = HeaderPanelStyles.BuildHoverVariant(_normalBase);
        _selectedBase = HeaderPanelStyles.BuildNormalStyle(selected: true);
        _selectedHover = HeaderPanelStyles.BuildHoverVariant(_selectedBase);

        _headerPanel.MouseEntered += OnMouseEntered;
        _headerPanel.MouseExited += OnMouseExited;
        _headerPanel.GuiInput += OnHeaderGuiInput;

        foreach (var tickbox in tickboxes)
        {
            ((GodotObject)(object)tickbox).Connect(
                NTickbox.SignalName.Toggled,
                Callable.From<NRunModifierTickbox>(OnTickboxToggled),
                0u
            );
        }

        Root.AddChild(_headerPanel);
        Root.AddChild(_body);

        Refresh();
    }

    /// <summary>Refreshes both the header label text and the panel style.</summary>
    public void Refresh()
    {
        ApplyStyle();
        RefreshHeaderText();
    }

    private void ApplyStyle()
    {
        bool hasSelection = _tickboxes.Any(t => t.IsTicked);
        var style = hasSelection
            ? (_isHovering ? _selectedHover : _selectedBase)
            : (_isHovering ? _normalHover : _normalBase);
        _headerPanel.AddThemeStyleboxOverride("panel", style);
    }

    private void RefreshHeaderText()
    {
        string arrow = _body.Visible ? "▼  " : "▶  ";

        if (!_body.Visible)
        {
            var selected = _tickboxes.Where(t => t.IsTicked).ToArray();
            if (selected.Length > 0)
            {
                _headerLabel.Text =
                    arrow + ModifierGroupControls.ModifierGroupCollapsedText(selected);
                return;
            }
        }

        string text =
            _group.GroupName != null
                ? _group.GroupName.GetFormattedText()
                : ModifierGroupControls.ModifierGroupCollapsedText(_tickboxes.ToArray());
        _headerLabel.Text = arrow + text;
    }

    private void OnMouseEntered()
    {
        _isHovering = true;
        ApplyStyle();
    }

    private void OnMouseExited()
    {
        _isHovering = false;
        ApplyStyle();
    }

    private void OnHeaderGuiInput(InputEvent evt)
    {
        if (evt is InputEventMouseButton btn && btn.ButtonIndex == MouseButton.Left && btn.Pressed)
        {
            _body.Visible = !_body.Visible;
            Refresh();
        }
    }

    private void OnTickboxToggled(NRunModifierTickbox toggled)
    {
        Refresh();
        if (_group.IsMutuallyExclusive && toggled.IsTicked)
        {
            foreach (var other in _tickboxes)
                if (other != toggled)
                    other.IsTicked = false;
        }
        else if (
            toggled.Modifier != null
            && toggled.IsTicked
            && _group.MutuallyExclusiveModifiers.Contains(toggled.Modifier.GetType())
        )
        {
            foreach (var other in _tickboxes)
                if (
                    other.Modifier != null
                    && other != toggled
                    && _group.MutuallyExclusiveModifiers.Contains(other.Modifier.GetType())
                )
                    other.IsTicked = false;
        }
    }
}
