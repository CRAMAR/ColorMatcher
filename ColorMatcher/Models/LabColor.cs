using System;

namespace ColorMatcher.Models;

/// <summary>
/// Represents a color in the CIE LAB color space.
/// </summary>
public class LabColor
{
    /// <summary>
    /// Lightness component (0-100).
    /// </summary>
    public double L { get; set; }

    /// <summary>
    /// Green-Red component (-128 to 127, typically).
    /// </summary>
    public double A { get; set; }

    /// <summary>
    /// Blue-Yellow component (-128 to 127, typically).
    /// </summary>
    public double B { get; set; }

    public LabColor(double l, double a, double b)
    {
        L = l;
        A = a;
        B = b;
    }

    /// <summary>
    /// Calculates the Euclidean distance between this color and another in LAB space.
    /// </summary>
    public double DeltaE(LabColor other)
    {
        var dL = L - other.L;
        var dA = A - other.A;
        var dB = B - other.B;
        return Math.Sqrt(dL * dL + dA * dA + dB * dB);
    }

    public override string ToString() => $"LAB(L:{L:F2}, a:{A:F2}, b:{B:F2})";
}
