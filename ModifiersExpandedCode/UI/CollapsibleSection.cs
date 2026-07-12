using Godot;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// Base class for a collapsible accordion-style section with a clickable header panel.
/// Manages the root container, header panel/label, body container, hover/selection styles,
/// and mouse/input event wiring. Subclasses implement <see cref="IsSelected"/> and
/// <see cref="Refresh"/> to provide section-specific selection state and header text.
/// </summary>
public abstract class CollapsibleSection
{
    protected readonly PanelContainer _headerPanel;
    protected readonly Label _headerLabel;
    protected readonly MarginContainer _body;
    private readonly StyleBoxFlat _normalBase;
    private readonly StyleBoxFlat _normalHover;
    private readonly StyleBoxFlat _selectedBase;
    private readonly StyleBoxFlat _selectedHover;
    private bool _isHovering;

    /// <summary>The root container node to add to the scene tree.</summary>
    public VBoxContainer Root { get; }

    /// <summary>
    /// Initialises the shared scaffold: root, header panel/label, body container,
    /// four style variants, and mouse/input event connections.
    /// Subclasses should populate <see cref="_body"/> before calling <see cref="Refresh"/>.
    /// </summary>
    /// <param name="startExpanded">Whether the body is visible on first render.</param>
    protected CollapsibleSection(bool startExpanded = false)
    {
        Root = new VBoxContainer();
        Root.AddThemeConstantOverride("separation", 0);

        (_headerPanel, _headerLabel) = HeaderPanelStyles.CreateHeaderPanel();

        _body = new MarginContainer();
        _body.AddThemeConstantOverride("margin_top", 8);
        _body.Visible = startExpanded;

        _normalBase = HeaderPanelStyles.BuildNormalStyle(selected: false);
        _normalHover = HeaderPanelStyles.BuildHoverVariant(_normalBase);
        _selectedBase = HeaderPanelStyles.BuildNormalStyle(selected: true);
        _selectedHover = HeaderPanelStyles.BuildHoverVariant(_selectedBase);

        _headerPanel.MouseEntered += OnMouseEntered;
        _headerPanel.MouseExited += OnMouseExited;
        _headerPanel.GuiInput += OnHeaderGuiInput;

        Root.AddChild(_headerPanel);
        Root.AddChild(_body);
    }

    /// <summary>Refreshes the header label text and panel style. Called after any state change.</summary>
    public abstract void Refresh();

    /// <summary>
    /// Returns true when the section should be rendered in the "selected" style
    /// (e.g. at least one tickbox is active, or a modifier value is non-default).
    /// </summary>
    protected abstract bool IsSelected();

    /// <summary>Applies the correct style variant based on hover and selection state.</summary>
    protected void ApplyStyle()
    {
        var style = IsSelected()
            ? (_isHovering ? _selectedHover : _selectedBase)
            : (_isHovering ? _normalHover : _normalBase);
        _headerPanel.AddThemeStyleboxOverride("panel", style);
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

    protected string Arrow => _body.Visible ? "▼  " : "▶  ";
}
