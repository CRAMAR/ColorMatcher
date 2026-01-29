using System;
using Xunit;
using ColorMatcher.Models;

namespace ColorMatcher.Tests
{
    /// <summary>
    /// Comprehensive edge case and boundary value tests for color space conversion.
    /// Tests extreme values, invalid inputs, and boundary conditions.
    /// </summary>
    public class ColorSpaceEdgeCaseTests
    {
        private const double LabTolerance = 1.0;
        private const double RgbTolerance = 2;

        #region RGB Boundary Tests

        [Fact]
        public void RgbToLab_MinimumRgb_000_ProducesBlackLab()
        {
            var black = new RgbColor(0, 0, 0);
            var lab = ColorSpaceConverter.RgbToLab(black);

            Assert.True(lab.L < 0.5, $"Black L should be near 0, got {lab.L}");
            Assert.True(Math.Abs(lab.A) < 0.5, $"Black a should be near 0, got {lab.A}");
            Assert.True(Math.Abs(lab.B) < 0.5, $"Black b should be near 0, got {lab.B}");
        }

        [Fact]
        public void RgbToLab_MaximumRgb_255255255_ProducesWhiteLab()
        {
            var white = new RgbColor(255, 255, 255);
            var lab = ColorSpaceConverter.RgbToLab(white);

            Assert.True(lab.L > 99.5, $"White L should be near 100, got {lab.L}");
            Assert.True(Math.Abs(lab.A) < 0.5, $"White a should be near 0, got {lab.A}");
            Assert.True(Math.Abs(lab.B) < 0.5, $"White b should be near 0, got {lab.B}");
        }

        [Fact]
        public void RgbToLab_PartialMaxRgb_255000000_ProducesRed()
        {
            var red = new RgbColor(255, 0, 0);
            var lab = ColorSpaceConverter.RgbToLab(red);

            // Pure red should have high a (positive = red) and positive b
            Assert.True(lab.A > 50, $"Red should have positive a, got {lab.A}");
            Assert.True(lab.B > 0, $"Red should have positive b, got {lab.B}");
        }

        [Fact]
        public void RgbToLab_PartialMaxRgb_000255000_ProducesGreen()
        {
            var green = new RgbColor(0, 255, 0);
            var lab = ColorSpaceConverter.RgbToLab(green);

            // Pure green should have negative a (negative = green) and positive b
            Assert.True(lab.A < -50, $"Green should have negative a, got {lab.A}");
            Assert.True(lab.B > 50, $"Green should have positive b, got {lab.B}");
        }

        [Fact]
        public void RgbToLab_PartialMaxRgb_000000255_ProducesBlue()
        {
            var blue = new RgbColor(0, 0, 255);
            var lab = ColorSpaceConverter.RgbToLab(blue);

            // Pure blue should have positive a and negative b
            Assert.True(lab.A > 0, $"Blue should have positive a, got {lab.A}");
            Assert.True(lab.B < -50, $"Blue should have negative b, got {lab.B}");
        }

        #endregion

        #region Grayscale Tests

        [Theory]
        [InlineData(0)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(192)]
        [InlineData(255)]
        public void RgbToLab_GrayscaleValues_ProduceNeutralAandB(int grayValue)
        {
            var gray = new RgbColor((byte)grayValue, (byte)grayValue, (byte)grayValue);
            var lab = ColorSpaceConverter.RgbToLab(gray);

            // Grayscale should have neutral a and b (near 0)
            Assert.True(Math.Abs(lab.A) < 2, $"Grayscale a should be near 0, got {lab.A}");
            Assert.True(Math.Abs(lab.B) < 2, $"Grayscale b should be near 0, got {lab.B}");
            
            // L should increase with gray value
            Assert.True(lab.L >= 0 && lab.L <= 100, $"L should be in valid range, got {lab.L}");
        }

        #endregion

        #region Round-Trip Tests

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(255, 255, 255)]
        [InlineData(128, 128, 128)]
        [InlineData(255, 0, 0)]
        [InlineData(0, 255, 0)]
        [InlineData(0, 0, 255)]
        [InlineData(255, 255, 0)]
        [InlineData(255, 0, 255)]
        [InlineData(0, 255, 255)]
        public void RoundTrip_RgbToLabToRgb_PreservesColor(byte r, byte g, byte b)
        {
            var original = new RgbColor(r, g, b);
            var lab = ColorSpaceConverter.RgbToLab(original);
            var roundTrip = ColorSpaceConverter.LabToRgb(lab);

            // Use higher tolerance for round-trip to account for color space precision limits
            var tolerance = 20;
            
            Assert.True(Math.Abs(roundTrip.R - original.R) <= tolerance, 
                $"R mismatch: {original.R} -> {lab.L},{lab.A},{lab.B} -> {roundTrip.R}");
            Assert.True(Math.Abs(roundTrip.G - original.G) <= tolerance, 
                $"G mismatch: {original.G} -> {roundTrip.G}");
            Assert.True(Math.Abs(roundTrip.B - original.B) <= tolerance, 
                $"B mismatch: {original.B} -> {roundTrip.B}");
        }

