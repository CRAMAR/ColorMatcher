namespace ColorMatcher.Models
{

/// <summary>
/// Represents a color in the RGB (Red-Green-Blue) color space.
/// 
/// RGB is an additive color model where each component ranges from 0 (no intensity) to 255 (maximum intensity).
/// This class is primarily used for input/output operations and sensor readings in the ColorMatcher application.
/// For color difference calculations and perceptual color matching, RGB values are converted to the CIE LAB color space.
/// </summary>
/// <remarks>
/// RGB color space characteristics:
/// - Device-dependent color space (appearance varies by display)
/// - Used for light-based applications (displays, sensors)
/// - Three components: Red (0-255), Green (0-255), Blue (0-255)
/// - Component values are unsigned bytes for efficient storage
/// - Conversion to LAB space uses D65 illuminant with gamma correction
/// 
/// Example usage:
/// <code>
/// var red = new RgbColor(255, 0, 0);
/// var normalized = red.ToNormalized(); // (1.0, 0.0, 0.0)
/// var lab = ColorSpaceConverter.RgbToLab(red);
/// </code>
/// </remarks>
public class RgbColor
{
    /// <summary>
    /// Red component (0-255). Higher values indicate more red intensity.
    /// </summary>
    public byte R { get; set; }

    /// <summary>
    /// Green component (0-255). Higher values indicate more green intensity.
    /// </summary>
    public byte G { get; set; }

    /// <summary>
    /// Blue component (0-255). Higher values indicate more blue intensity.
    /// </summary>
    public byte B { get; set; }

    /// <summary>
    /// Initializes a new instance of the RgbColor class with specified component values.
    /// </summary>
    /// <param name="r">Red component (0-255)</param>
    /// <param name="g">Green component (0-255)</param>
    /// <param name="b">Blue component (0-255)</param>
    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Converts RGB components from 0-255 byte range to normalized 0-1 double range.
    /// </summary>
    /// <returns>
    /// A tuple of normalized (red, green, blue) values where each component ranges from 0.0 to 1.0.
    /// For example, RGB(255, 128, 0) returns (1.0, 0.502, 0.0).
    /// </returns>
    /// <remarks>
    /// This normalization is typically used as an intermediate step in color space conversions.
    /// The normalized values are then gamma-corrected before conversion to LAB color space.
    /// </remarks>
    public (double r, double g, double b) ToNormalized()
    {
        return (R / 255.0, G / 255.0, B / 255.0);
    }

    /// <summary>
    /// Returns a string representation of the RGB color.
    /// </summary>
    /// <returns>String in format "RGB(R, G, B)" where R, G, B are component values 0-255.</returns>
    public override string ToString() => $"RGB({R}, {G}, {B})";
    }
}
