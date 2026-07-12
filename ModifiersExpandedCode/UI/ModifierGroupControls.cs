using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using ModifiersExpanded.ModifiersExpandedCode.State;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

public class ModifierGroupControls
{
    public static (VBoxContainer section, Action refreshHeader) BuildAccordionSection(
        ModifierGroup group,
        List<NRunModifierTickbox> groupTickboxes,
        bool startExpanded
    )
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);

        var (headerPanel, headerLabel) = CreateHeaderPanel();
        var body = new MarginContainer();
        body.AddThemeConstantOverride("margin_top", 8);
        var vbox = new VBoxContainer();
        body.AddChild(vbox);
        body.AddThemeConstantOverride("separation", 0);
        body.Visible = startExpanded;

        foreach (var tickbox in groupTickboxes)
            vbox.AddChild((Node)(object)tickbox);

        // ── Style state ─────────────────────────────────────────────────────
        // Four combinations: (selected | unselected) x (hovered | idle)
        var normalBase = BuildNormalStyle(selected: false);
        var normalHover = BuildHoverVariant(normalBase);
        var selectedBase = BuildNormalStyle(selected: true);
        var selectedHover = BuildHoverVariant(selectedBase);
        bool isHovering = false;

        void ApplyStyle()
        {
            bool hasSelection = groupTickboxes.Any(t => t.IsTicked);
            var style = hasSelection
                ? (isHovering ? selectedHover : selectedBase)
                : (isHovering ? normalHover : normalBase);
            headerPanel.AddThemeStyleboxOverride("panel", style);
        }

        headerPanel.MouseEntered += () =>
        {
            isHovering = true;
            ApplyStyle();
        };
        headerPanel.MouseExited += () =>
        {
            isHovering = false;
            ApplyStyle();
        };

        // ── Unified refresh (style + text) ──────────────────────────────────
        void Refresh()
        {
            ApplyStyle();
            RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);
        }

        Refresh(); // initial state

        // Toggle on header click.
        headerPanel.GuiInput += evt =>
        {
            if (
                evt is InputEventMouseButton btn
                && btn.ButtonIndex == MouseButton.Left
                && btn.Pressed
            )
            {
                body.Visible = !body.Visible;
                Refresh();
            }
        };

        // Single Toggled handler per tickbox: refreshes header AND enforces mutual
        // exclusion if the group requires it. Guarding on toggled.IsTicked == true
        // prevents cascades when we programmatically untick sibling tickboxes.
        foreach (var tickbox in groupTickboxes)
        {
            ((GodotObject)(object)tickbox).Connect(
                NTickbox.SignalName.Toggled,
                Callable.From<NRunModifierTickbox>(toggled =>
                {
                    Refresh();
                    if (group.IsMutuallyExclusive && toggled.IsTicked)
                    {
                        foreach (var other in groupTickboxes)
                            if (other != toggled)
                                other.IsTicked = false;
                    }
                    else if (
                        toggled.Modifier != null
                        && toggled.IsTicked
                        && group.MutuallyExclusiveModifiers.Contains(toggled.Modifier.GetType())
                    )
                    {
                        foreach (var other in groupTickboxes)
                            if (
                                other.Modifier != null
                                && other != toggled
                                && group.MutuallyExclusiveModifiers.Contains(
                                    other.Modifier.GetType()
                                )
                            )
                            {
                                other.IsTicked = false;
                            }
                    }
                }),
                0u
            );
        }

        root.AddChild(headerPanel);
        root.AddChild(body);
        return (root, Refresh);
    }

    private static void RefreshHeader(
        Label label,
        bool expanded,
        ModifierGroup group,
        List<NRunModifierTickbox> tickboxes
    )
    {
        string arrow = expanded ? "▼  " : "▶  ";

        // Collapsed with a selection: show the selected modifier's name.
        if (!expanded)
        {
            NRunModifierTickbox[] selected = tickboxes.Where(t => t.IsTicked).ToArray();
            if (selected.Length > 0)
            {
                label.Text = arrow + ModifierGroupCollapsedText(selected);
                return;
            }
        }

        // Expanded, or collapsed with nothing selected:
        // prefer the group's localized name; fall back to listing member names.
        string text =
            group.GroupName != null
                ? group.GroupName.GetFormattedText()
                : ModifierGroupCollapsedText(tickboxes.ToArray());
        label.Text = arrow + text;
    }

    public static string ModifierGroupCollapsedText(NRunModifierTickbox[] tickboxes)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(string.Join(", ", tickboxes.Select(t => ModifierDisplayName(t.Modifier))));
        return sb.ToString();
    }

    /// <summary>
    /// Converts a PascalCase modifier ID to a readable display name.
    /// "SealedDeck" → "Sealed Deck"
    /// </summary>
    public static string ModifierDisplayName(ModifierModel? modifier)
    {
        if (modifier == null)
            return "?";
        return modifier.Title.GetFormattedText();
    }

    /// <summary>
    /// Creates a styled clickable header panel. Hover and selection styles are applied
    /// externally by <see cref="BuildAccordionSection"/> so they can react to selection state.
    /// </summary>
    private static (PanelContainer panel, Label label) CreateHeaderPanel()
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(0, 50);
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        var label = new Label();
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        panel.AddChild(label);

        return (panel, label);
    }

    // ── Header styles ────────────────────────────────────────────────────────

    private static StyleBoxFlat BuildNormalStyle(bool selected)
    {
        var style = new StyleBoxFlat();
        style.BgColor = selected
            ? new Color(0.22f, 0.16f, 0.07f, 0.95f)
            : new Color(0.13f, 0.10f, 0.07f, 0.95f);
        style.BorderWidthLeft = 1;
        style.BorderWidthTop = 1;
        style.BorderWidthRight = 1;
        style.BorderWidthBottom = 1;
        style.BorderColor = selected
            ? new Color(0.70f, 0.52f, 0.15f, 0.95f)
            : new Color(0.45f, 0.33f, 0.12f, 0.80f);
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3;
        style.CornerRadiusBottomRight = 3;
        style.ContentMarginLeft = 10;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        return style;
    }

    private static StyleBoxFlat BuildHoverVariant(StyleBoxFlat baseStyle)
    {
        var hover = (StyleBoxFlat)baseStyle.Duplicate();
        hover.BgColor = baseStyle.BgColor.Lightened(0.08f);
        return hover;
    }

    public static (VBoxContainer section, Action refreshHeader) BuildEnemyScalingSection(
        string title,
        IReadOnlyList<NRunModifierTickbox> enemyScalingTickboxes
    )
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);

        var (headerPanel, headerLabel) = CreateHeaderPanel();
        var body = new MarginContainer();
        body.AddThemeConstantOverride("margin_top", 8);
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        body.AddChild(vbox);
        body.Visible = false;

        var damageSlider = new ScalingSlider(v => EnemyScalingState.Instance.Damage = v);
        var valueLabel = new Label();
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        valueLabel.Text = FormatScalingValue(EnemyScalingState.Instance.Damage);
        var damageLabel = new Label();
        damageLabel.HorizontalAlignment = HorizontalAlignment.Left;
        damageLabel.Text = new LocString(
            "main_menu_ui",
            "MODIFIER_GROUP.ENEMY_SCALING.damage"
        ).GetFormattedText();
        vbox.AddChild(damageLabel);
        vbox.AddChild(damageSlider);
        vbox.AddChild(valueLabel);

        var normalBase = BuildNormalStyle(selected: false);
        var normalHover = BuildHoverVariant(normalBase);
        var selectedBase = BuildNormalStyle(selected: true);
        var selectedHover = BuildHoverVariant(selectedBase);
        bool isHovering = false;

        void ApplyStyle()
        {
            bool isModified = EnemyScalingState.Instance.Damage != 1.0f;
            var style = isModified
                ? (isHovering ? selectedHover : selectedBase)
                : (isHovering ? normalHover : normalBase);
            headerPanel.AddThemeStyleboxOverride("panel", style);
        }

        void Refresh()
        {
            string arrow = body.Visible ? "\u25bc  " : "\u25b6  ";
            string suffix = body.Visible
                ? ""
                : $": {FormatScalingValue(EnemyScalingState.Instance.Damage)}";
            headerLabel.Text = arrow + title + suffix;
            ApplyStyle();
        }

        headerPanel.MouseEntered += () =>
        {
            isHovering = true;
            ApplyStyle();
        };
        headerPanel.MouseExited += () =>
        {
            isHovering = false;
            ApplyStyle();
        };

        headerPanel.GuiInput += evt =>
        {
            if (
                evt is InputEventMouseButton btn
                && btn.ButtonIndex == MouseButton.Left
                && btn.Pressed
            )
            {
                body.Visible = !body.Visible;
                Refresh();
            }
        };

        damageSlider.ValueChanged += _ =>
        {
            MainFile.Logger.Info(
                MainFile.CreateLogMessage(
                    $"EnemyScaling damage changed to {EnemyScalingState.Instance.Damage}"
                )
            );
            bool ticked = EnemyScalingState.Instance.Damage > 1f;
            foreach (var tb in enemyScalingTickboxes)
            {
                MainFile.Logger.Info(
                    MainFile.CreateLogMessage($"EnemyScaling tickbox {tb} set to {ticked}")
                );
                tb.IsTicked = ticked;
                // IsTicked setter does not emit Toggled; emit it manually so
                // AfterModifiersChanged → ModifiersChanged fires and the run
                // start button picks up the updated modifier list.
                ((GodotObject)(object)tb).EmitSignal(
                    NTickbox.SignalName.Toggled,
                    new Variant[] { Variant.From<GodotObject>((GodotObject)(object)tb) }
                );
            }

            valueLabel.Text = FormatScalingValue(EnemyScalingState.Instance.Damage);
            Refresh();
        };

        Refresh();

        root.AddChild(headerPanel);
        root.AddChild(body);
        return (root, Refresh);
    }

    private static string FormatScalingValue(float value) => $"{value:F2}x";
}
