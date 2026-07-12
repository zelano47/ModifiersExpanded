using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using ModifiersExpanded.ModifiersExpandedCode.State;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// A collapsible accordion section that exposes a damage-scaling slider and drives the
/// enemy-scaling modifier tickboxes based on the slider's value. When collapsed, the
/// header shows the current scaling multiplier as a suffix.
/// </summary>
public class EnemyScalingSection
{
    private readonly IReadOnlyList<NRunModifierTickbox> _tickboxes;
    private readonly PanelContainer _headerPanel;
    private readonly Label _headerLabel;
    private readonly MarginContainer _body;
    private readonly string _title;
    private readonly StyleBoxFlat _normalBase;
    private readonly StyleBoxFlat _normalHover;
    private readonly StyleBoxFlat _selectedBase;
    private readonly StyleBoxFlat _selectedHover;
    private bool _isHovering;

    /// <summary>The root container node to add to the scene tree.</summary>
    public VBoxContainer Root { get; }

    /// <summary>
    /// Initialises a new enemy scaling section with the given title and tickboxes.
    /// </summary>
    /// <param name="title">Localised header text for the section.</param>
    /// <param name="tickboxes">Enemy-scaling tickboxes driven by the slider.</param>
    public EnemyScalingSection(string title, IReadOnlyList<NRunModifierTickbox> tickboxes)
    {
        _title = title;
        _tickboxes = tickboxes;

        Root = new VBoxContainer();
        Root.AddThemeConstantOverride("separation", 0);

        (_headerPanel, _headerLabel) = HeaderPanelStyles.CreateHeaderPanel();

        _body = new MarginContainer();
        _body.AddThemeConstantOverride("margin_top", 8);
        _body.AddThemeConstantOverride("margin_left", 20);
        _body.AddThemeConstantOverride("margin_right", 20);
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        _body.AddChild(vbox);
        _body.Visible = false;

        var damageSlider = new ScalingSlider(
            label: new LocString(
                "main_menu_ui",
                "MODIFIER_GROUP.ENEMY_SCALING.damage"
            ).GetFormattedText(),
            initialValue: EnemyScalingState.Instance.DamageMultiplier,
            onValueChanged: OnDamageSliderChanged
        );

        var playersSlider = new ScalingSlider(
            label: new LocString(
                "main_menu_ui",
                "MODIFIER_GROUP.ENEMY_SCALING.num_players"
            ).GetFormattedText(),
            initialValue: EnemyScalingState.Instance.NumAdditionalPlayers,
            onValueChanged: OnPlayersSliderChanged,
            minValue: 0f,
            maxValue: 4f,
            step: 1f,
            formatter: v => $"+{(int)v}"
        );

        vbox.AddChild(damageSlider);
        vbox.AddChild(playersSlider);

        _normalBase = HeaderPanelStyles.BuildNormalStyle(selected: false);
        _normalHover = HeaderPanelStyles.BuildHoverVariant(_normalBase);
        _selectedBase = HeaderPanelStyles.BuildNormalStyle(selected: true);
        _selectedHover = HeaderPanelStyles.BuildHoverVariant(_selectedBase);

        _headerPanel.MouseEntered += OnMouseEntered;
        _headerPanel.MouseExited += OnMouseExited;
        _headerPanel.GuiInput += OnHeaderGuiInput;

        Root.AddChild(_headerPanel);
        Root.AddChild(_body);

        Refresh();
    }

    /// <summary>Refreshes the header label text and panel style.</summary>
    public void Refresh()
    {
        string arrow = _body.Visible ? "▼  " : "▶  ";
        string suffix = String.Empty;
        if (!_body.Visible && ModifierEnabled())
        {
            StringBuilder sb = new StringBuilder();
            if (DamageModified())
            {
                sb.Append(
                    $"Damage {FormatScalingValue(EnemyScalingState.Instance.DamageMultiplier)}"
                );
            }

            if (PlayersModified())
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append($"Players +{EnemyScalingState.Instance.NumAdditionalPlayers}");
            }
            suffix = $": {sb}";
        }
        _headerLabel.Text = arrow + _title + suffix;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        var style = ModifierEnabled()
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

    private void OnDamageSliderChanged(float value)
    {
        EnemyScalingState.Instance.DamageMultiplier = value;
        bool ticked = EnemyScalingState.Instance.DamageMultiplier > 1f;
        foreach (var tb in _tickboxes)
        {
            tb.IsTicked = ticked;
            // IsTicked setter does not emit Toggled; emit it manually so
            // AfterModifiersChanged → ModifiersChanged fires and the run
            // start button picks up the updated modifier list.
            ((GodotObject)(object)tb).EmitSignal(
                NTickbox.SignalName.Toggled,
                new Variant[] { Variant.From<GodotObject>((GodotObject)(object)tb) }
            );
        }
        Refresh();
    }

    private bool ModifierEnabled()
    {
        return PlayersModified() || DamageModified();
    }

    private bool PlayersModified()
    {
        return EnemyScalingState.Instance.NumAdditionalPlayers != 0;
    }

    private bool DamageModified()
    {
        return EnemyScalingState.Instance.DamageMultiplier != 1.0f;
    }

    private void OnPlayersSliderChanged(float value)
    {
        EnemyScalingState.Instance.NumAdditionalPlayers = (int)value;
        Refresh();
    }

    private static string FormatScalingValue(float value) => $"{value:F2}x";
}
