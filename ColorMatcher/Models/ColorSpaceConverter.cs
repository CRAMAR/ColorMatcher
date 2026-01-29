using System;

namespace ColorMatcher.Models;

/// <summary>
/// Provides color space conversion utilities between RGB and LAB color spaces.
/// Uses CIE LAB with D65 illuminant and 2° observer.
/// </summary>
public static class ColorSpaceConverter
{
    // D65 illuminant reference white point
    private const double RefX = 0.95047;
    private const double RefY = 1.00000;
    private const double RefZ = 1.08883;

    /// <summary>
    /// Converts an RGB color to LAB color space.
    /// </summary>
    public static LabColor RgbToLab(RgbColor rgb)
    {
        var (r, g, b) = rgb.ToNormalized();
        
        // Apply sRGB companding (gamma correction)
        r = Linearize(r);
        g = Linearize(g);
        b = Linearize(b);

        // Convert RGB to XYZ using sRGB matrix
        var x = r * 0.4124 + g * 0.3576 + b * 0.1805;
        var y = r * 0.2126 + g * 0.7152 + b * 0.0722;
        var z = r * 0.0193 + g * 0.1192 + b * 0.9505;

        // Normalize by reference white point
        x = x / RefX;
        y = y / RefY;
        z = z / RefZ;

        // Apply LAB transformation
        x = LabTransform(x);
        y = LabTransform(y);
        z = LabTransform(z);

        var l = 116 * y - 16;
        var a = 500 * (x - y);
        var bLab = 200 * (y - z);

        return new LabColor(l, a, bLab);
    }

    /// <summary>
    /// Converts a LAB color to RGB color space.
    /// </summary>
    public static RgbColor LabToRgb(LabColor lab)
    {
        // Reverse LAB transformation
        var fy = (lab.L + 16) / 116;
        var fx = lab.A / 500 + fy;
        var fz = fy - lab.B / 200;

        var x = InverseLabTransform(fx) * RefX;
        var y = InverseLabTransform(fy) * RefY;
        var z = InverseLabTransform(fz) * RefZ;

        // Convert XYZ to RGB using inverse sRGB matrix
        var r = x * 3.2406 + y * -1.5372 + z * -0.4986;
        var g = x * -0.9689 + y * 1.8758 + z * 0.0415;
        var b = x * 0.0557 + y * -0.2040 + z * 1.0570;

        // Reverse sRGB companding
        r = Delinearize(r);
        g = Delinearize(g);
        b = Delinearize(b);

        // Clamp to 0-255 and round
        var rByte = ClampByte(r * 255);
        var gByte = ClampByte(g * 255);
        var bByte = ClampByte(b * 255);

        return new RgbColor(rByte, gByte, bByte);
    }

    /// <summary>
    /// Applies sRGB gamma correction (linearization).
    /// </summary>
    private static double Linearize(double value)
    {
        if (value <= 0.04045)
            return value / 12.92;
        return Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Reverses sRGB gamma correction (delinearization).
    /// </summary>
    private static double Delinearize(double value)
    {
        if (value <= 0.0031308)
            return value * 12.92;
        return 1.055 * Math.Pow(value, 1 / 2.4) - 0.055;
    }

    /// <summary>
    /// LAB transformation function.
    /// </summary>
    private static double LabTransform(double value)
    {
        const double delta = 6.0 / 29.0;
        const double deltaSquared = delta * delta;
        
        if (value > deltaSquared)
            return Math.Pow(value, 1.0 / 3.0);
        return value / (3 * deltaSquared) + 4.0 / 29.0;
    }

    /// <summary>
    /// Inverse LAB transformation function.
    /// </summary>
    private static double InverseLabTransform(double value)
    {
        const double delta = 6.0 / 29.0;
        
        if (value > delta)
            return Math.Pow(value, 3);
        return 3 * delta * delta * (value - 4.0 / 29.0);
    }

    /// <summary>
    /// Clamps a byte value to 0-255 range and rounds.
    /// </summary>
    private static byte ClampByte(double value)
    {
        var clamped = Math.Max(0, Math.Min(255, value));
        return (byte)Math.Round(clamped);
    }
}
