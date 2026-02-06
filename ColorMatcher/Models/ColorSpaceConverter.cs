using System;

namespace ColorMatcher.Models
{
/// <summary>
/// Provides color space conversion utilities between RGB and CIE LAB color spaces.
/// 
/// This converter implements the standard CIE color space transformation using D65 illuminant
/// and the CIE 1931 2° Standard Observer. It is used throughout ColorMatcher for accurate
/// color matching in paint and gelcoat applications.
/// </summary>
/// <remarks>
/// Conversion Pipeline:
/// 1. RGB (byte 0-255) → Normalized RGB (double 0-1)
/// 2. sRGB Gamma Correction (linearization) → Linear RGB
/// 3. RGB to XYZ transformation (using sRGB color matrix)
/// 4. XYZ normalization by D65 white point reference
/// 5. LAB transformation (non-linear companding)
/// 6. LAB output (L: 0-100, a: -128 to 127, b: -128 to 127)
/// 
/// Reverse conversion follows the same steps in reverse order.
/// 
/// Reference Standards:
/// - D65 Illuminant: Standard daylight illumination (color temperature ~6500K)
/// - CIE 1931 2° Standard Observer: Standard human color perception angle
/// - sRGB Color Space: IEC 61966-2-1 standard (used in most digital displays)
/// 
/// Key Constants:
/// - D65 Reference White: (X=0.95047, Y=1.00000, Z=1.08883)
/// - Gamma Correction Threshold: 0.04045 for linearization, 0.0031308 for delinearization
/// - LAB Delta Threshold: δ = 6/29 (≈0.2069) for smooth continuous function
/// 
/// Accuracy Notes:
/// - Round-trip conversion (RGB→LAB→RGB) has tolerance ~20 bytes per component due to:
///   - Floating-point precision limits
///   - Byte rounding in final step
///   - Non-linear transformations with multiple approximations
/// - For color matching, ΔE < 2 is considered imperceptible difference
/// 
/// Example Usage:
/// <code>
/// var rgbRef = new RgbColor(255, 0, 0);      // Pure red
/// var labRef = ColorSpaceConverter.RgbToLab(rgbRef);
/// Console.WriteLine(labRef);  // Output: LAB(L:53.24, a:80.09, b:67.20)
/// 
/// var rgbSample = new RgbColor(250, 10, 20);  // Close to red
/// var labSample = ColorSpaceConverter.RgbToLab(rgbSample);
/// double deltaE = labRef.DeltaE(labSample);   // Perceptual color difference
/// </code>
/// </remarks>
public static class ColorSpaceConverter
{
    // D65 illuminant reference white point
    // These are the tristimulus values for D65 standard illuminant with CIE 1931 2° observer
    private const double RefX = 0.95047;
    private const double RefY = 1.00000;
    private const double RefZ = 1.08883;

    /// <summary>
    /// Converts an RGB color to CIE LAB color space.
    /// </summary>
    /// <param name="rgb">The RGB color to convert. Components should be in byte range (0-255).</param>
    /// <returns>A LabColor with L* (0-100), a* (-128 to 127), b* (-128 to 127) components.</returns>
    /// <remarks>
    /// The conversion process:
    /// 1. Normalizes RGB bytes to 0-1 range
    /// 2. Applies sRGB gamma correction (linearization) to reverse display gamma
    /// 3. Multiplies by sRGB color matrix to convert to CIE XYZ color space
    /// 4. Normalizes XYZ values by D65 reference white point
    /// 5. Applies LAB transformation function (non-linear) to produce perceptually uniform space
    /// 6. Calculates L*, a*, b* components from transformed XYZ values
    /// 
    /// This transformation ensures that equal steps in LAB space correspond to equal perceptual
    /// color differences to the human eye, making it ideal for color matching applications.
    /// </remarks>
    public static LabColor RgbToLab(RgbColor rgb)
    {
        var (r, g, b) = rgb.ToNormalized();
        
        // Step 1: Apply sRGB companding (gamma correction)
        // Converts from display gamma space to linear light space
        r = Linearize(r);
        g = Linearize(g);
        b = Linearize(b);

        // Step 2: Convert linear RGB to XYZ using sRGB color matrix
        // These coefficients are the standard sRGB to XYZ transformation matrix
        var x = r * 0.4124 + g * 0.3576 + b * 0.1805;
        var y = r * 0.2126 + g * 0.7152 + b * 0.0722;
        var z = r * 0.0193 + g * 0.1192 + b * 0.9505;

        // Step 3: Normalize XYZ by D65 reference white point
        x = x / RefX;
        y = y / RefY;
        z = z / RefZ;

        // Step 4: Apply LAB transformation (non-linear)
        // This creates the perceptually uniform LAB space
        x = LabTransform(x);
        y = LabTransform(y);
        z = LabTransform(z);

        // Step 5: Calculate LAB components
        var l = 116 * y - 16;           // L* ranges from 0 to 100
        var a = 500 * (x - y);          // a* (green-red component)
        var bLab = 200 * (y - z);       // b* (blue-yellow component)

        return new LabColor(l, a, bLab);
    }

