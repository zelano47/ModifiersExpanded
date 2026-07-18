using Godot;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

/// <summary>
/// A self-contained slider component: a label/value header row above an HSlider.
/// The value display updates automatically on each change.
/// </summary>
public partial class ScalingSlider : VBoxContainer
{
    private readonly Label _valueLabel;

    public HSlider Slider { get; internal set; }

    /// <summary>
    /// Initialises a new scaling slider.
    /// </summary>
    /// <param name="label">Display name shown to the left of the current value.</param>
    /// <param name="initialValue">Initial slider value.</param>
    /// <param name="onValueChanged">Callback invoked with the new value on each change.</param>
    /// <param name="minValue">Minimum slider value.</param>
    /// <param name="maxValue">Maximum slider value.</param>
    /// <param name="step">Slider step size.</param>
    /// <param name="formatter">
    /// Optional value formatter. Defaults to two decimal places with an 'x' suffix.
    /// </param>
    public ScalingSlider(
        string label,
        float initialValue,
        Action<float> onValueChanged,
        Func<float, string> formatter,
        float minValue = 1.0f,
        float maxValue = 2.5f,
        float step = 0.05f
    )
    {
        AddThemeConstantOverride("separation", 2);

        var row = new HBoxContainer();
        var nameLabel = new Label();
        nameLabel.Text = label;
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        _valueLabel = new Label();
        _valueLabel.Text = formatter(initialValue);
        _valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(nameLabel);
        row.AddChild(_valueLabel);

        Slider = new HSlider();
        Slider.MinValue = minValue;
        Slider.MaxValue = maxValue;
        Slider.Step = step;
        Slider.Value = initialValue;
        Slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Slider.CustomMinimumSize = new Vector2(0, 32);

        var capturedFormatter = formatter;
        Slider.ValueChanged += v =>
        {
            _valueLabel.Text = capturedFormatter((float)v);
            onValueChanged?.Invoke((float)v);
        };

        AddChild(row);
        AddChild(Slider);
    }
}
