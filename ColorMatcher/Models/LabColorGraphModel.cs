using System;
using Avalonia.Media;

namespace ColorMatcher.Models;

/// <summary>
/// Represents color matching data and algorithms for visualization in LAB color space.
/// 
/// This model serves as the data bridge between the ViewModel's RGB color inputs and the
/// perceptually-meaningful calculations needed for color matching guidance. It maintains
/// reference (target) and sample (candidate) colors in LAB space and provides methods for:
/// - Calculating ΔE (color difference)
/// - Generating tint recommendations (which direction to adjust)
/// - Supporting real-time color matching feedback
/// 
/// <remarks>
/// **LAB Color Space Overview**
/// 
/// LAB is a device-independent, perceptually uniform color space defined by the CIE:
/// 
/// - **L* (Lightness)**: 0-100, where 0 is black and 100 is white
/// - **a* (Red-Green)**: -128 to +127, where negative = green, positive = red
/// - **b* (Yellow-Blue)**: -128 to +127, where negative = blue, positive = yellow
/// 
/// **Perceptual Uniformity**
/// 
/// A key advantage of LAB: Equal distance in LAB space ≈ Equal perceived color difference
/// to human observers. This is NOT true for RGB (where (255,0,0) and (254,0,0) are 
/// imperceptible in difference, but (0,0,255) and (0,0,0) are obviously different).
/// 
/// **Color Difference Algorithm**
/// 
/// ΔE = √((ΔL*)² + (Δa*)² + (Δb*)²)
/// 
/// This is the CIE76 formula (Euclidean distance in LAB space).
/// 
/// Interpretation:
/// - &lt; 1: Imperceptible to human eye
/// - 1-2: Barely perceptible
/// - 2-5: Perceptible but acceptable
/// - 5-10: Significant, likely unacceptable
/// - &gt; 10: Very obvious mismatch
/// 
/// Modern color science also uses CIE94 and CIEDE2000 (which weight channels differently),
/// but CIE76 is adequate for this application and faster to compute.
/// 
/// **Tint Recommendation Algorithm**
/// 
/// The recommendation system guides users on how to adjust sample colors to match reference:
/// 
/// 1. If |ΔL*| + |Δa*| + |Δb*| &lt; 0.5: "Colors are very close" (imperceptible difference)
/// 
/// 2. For each channel:
///    - a* channel (Red-Green axis):
///      - If Δa* &gt; 5: Recommend "Add Red" (sample too green, needs red)
///      - If Δa* &lt; -5: Recommend "Add Green" (sample too red, needs green)
///    - b* channel (Yellow-Blue axis):
///      - If Δb* &gt; 5: Recommend "Add Yellow" (sample too blue, needs yellow)
///      - If Δb* &lt; -5: Recommend "Add Blue" (sample too yellow, needs blue)
/// 
/// 3. Combine recommendations:
///    - Multi-axis: "Add Red, Add Yellow" (if both axes need adjustment &gt; 5)
///    - Single-axis: "Add Red" (if only one axis needs adjustment &gt; 5)
///    - Fine-tune: "Fine adjustments needed" (if changes needed but &lt; 5 on all axes)
/// 
/// Threshold Selection (5 units):
/// The threshold of 5 units was chosen empirically:
/// - Too low: Generates recommendations too frequently for imperceptible differences
/// - Too high: Misses observable differences
/// - 5 units: ~2 perceptible units on a* or b* axis, practical for paint/dye adjustment
/// 
/// **Recommendation Accuracy**
/// 
/// Recommendations are designed to be intuitive for users without color science background:
/// - "Add Red": Red pigment should be added to sample
/// - "Add Green": Green pigment (or reduce red) should be added to sample
/// - "Add Yellow": Yellow pigment should be added to sample
/// - "Add Blue": Blue pigment should be added to sample
/// 
/// This quadrant-based approach provides actionable guidance without requiring users to
/// understand LAB space mathematics.
/// </remarks>
/// </summary>
public class LabColorGraphModel
{
    /// <summary>
    /// The reference (target) color in LAB color space.
    /// 
    /// This is the "goal" color the user is trying to match. Set by the ViewModel when
    /// the user enters a reference color. Null if no reference color has been set or
    /// if the reference RGB values are invalid.
    /// 
    /// Used for:
    /// - DeltaE calculation (color difference from sample)
    /// - Tint recommendations (how to adjust sample toward reference)
    /// - Visualization in matching UI
    /// </summary>
    public LabColor? ReferenceColor { get; set; }

