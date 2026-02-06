using System;

namespace ColorMatcher.Models
{

/// <summary>
/// Represents a color in the CIE LAB (L*a*b*) perceptual color space.
/// 
/// LAB is a device-independent color space designed to be perceptually uniform, meaning 
/// equal distances in LAB space correspond to equal perceived color differences by human eyes.
/// This makes it ideal for color matching applications where perceptual accuracy is critical.
/// </summary>
/// <remarks>
/// CIE LAB color space characteristics:
/// - Device-independent color space (consistent across displays and media)
/// - Perceptually uniform: equal steps in LAB correspond to equal perceptual differences
/// - Three components: L* (lightness), a* (green-red), b* (blue-yellow)
/// - L* ranges from 0 (black) to 100 (white)
/// - a* ranges from -128 (green) to 127 (red), neutral at 0
/// - b* ranges from -128 (blue) to 127 (yellow), neutral at 0
/// - Standard illuminant: D65 (daylight), 2° observer angle
/// 
/// Color Difference (ΔE):
/// The Euclidean distance between two colors in LAB space represents the perceived color difference (ΔE).
/// - ΔE < 1: Difference not perceptible by human eye
/// - ΔE 1-2: Barely perceptible difference
/// - ΔE 2-10: Noticeable difference
/// - ΔE > 10: Very obvious difference
/// 
/// Example usage:
/// <code>
/// var refLab = new LabColor(50, 20, 30);
/// var sampleLab = new LabColor(48, 22, 28);
/// double deltaE = refLab.DeltaE(sampleLab); // ~2.83
/// if (deltaE < 5) Console.WriteLine("Good color match!");
/// </code>
/// </remarks>
public class LabColor
{
    /// <summary>
    /// Lightness component (L*), ranging from 0 (black) to 100 (white).
    /// Represents the perceived brightness of the color independent of hue.
    /// </summary>
    public double L { get; set; }

    /// <summary>
    /// Green-Red opponent component (a*), typically ranging from -128 (green) to 127 (red).
    /// Negative values indicate green hues, positive values indicate red hues.
    /// </summary>
    public double A { get; set; }

    /// <summary>
    /// Blue-Yellow opponent component (b*), typically ranging from -128 (blue) to 127 (yellow).
    /// Negative values indicate blue hues, positive values indicate yellow hues.
    /// </summary>
    public double B { get; set; }

    /// <summary>
    /// Initializes a new instance of the LabColor class with specified component values.
    /// </summary>
    /// <param name="l">Lightness component (L*), typically 0-100</param>
    /// <param name="a">Green-Red component (a*), typically -128 to 127</param>
    /// <param name="b">Blue-Yellow component (b*), typically -128 to 127</param>
    public LabColor(double l, double a, double b)
    {
        L = l;
        A = a;
        B = b;
    }

    /// <summary>
    /// Calculates the color difference (ΔE) between this color and another color using Euclidean distance in LAB space.
    /// </summary>
    /// <param name="other">The other LAB color to compare against</param>
    /// <returns>
    /// The color difference (ΔE) as a double value. Interpretation:
    /// - Less than 1: Not perceptible by human eye
    /// - 1-2: Just barely perceptible
    /// - 2-10: Noticeable difference
    /// - Greater than 10: Very obvious difference
    /// </returns>
    /// <remarks>
    /// This implements the CIE76 color difference formula, which calculates the Euclidean distance
    /// between two points in LAB space: ΔE = √((ΔL*)² + (Δa*)² + (Δb*)²)
    /// 
    /// For industrial applications requiring higher precision, more advanced formulas like CIE94 or CIEDE2000 
    /// may be used, but CIE76 provides a good balance of simplicity and accuracy for paint/gelcoat matching.
    /// </remarks>
    public double DeltaE(LabColor other)
    {
        var dL = L - other.L;
        var dA = A - other.A;
        var dB = B - other.B;
        return Math.Sqrt(dL * dL + dA * dA + dB * dB);
    }

    /// <summary>
    /// Returns a formatted string representation of the LAB color.
    /// </summary>
    /// <returns>String in format "LAB(L:value, a:value, b:value)" with 2 decimal places.</returns>
    public override string ToString() => $"LAB(L:{L:F2}, a:{A:F2}, b:{B:F2})";
    }
}
