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
public class EnemyScalingSection : CollapsibleSection
{
    private readonly IReadOnlyList<NRunModifierTickbox> _tickboxes;
    private readonly string _title;

    /// <summary>
    /// Initialises a new enemy scaling section with the given title and tickboxes.
    /// </summary>
    /// <param name="title">Localised header text for the section.</param>
    /// <param name="tickboxes">Enemy-scaling tickboxes driven by the slider.</param>
    public EnemyScalingSection(string title, IReadOnlyList<NRunModifierTickbox> tickboxes)
        : base(startExpanded: false)
    {
        _title = title;
        _tickboxes = tickboxes;

        _body.AddThemeConstantOverride("margin_left", 20);
        _body.AddThemeConstantOverride("margin_right", 20);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        _body.AddChild(vbox);

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

        Refresh();
    }

    /// <inheritdoc/>
    protected override bool IsSelected() => ModifierEnabled();

    /// <summary>Refreshes the header label text and panel style.</summary>
    public override void Refresh()
    {
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
        _headerLabel.Text = Arrow + _title + suffix;
        ApplyStyle();
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

    private void OnPlayersSliderChanged(float value)
    {
        EnemyScalingState.Instance.NumAdditionalPlayers = (int)value;
        bool ticked = EnemyScalingState.Instance.NumAdditionalPlayers > 0;
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

    private bool ModifierEnabled() => PlayersModified() || DamageModified();

    private bool PlayersModified() => EnemyScalingState.Instance.NumAdditionalPlayers != 0;

    private bool DamageModified() => EnemyScalingState.Instance.DamageMultiplier != 1.0f;

    private static string FormatScalingValue(float value) => $"{value:F2}x";
}