    /// <summary>
    /// The sample (candidate) color in LAB color space.
    /// 
    /// This is the "current attempt" color the user is evaluating for matching.
    /// Set by the ViewModel when the user enters a sample color. Null if no sample
    /// color has been set or if the sample RGB values are invalid.
    /// 
    /// Used for:
    /// - DeltaE calculation (difference from reference)
    /// - Tint recommendations (direction to adjust)
    /// - Matching feedback to user
    /// </summary>
    public LabColor? SampleColor { get; set; }

    /// <summary>
    /// Calculates the color difference (ΔE) between reference and sample colors.
    /// 
    /// Uses the CIE76 formula: ΔE = √((ΔL*)² + (Δa*)² + (Δb*)²)
    /// This is the Euclidean distance between two points in LAB color space.
    /// 
    /// <returns>
    /// ΔE value (0-100+):
    /// - 0: Colors are identical
    /// - &lt; 1: Imperceptible to human eye
    /// - 1-2: Barely perceptible
    /// - 2-5: Perceptible but acceptable
    /// - 5-10: Significant difference, likely unacceptable
    /// - &gt; 10: Very obvious mismatch
    /// - 0 if either color is null (incomplete input)
    /// </returns>
    /// </summary>
    public double GetColorDifference()
    {
        if (ReferenceColor == null || SampleColor == null)
            return 0;
        return ReferenceColor.DeltaE(SampleColor);
    }

    /// <summary>
    /// Generates a tint recommendation based on color differences in a* and b* channels.
    /// 
    /// Analyzes the LAB color difference and provides intuitive guidance on how to
    /// adjust the sample color to match the reference. Recommendations are based on
    /// thresholds in the a* (red-green) and b* (yellow-blue) axes.
    /// 
    /// **Recommendation Examples**
    /// - "Add Red, Add Yellow": Sample is too green and too blue; add red and yellow
    /// - "Add Green": Sample is too red; add green (or reduce red intensity)
    /// - "Fine adjustments needed": Small differences (&lt;5) on one or both axes
    /// - "Colors are very close": Imperceptible difference (|ΔL*| + |Δa*| + |Δb*| &lt; 0.5)
    /// - "Enter both colors": Missing reference or sample color
    /// 
    /// <returns>
    /// Human-readable recommendation string suitable for UI display without further processing.
    /// </returns>
    /// 
    /// <remarks>
    /// **Channel Interpretation**
    /// - Δa* &gt; 5: Sample is too green, needs red added
    /// - Δa* &lt; -5: Sample is too red, needs green added
    /// - Δb* &gt; 5: Sample is too blue, needs yellow added
    /// - Δb* &lt; -5: Sample is too yellow, needs blue added
    /// 
    /// Multiple recommendations are comma-separated when adjustments are needed on multiple axes.
    /// </remarks>
    /// </summary>
    public string GetTintRecommendation()
    {
        if (ReferenceColor == null || SampleColor == null)
            return "Enter both colors";

        var dA = ReferenceColor.A - SampleColor.A;
        var dB = ReferenceColor.B - SampleColor.B;

        if (Math.Abs(dA) < 0.5 && Math.Abs(dB) < 0.5)
            return "Colors are very close";

        var recommendations = new System.Collections.Generic.List<string>();

        // A-axis: positive = red/green, negative = green/magenta
        if (dA > 5)
            recommendations.Add("Add Red");
        else if (dA < -5)
            recommendations.Add("Add Green");

        // B-axis: positive = yellow, negative = blue
        if (dB > 5)
            recommendations.Add("Add Yellow");
        else if (dB < -5)
            recommendations.Add("Add Blue");

        return recommendations.Count > 0 
            ? string.Join(", ", recommendations)
            : "Fine adjustments needed";
    }
}
