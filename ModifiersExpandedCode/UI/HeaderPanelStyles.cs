using Godot;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// Shared style and layout factory methods used by accordion sections throughout the UI.
/// All methods are stateless and return new Godot resources on every call.
/// </summary>
public static class HeaderPanelStyles
{
    /// <summary>
    /// Builds the base flat style for an accordion header panel.
    /// </summary>
    /// <param name="selected">When true, uses the highlighted (selection) colour scheme.</param>
    public static StyleBoxFlat BuildNormalStyle(bool selected)
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

    /// <summary>
    /// Creates a lightened hover variant of an existing style by duplicating it and
    /// brightening the background colour.
    /// </summary>
    public static StyleBoxFlat BuildHoverVariant(StyleBoxFlat baseStyle)
    {
        var hover = (StyleBoxFlat)baseStyle.Duplicate();
        hover.BgColor = baseStyle.BgColor.Lightened(0.08f);
        return hover;
    }

    /// <summary>
    /// Creates a horizontally expanding header panel with an embedded, ellipsis-trimmed label.
    /// </summary>
    public static (PanelContainer panel, Label label) CreateHeaderPanel()
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
}
