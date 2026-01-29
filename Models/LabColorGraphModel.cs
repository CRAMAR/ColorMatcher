using System;
using Avalonia.Media;

namespace ColorMatcher.Models;

/// <summary>
/// Represents data for visualizing colors in LAB color space.
/// </summary>
public class LabColorGraphModel
{
    /// <summary>
    /// The reference (target) color in LAB space.
    /// </summary>
    public LabColor? ReferenceColor { get; set; }

    /// <summary>
    /// The sample color in LAB space.
    /// </summary>
    public LabColor? SampleColor { get; set; }

    /// <summary>
    /// Calculates the color difference (ΔE) between reference and sample.
    /// </summary>
    public double GetColorDifference()
    {
        if (ReferenceColor == null || SampleColor == null)
            return 0;
        return ReferenceColor.DeltaE(SampleColor);
    }

    /// <summary>
    /// Gets a tint recommendation based on the a and b difference.
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