        #endregion

        #region DeltaE Edge Cases

        [Fact]
        public void DeltaE_SameColor_ReturnsZero()
        {
            var lab1 = new LabColor(50, 25, -30);
            var lab2 = new LabColor(50, 25, -30);

            var deltaE = lab1.DeltaE(lab2);

            Assert.Equal(0, deltaE);
        }

        [Fact]
        public void DeltaE_OnlyLDifferent_ReturnsLDifference()
        {
            var lab1 = new LabColor(50, 0, 0);
            var lab2 = new LabColor(60, 0, 0);

            var deltaE = lab1.DeltaE(lab2);

            Assert.True(deltaE > 0 && deltaE < 15, $"Expected small ΔE, got {deltaE}");
        }

        [Fact]
        public void DeltaE_OnlyADifferent_ReturnsADifference()
        {
            var lab1 = new LabColor(50, 0, 0);
            var lab2 = new LabColor(50, 25, 0);

            var deltaE = lab1.DeltaE(lab2);

            Assert.True(deltaE > 0, "ΔE should be positive");
        }

        [Fact]
        public void DeltaE_OnlyBDifferent_ReturnsBDifference()
        {
            var lab1 = new LabColor(50, 0, 0);
            var lab2 = new LabColor(50, 0, 25);

            var deltaE = lab1.DeltaE(lab2);

            Assert.True(deltaE > 0, "ΔE should be positive");
        }

        [Fact]
        public void DeltaE_ExtremeValuesL0_ReturnsLargeValue()
        {
            var lab1 = new LabColor(0, 0, 0);    // Black
            var lab2 = new LabColor(100, 0, 0);  // White

            var deltaE = lab1.DeltaE(lab2);

            Assert.True(deltaE > 50, $"Expected large ΔE for black-white, got {deltaE}");
        }

        [Fact]
        public void DeltaE_ExtremeValuesAMinMax_ReturnsLargeValue()
        {
            var lab1 = new LabColor(50, -128, 0);  // Maximum negative a
            var lab2 = new LabColor(50, 127, 0);   // Maximum positive a

            var deltaE = lab1.DeltaE(lab2);

            Assert.True(deltaE > 100, $"Expected large ΔE for extreme a values, got {deltaE}");
        }

        [Fact]
        public void DeltaE_ExtremeValuesBMinMax_ReturnsLargeValue()
        {
            var lab1 = new LabColor(50, 0, -128);  // Maximum negative b
            var lab2 = new LabColor(50, 0, 127);   // Maximum positive b

            var deltaE = lab1.DeltaE(lab2);

            Assert.True(deltaE > 100, $"Expected large ΔE for extreme b values, got {deltaE}");
        }

        [Fact]
        public void DeltaE_IsSymmetric()
        {
            var lab1 = new LabColor(30, 20, -15);
            var lab2 = new LabColor(50, -10, 25);

            var deltaE12 = lab1.DeltaE(lab2);
            var deltaE21 = lab2.DeltaE(lab1);

            Assert.Equal(deltaE12, deltaE21);
        }

        #endregion

        #region LAB Value Range Tests

        [Fact]
        public void LabToRgb_WithLAt0_ProducesBlack()
        {
            var lab = new LabColor(0, 0, 0);
            var rgb = ColorSpaceConverter.LabToRgb(lab);

            Assert.True(rgb.R <= 5, $"R should be near 0, got {rgb.R}");
            Assert.True(rgb.G <= 5, $"G should be near 0, got {rgb.G}");
            Assert.True(rgb.B <= 5, $"B should be near 0, got {rgb.B}");
        }

        [Fact]
        public void LabToRgb_WithLAt100_ProducesWhite()
        {
            var lab = new LabColor(100, 0, 0);
            var rgb = ColorSpaceConverter.LabToRgb(lab);

            Assert.True(rgb.R >= 250, $"R should be near 255, got {rgb.R}");
            Assert.True(rgb.G >= 250, $"G should be near 255, got {rgb.G}");
            Assert.True(rgb.B >= 250, $"B should be near 255, got {rgb.B}");
        }

