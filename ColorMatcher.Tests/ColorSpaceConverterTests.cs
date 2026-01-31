using ColorMatcher.Models;

namespace ColorMatcher.Tests;

/// <summary>
/// Tests for RGB to LAB color space conversion accuracy.
/// </summary>
public class ColorSpaceConverterTests
{
    private const double Tolerance = 0.5;

    [Fact]
    public void RgbToLab_White_ReturnsCorrectLabValues()
    {
        var white = new RgbColor(255, 255, 255);

        var lab = ColorSpaceConverter.RgbToLab(white);

        Assert.True(lab.L > 99.9 && lab.L <= 100, $"Expected L ≈ 100, got {lab.L}");
        Assert.True(Math.Abs(lab.A) < Tolerance, $"Expected a ≈ 0, got {lab.A}");
        Assert.True(Math.Abs(lab.B) < Tolerance, $"Expected b ≈ 0, got {lab.B}");
    }

    [Fact]
    public void RgbToLab_Black_ReturnsCorrectLabValues()
    {
        var black = new RgbColor(0, 0, 0);

        var lab = ColorSpaceConverter.RgbToLab(black);

        Assert.True(lab.L < 0.1, $"Expected L ≈ 0, got {lab.L}");
        Assert.True(Math.Abs(lab.A) < Tolerance, $"Expected a ≈ 0, got {lab.A}");
        Assert.True(Math.Abs(lab.B) < Tolerance, $"Expected b ≈ 0, got {lab.B}");
    }

    [Fact]
    public void RgbToLab_Red_ReturnsCorrectLabValues()
    {
        var red = new RgbColor(255, 0, 0);

        var lab = ColorSpaceConverter.RgbToLab(red);

        Assert.True(lab.L > 50 && lab.L < 55, $"Expected L ≈ 53, got {lab.L}");
        Assert.True(lab.A > 75, $"Expected a > 75 (positive = red), got {lab.A}");
        Assert.True(lab.B > 30, $"Expected b > 30, got {lab.B}");
    }

    [Fact]
    public void RgbToLab_Green_ReturnsCorrectLabValues()
    {
        var green = new RgbColor(0, 255, 0);

        var lab = ColorSpaceConverter.RgbToLab(green);

        Assert.True(lab.L > 85 && lab.L < 90, $"Expected L ≈ 87, got {lab.L}");
        Assert.True(lab.A < -70, $"Expected a < -70 (negative = green), got {lab.A}");
        Assert.True(lab.B > 80, $"Expected b > 80, got {lab.B}");
    }

    [Fact]
    public void RgbToLab_Blue_ReturnsCorrectLabValues()
    {
        var blue = new RgbColor(0, 0, 255);

        var lab = ColorSpaceConverter.RgbToLab(blue);

        Assert.True(lab.L > 25 && lab.L < 35, $"Expected L ≈ 32, got {lab.L}");
        Assert.True(lab.A > 60, $"Expected a > 60, got {lab.A}");
        Assert.True(lab.B < -100, $"Expected b < -100 (negative = blue), got {lab.B}");
    }

    [Fact]
    public void LabToRgb_White_ReturnsApproximatelyWhite()
    {
        var white = new RgbColor(255, 255, 255);
        var lab = ColorSpaceConverter.RgbToLab(white);

        var rgbBack = ColorSpaceConverter.LabToRgb(lab);

        Assert.True(Math.Abs(rgbBack.R - white.R) <= 1, $"R mismatch: {white.R} vs {rgbBack.R}");
        Assert.True(Math.Abs(rgbBack.G - white.G) <= 1, $"G mismatch: {white.G} vs {rgbBack.G}");
        Assert.True(Math.Abs(rgbBack.B - white.B) <= 1, $"B mismatch: {white.B} vs {rgbBack.B}");
    }

    [Fact]
    public void LabToRgb_Black_ReturnsApproximatelyBlack()
    {
        var black = new RgbColor(0, 0, 0);
        var lab = ColorSpaceConverter.RgbToLab(black);

        var rgbBack = ColorSpaceConverter.LabToRgb(lab);

        Assert.True(Math.Abs(rgbBack.R - black.R) <= 1, $"R mismatch: {black.R} vs {rgbBack.R}");
        Assert.True(Math.Abs(rgbBack.G - black.G) <= 1, $"G mismatch: {black.G} vs {rgbBack.G}");
        Assert.True(Math.Abs(rgbBack.B - black.B) <= 1, $"B mismatch: {black.B} vs {rgbBack.B}");
    }

    [Fact]
    public void LabToRgb_RoundTrip_MidtoneGray_PreservesColor()
    {
        var gray = new RgbColor(128, 128, 128);

        var lab = ColorSpaceConverter.RgbToLab(gray);
        var rgbBack = ColorSpaceConverter.LabToRgb(lab);

        Assert.True(Math.Abs(rgbBack.R - gray.R) <= 2);
        Assert.True(Math.Abs(rgbBack.G - gray.G) <= 2);
        Assert.True(Math.Abs(rgbBack.B - gray.B) <= 2);
    }

    [Fact]
    public void DeltaE_IdenticalColors_ReturnsZero()
    {
        var color1 = new RgbColor(100, 150, 200);
        var lab1 = ColorSpaceConverter.RgbToLab(color1);
        var lab2 = ColorSpaceConverter.RgbToLab(color1);

        var deltaE = lab1.DeltaE(lab2);

        Assert.True(deltaE < 0.01, $"Expected ΔE ≈ 0, got {deltaE}");
    }

    [Fact]
    public void DeltaE_BlackAndWhite_ReturnsLargeValue()
    {
        var black = new RgbColor(0, 0, 0);
        var white = new RgbColor(255, 255, 255);
        var labBlack = ColorSpaceConverter.RgbToLab(black);
        var labWhite = ColorSpaceConverter.RgbToLab(white);

        var deltaE = labBlack.DeltaE(labWhite);

        Assert.True(deltaE > 100, $"Expected large ΔE, got {deltaE}");
    }

    [Fact]
    public void DeltaE_SimilarColors_ReturnsSmallValue()
    {
        var color1 = new RgbColor(100, 100, 100);
        var color2 = new RgbColor(105, 105, 105);
        var lab1 = ColorSpaceConverter.RgbToLab(color1);
        var lab2 = ColorSpaceConverter.RgbToLab(color2);

        var deltaE = lab1.DeltaE(lab2);

        Assert.True(deltaE < 5, $"Expected small ΔE, got {deltaE}");
    }
}
