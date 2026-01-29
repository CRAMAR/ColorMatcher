namespace ColorMatcher.Models;

/// <summary>
/// Represents a color in the RGB color space.
/// </summary>
public class RgbColor
{
    /// <summary>
    /// Red component (0-255).
    /// </summary>
    public byte R { get; set; }

    /// <summary>
    /// Green component (0-255).
    /// </summary>
    public byte G { get; set; }

    /// <summary>
    /// Blue component (0-255).
    /// </summary>
    public byte B { get; set; }

    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Converts RGB to a normalized 0-1 scale.
    /// </summary>
    public (double r, double g, double b) ToNormalized()
    {
        return (R / 255.0, G / 255.0, B / 255.0);
    }

    public override string ToString() => $"RGB({R}, {G}, {B})";
}