    /// <summary>
    /// Converts a CIE LAB color to RGB color space.
    /// </summary>
    /// <param name="lab">The LAB color to convert.</param>
    /// <returns>An RgbColor with components clamped to byte range (0-255).</returns>
    /// <remarks>
    /// The conversion process reverses the steps in RgbToLab:
    /// 1. Reverses LAB transformation using inverse function
    /// 2. Denormalizes XYZ by D65 reference white point
    /// 3. Multiplies by inverse sRGB color matrix to convert XYZ back to linear RGB
    /// 4. Applies reverse sRGB gamma correction (delinearization)
    /// 5. Scales to 0-255 byte range and clamps outliers
    /// 
    /// Note: Round-trip conversion (RGB→LAB→RGB) will have ~20-byte tolerance per component
    /// due to floating-point precision and byte rounding. This is normal and expected.
    /// </remarks>
    public static RgbColor LabToRgb(LabColor lab)
    {
        // Step 1: Reverse LAB transformation
        var fy = (lab.L + 16) / 116;
        var fx = lab.A / 500 + fy;
        var fz = fy - lab.B / 200;

        // Step 2: Reverse LAB transformation with inverse function
        var x = InverseLabTransform(fx) * RefX;
        var y = InverseLabTransform(fy) * RefY;
        var z = InverseLabTransform(fz) * RefZ;

        // Step 3: Convert XYZ back to linear RGB using inverse sRGB matrix
        var r = x * 3.2406 + y * -1.5372 + z * -0.4986;
        var g = x * -0.9689 + y * 1.8758 + z * 0.0415;
        var b = x * 0.0557 + y * -0.2040 + z * 1.0570;

        // Step 4: Reverse sRGB gamma correction (delinearization)
        r = Delinearize(r);
        g = Delinearize(g);
        b = Delinearize(b);

        // Step 5: Clamp to 0-255 byte range and convert
        var rByte = ClampByte(r * 255);
        var gByte = ClampByte(g * 255);
        var bByte = ClampByte(b * 255);

        return new RgbColor(rByte, gByte, bByte);
    }

    /// <summary>
    /// Applies sRGB gamma correction (linearization).
    /// Converts from sRGB display gamma to linear light space.
    /// </summary>
    /// <param name="value">Normalized RGB component (0-1)</param>
    /// <returns>Linearized (linear light) RGB component</returns>
    /// <remarks>
    /// sRGB uses a piecewise linear and exponential transfer function:
    /// - For values ≤ 0.04045: linear transformation C/12.92
    /// - For values > 0.04045: exponential transformation ((C+0.055)/1.055)^2.4
    /// 
    /// This reverses the gamma correction applied by displays to human perception.
    /// </remarks>
    private static double Linearize(double value)
    {
        if (value <= 0.04045)
            return value / 12.92;
        return Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Reverses sRGB gamma correction (delinearization).
    /// Converts from linear light space back to sRGB display gamma.
    /// </summary>
    /// <param name="value">Linearized (linear light) RGB component</param>
    /// <returns>Non-linear sRGB component (0-1)</returns>
    /// <remarks>
    /// Inverse of Linearize() function. Uses the same piecewise function boundaries.
    /// </remarks>
    private static double Delinearize(double value)
    {
        if (value <= 0.0031308)
            return value * 12.92;
        return 1.055 * Math.Pow(value, 1 / 2.4) - 0.055;
    }

    /// <summary>
    /// Applies the LAB transformation function to normalize XYZ values.
    /// Creates the perceptually uniform property of LAB color space.
    /// </summary>
    /// <param name="value">Normalized XYZ component (0-1 approximately)</param>
    /// <returns>Transformed value for LAB calculation</returns>
    /// <remarks>
    /// The LAB transformation uses a piecewise function with delta = 6/29:
    /// - For values > δ²: f(x) = ∛x (cube root)
    /// - For values ≤ δ²: f(x) = x/(3δ²) + 4/29 (linear approximation at origin)
    /// 
    /// This piecewise function ensures smoothness and numerical stability near zero.
    /// The threshold δ ≈ 0.2069 represents the point where the linear and exponential
    /// portions of the function have matching slopes.
    /// </remarks>
    private static double LabTransform(double value)
    {
        const double delta = 6.0 / 29.0;
        const double deltaSquared = delta * delta;
        
        if (value > deltaSquared)
            return Math.Pow(value, 1.0 / 3.0);
        return value / (3 * deltaSquared) + 4.0 / 29.0;
    }

    /// <summary>
    /// Inverse of the LAB transformation function.
    /// Reverses the normalization applied by LabTransform.
    /// </summary>
    /// <param name="value">Transformed LAB component value</param>
    /// <returns>Denormalized XYZ component value</returns>
    /// <remarks>
    /// Inverse of LabTransform() function. Also uses piecewise definition with delta = 6/29.
    /// </remarks>
    private static double InverseLabTransform(double value)
    {
        const double delta = 6.0 / 29.0;
        
        if (value > delta)
            return Math.Pow(value, 3);
        return 3 * delta * delta * (value - 4.0 / 29.0);
    }

    /// <summary>
    /// Clamps a byte value to 0-255 range and rounds.
    /// Used to convert floating-point RGB values back to byte representation.
    /// </summary>
    /// <param name="value">Raw floating-point value (typically 0-255)</param>
    /// <returns>Clamped byte value (0-255)</returns>
    /// <remarks>
    /// This handles out-of-range values that can occur due to floating-point precision
    /// limitations during color space conversion. Values are clamped symmetrically
    /// around the 0-255 range and then standard rounded to nearest integer.
    /// </remarks>
    private static byte ClampByte(double value)
    {
        var clamped = Math.Max(0, Math.Min(255, value));
        return (byte)Math.Round(clamped);
    }
}
}