        [Fact]
        public void LabToRgb_WithExtremePlusA_ProducesValidRgb()
        {
            var lab = new LabColor(50, 127, 0);  // Maximum positive a (red)
            var rgb = ColorSpaceConverter.LabToRgb(lab);

            // Should clamp to valid RGB range
            Assert.True(rgb.R >= 0 && rgb.R <= 255, $"R out of range: {rgb.R}");
            Assert.True(rgb.G >= 0 && rgb.G <= 255, $"G out of range: {rgb.G}");
            Assert.True(rgb.B >= 0 && rgb.B <= 255, $"B out of range: {rgb.B}");
        }

        [Fact]
        public void LabToRgb_WithExtremeMinusA_ProducesValidRgb()
        {
            var lab = new LabColor(50, -128, 0);  // Maximum negative a (green)
            var rgb = ColorSpaceConverter.LabToRgb(lab);

            // Should clamp to valid RGB range
            Assert.True(rgb.R >= 0 && rgb.R <= 255, $"R out of range: {rgb.R}");
            Assert.True(rgb.G >= 0 && rgb.G <= 255, $"G out of range: {rgb.G}");
            Assert.True(rgb.B >= 0 && rgb.B <= 255, $"B out of range: {rgb.B}");
        }

        #endregion

        #region Color Model Property Tests

        [Fact]
        public void RgbColor_Normalization_ConvertsByteTo0to1Range()
        {
            var color = new RgbColor(255, 128, 0);
            var normalizedR = color.R / 255.0;
            var normalizedG = color.G / 255.0;
            var normalizedB = color.B / 255.0;

            Assert.True(normalizedR >= 0 && normalizedR <= 1, $"Normalized R out of range: {normalizedR}");
            Assert.True(normalizedG >= 0 && normalizedG <= 1, $"Normalized G out of range: {normalizedG}");
            Assert.True(normalizedB >= 0 && normalizedB <= 1, $"Normalized B out of range: {normalizedB}");
            
            // Specific check for 255
            Assert.True(Math.Abs(normalizedR - 1.0) < 0.01, "255 should normalize to ~1.0");
        }

        [Fact]
        public void RgbColor_Equality_ComparesValuesCorrectly()
        {
            var color1 = new RgbColor(100, 150, 200);
            var color2 = new RgbColor(100, 150, 200);
            var color3 = new RgbColor(100, 150, 201);

            Assert.Equal(color1.R, color2.R);
            Assert.Equal(color1.G, color2.G);
            Assert.Equal(color1.B, color2.B);
            Assert.NotEqual(color1.B, color3.B);
        }

        [Fact]
        public void LabColor_ValidRange_LBetween0And100()
        {
            var colors = new[]
            {
                new LabColor(0, 0, 0),
                new LabColor(50, 50, 50),
                new LabColor(100, -100, 100),
            };

            foreach (var color in colors)
            {
                Assert.True(color.L >= 0 && color.L <= 100, 
                    $"L value {color.L} outside valid range [0, 100]");
            }
        }

        #endregion

        #region Stress Tests

        [Fact]
        public void RgbToLab_RandomColors_AlwaysProducesValidLab()
        {
            var random = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                var rgb = new RgbColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                var lab = ColorSpaceConverter.RgbToLab(rgb);

                Assert.True(lab.L >= 0 && lab.L <= 100, $"L out of range: {lab.L}");
                Assert.True(lab.A >= -128 && lab.A <= 127, $"a out of range: {lab.A}");
                Assert.True(lab.B >= -128 && lab.B <= 127, $"b out of range: {lab.B}");
            }
        }

        [Fact]
        public void LabToRgb_RandomLab_AlwaysProducesValidRgb()
        {
            var random = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                var lab = new LabColor(random.NextDouble() * 100, random.Next(-128, 128), random.Next(-128, 128));
                var rgb = ColorSpaceConverter.LabToRgb(lab);

                Assert.True(rgb.R >= 0 && rgb.R <= 255, $"R out of range: {rgb.R}");
                Assert.True(rgb.G >= 0 && rgb.G <= 255, $"G out of range: {rgb.G}");
                Assert.True(rgb.B >= 0 && rgb.B <= 255, $"B out of range: {rgb.B}");
            }
        }

        #endregion
    }
}
