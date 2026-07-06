using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

public class ModifierGroupControls
{
    public static VBoxContainer BuildAccordionSection(
        ModifierGroup group,
        List<NRunModifierTickbox> groupTickboxes,
        bool startExpanded
    )
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);

        var (headerPanel, headerLabel) = CreateHeaderPanel();
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 0);
        body.Visible = startExpanded;

        foreach (var tickbox in groupTickboxes)
            body.AddChild((Node)(object)tickbox);

        RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);

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
                RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);
            }
        };

        // Single Toggled handler per tickbox: refreshes the header AND enforces mutual
        // exclusion if the group requires it. Guarding on toggled.IsTicked == true prevents
        // cascades when we programmatically untick sibling tickboxes below.
        foreach (var tickbox in groupTickboxes)
        {
            ((GodotObject)(object)tickbox).Connect(
                NTickbox.SignalName.Toggled,
                Callable.From<NRunModifierTickbox>(toggled =>
                {
                    RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);
                    if (group.IsMutuallyExclusive && toggled.IsTicked)
                        foreach (var other in groupTickboxes)
                            if (other != toggled)
                                other.IsTicked = false;
                }),
                0u
            );
        }

        root.AddChild(headerPanel);
        root.AddChild(body);
        return root;
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

    private const int maxCollapsedTextLength = 75;
    private const int numEllipsisChars = 3;

    public static string ModifierGroupCollapsedText(NRunModifierTickbox[] tickboxes)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(string.Join(", ", tickboxes.Select(t => ModifierDisplayName(t.Modifier))));
        if (sb.Length > maxCollapsedTextLength)
        {
            sb.Remove(
                maxCollapsedTextLength - numEllipsisChars,
                sb.Length - (maxCollapsedTextLength - numEllipsisChars)
            );
            sb.Append(new string('.', numEllipsisChars));
        }
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
    /// Creates a styled clickable header using a <see cref="PanelContainer"/> with a
    /// native <see cref="Label"/> child. <c>Label</c> inherits fonts through Godot's
    /// theme tree, picking up the project's custom font automatically at runtime.
    /// <c>NButton</c> is scene-based; <c>new MegaLabel()</c> creates a bare instance
    /// without the font resources embedded in scene-loaded versions.
    /// </summary>
    public static (PanelContainer panel, Label label) CreateHeaderPanel()
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(0, 40);
        // Stop propagates mouse events to this node so GuiInput fires.
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        // Dark brownish section header — visually distinct from the tickboxes below.
        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0.13f, 0.10f, 0.07f, 0.95f);
        normal.BorderWidthLeft = 1;
        normal.BorderWidthTop = 1;
        normal.BorderWidthRight = 1;
        normal.BorderWidthBottom = 1;
        normal.BorderColor = new Color(0.45f, 0.33f, 0.12f, 0.80f);
        normal.CornerRadiusTopLeft = 3;
        normal.CornerRadiusTopRight = 3;
        normal.CornerRadiusBottomLeft = 3;
        normal.CornerRadiusBottomRight = 3;
        normal.ContentMarginLeft = 10;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;

        // Slightly lighter on hover.
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.21f, 0.17f, 0.10f, 0.95f);

        panel.AddThemeStyleboxOverride("panel", normal);
        panel.MouseEntered += () => panel.AddThemeStyleboxOverride("panel", hover);
        panel.MouseExited += () => panel.AddThemeStyleboxOverride("panel", normal);

        var label = new Label();
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        panel.AddChild(label);

        return (panel, label);
    }
}
