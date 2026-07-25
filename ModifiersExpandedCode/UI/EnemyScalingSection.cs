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
    private readonly ScalingSlider? _damageSlider;
    private readonly ScalingSlider? _hpSlider;
    private readonly ScalingSlider? _playersSlider;
    private readonly ScalingSlider? _easyPoolSlider;

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

        _damageSlider = new ScalingSlider(
            label: new LocString(
                "main_menu_ui",
                "MODIFIER_GROUP.ENEMY_SCALING.damage"
            ).GetFormattedText(),
            initialValue: EnemyScalingState.Instance.DamageMultiplier,
            onValueChanged: OnDamageSliderChanged,
            formatter: v => FormatScalingValue(v)
        );

        _hpSlider = new ScalingSlider(
            label: new LocString(
                "main_menu_ui",
                "MODIFIER_GROUP.ENEMY_SCALING.hp"
            ).GetFormattedText(),
            initialValue: EnemyScalingState.Instance.HpMultiplier,
            onValueChanged: OnHpSliderChanged,
            formatter: v => FormatScalingValue(v),
            maxValue: 4f
        );

        _playersSlider = new ScalingSlider(
            label: new LocString(
                "main_menu_ui",
                "MODIFIER_GROUP.ENEMY_SCALING.num_players"
            ).GetFormattedText(),
            initialValue: EnemyScalingState.Instance.NumAdditionalPlayers,
            onValueChanged: OnPlayersSliderChanged,
            formatter: v => $"+{(int)v}",
            minValue: 0f,
            maxValue: 4f,
            step: 1f
        );

        _easyPoolSlider = new ScalingSlider(
            label: new LocString(
                "main_menu_ui",
                "MODIFIER_GROUP.ENEMY_SCALING.easy_pool_scale"
            ).GetFormattedText(),
            initialValue: EnemyScalingState.Instance.EasyPoolScalingPercent,
            onValueChanged: OnEasyPoolSliderChanged,
            formatter: v => $"{v:F0}%",
            minValue: 0f,
            maxValue: 100f,
            step: 5f
        );

        vbox.AddChild(_damageSlider);
        vbox.AddChild(_hpSlider);
        vbox.AddChild(_easyPoolSlider);
        vbox.AddChild(_playersSlider);

        Refresh();
    }

    /// <inheritdoc/>
    protected override bool IsSelected() => ModifierEnabled();

    /// <summary>Refreshes the header label text and panel style.</summary>
    public override void Refresh()
    {
        string suffix = string.Empty;
        if (!_body.Visible && ModifierEnabled())
        {
            StringBuilder sb = new StringBuilder();
            if (DamageModified())
            {
                string dmgShort = new LocString(
                    "main_menu_ui",
                    "MODIFIER_GROUP.ENEMY_SCALING.damage_short"
                ).GetFormattedText();
                sb.Append(
                    $"{dmgShort} {FormatScalingValue(EnemyScalingState.Instance.DamageMultiplier)}"
                );
            }

            if (HpModified())
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                string hpShort = new LocString(
                    "main_menu_ui",
                    "MODIFIER_GROUP.ENEMY_SCALING.hp_short"
                ).GetFormattedText();
                sb.Append(
                    $"{hpShort} {FormatScalingValue(EnemyScalingState.Instance.HpMultiplier)}"
                );
            }

            if (EasyPoolScalingModified())
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                string easyPoolShort = new LocString(
                    "main_menu_ui",
                    "MODIFIER_GROUP.ENEMY_SCALING.easy_pool_scale_short"
                ).GetFormattedText();
                sb.Append(
                    $"{easyPoolShort} {EnemyScalingState.Instance.EasyPoolScalingPercent:F0}%"
                );
            }

            if (PlayersModified())
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }
                string playersShort = new LocString(
                    "main_menu_ui",
                    "MODIFIER_GROUP.ENEMY_SCALING.num_players_short"
                ).GetFormattedText();
                sb.Append($"{playersShort} +{EnemyScalingState.Instance.NumAdditionalPlayers}");
            }
            suffix = $": {sb}";
        }
        _headerLabel.Text = Arrow + _title + suffix;
        MainFile.Logger.Info(
            MainFile.CreateLogMessage(
                $"EnemyScalingSection refreshed: DamageMultiplier={EnemyScalingState.Instance.DamageMultiplier}, HpMultiplier={EnemyScalingState.Instance.HpMultiplier}, NumAdditionalPlayers={EnemyScalingState.Instance.NumAdditionalPlayers}, EasyPoolScalingPercent={EnemyScalingState.Instance.EasyPoolScalingPercent}"
            )
        );
        _damageSlider!.Slider.Value = EnemyScalingState.Instance.DamageMultiplier;
        _hpSlider!.Slider.Value = EnemyScalingState.Instance.HpMultiplier;
        _playersSlider!.Slider.Value = EnemyScalingState.Instance.NumAdditionalPlayers;
        _easyPoolSlider!.Slider.Value = EnemyScalingState.Instance.EasyPoolScalingPercent;
        ApplyStyle();
    }

    private void OnDamageSliderChanged(float value)
    {
        EnemyScalingState.Instance.DamageMultiplier = value;
        UpdateTickboxesFromState();
        Refresh();
    }

    private void OnPlayersSliderChanged(float value)
    {
        EnemyScalingState.Instance.NumAdditionalPlayers = (int)value;
        UpdateTickboxesFromState();
        Refresh();
    }

    private void OnHpSliderChanged(float value)
    {
        EnemyScalingState.Instance.HpMultiplier = value;
        UpdateTickboxesFromState();
        Refresh();
    }

    private void OnEasyPoolSliderChanged(float value)
    {
        EnemyScalingState.Instance.EasyPoolScalingPercent = value;
        UpdateTickboxesFromState();
        Refresh();
    }

    private void UpdateTickboxesFromState()
    {
        bool ticked = ModifierEnabled();
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
    }

    private bool ModifierEnabled() => PlayersModified() || DamageModified() || HpModified();

    private bool PlayersModified() => EnemyScalingState.Instance.NumAdditionalPlayers != 0;

    private bool DamageModified() => EnemyScalingState.Instance.DamageMultiplier != 1.0f;

    private bool HpModified() => EnemyScalingState.Instance.HpMultiplier != 1.0f;

    private bool EasyPoolScalingModified() =>
        EnemyScalingState.Instance.EasyPoolScalingPercent != 100.0f;

    private static string FormatScalingValue(float value) => $"x{value:F2}";
}
