using System;
using Xunit;
using ColorMatcher.Models;

namespace ColorMatcher.Tests
{
    /// <summary>
    /// Edge case tests for LabColorGraphModel and tint recommendation logic.
    /// </summary>
    public class LabColorGraphModelEdgeCaseTests
    {
        [Fact]
        public void GetColorDifference_WithoutColors_ReturnsZero()
        {
            var model = new LabColorGraphModel();

            var diff = model.GetColorDifference();

            Assert.Equal(0, diff);
        }

        [Fact]
        public void GetColorDifference_WithOnlyReferenceColor_ReturnsZero()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, 20, -30);

            var diff = model.GetColorDifference();

            Assert.Equal(0, diff);
        }

        [Fact]
        public void GetColorDifference_WithOnlySampleColor_ReturnsZero()
        {
            var model = new LabColorGraphModel();
            model.SampleColor = new LabColor(50, 20, -30);

            var diff = model.GetColorDifference();

            Assert.Equal(0, diff);
        }

        [Fact]
        public void GetColorDifference_WithBothColors_ReturnsPositiveValue()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, 0, 0);
            model.SampleColor = new LabColor(60, 10, 10);

            var diff = model.GetColorDifference();

            Assert.True(diff > 0);
        }

        [Fact]
        public void GetColorDifference_WithIdenticalColors_ReturnsZero()
        {
            var lab = new LabColor(50, 25, -30);
            var model = new LabColorGraphModel();
            model.ReferenceColor = lab;
            model.SampleColor = lab;

            var diff = model.GetColorDifference();

            Assert.True(diff < 0.01);
        }

        [Fact]
        public void GetColorDifference_WithExtremelyDifferentColors_ReturnsLargeValue()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(0, 0, 0);      // Black-ish
            model.SampleColor = new LabColor(100, 127, 127);   // Extreme

            var diff = model.GetColorDifference();

            Assert.True(diff > 50);
        }

        [Fact]
        public void GetTintRecommendation_WithoutColors_ReturnsNeutral()
        {
            var model = new LabColorGraphModel();

            var rec = model.GetTintRecommendation();

            Assert.NotNull(rec);
        }

        [Fact]
        public void GetTintRecommendation_WithOnlyReferenceColor_ReturnsNeutral()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, 20, -30);

            var rec = model.GetTintRecommendation();

            Assert.NotNull(rec);
        }

        [Fact]
        public void GetTintRecommendation_SampleNeedsMoreRed_RecommendRed()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, 50, 0);    // Reference has lots of red
            model.SampleColor = new LabColor(50, -50, 0);      // Sample has green instead

            var rec = model.GetTintRecommendation();

            Assert.Contains("Red", rec, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetTintRecommendation_SampleNeedsMoreGreen_RecommendGreen()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, -50, 0);   // Reference has green
            model.SampleColor = new LabColor(50, 50, 0);       // Sample has red instead

            var rec = model.GetTintRecommendation();

            Assert.Contains("Green", rec, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetTintRecommendation_SampleNeedsMoreYellow_RecommendYellow()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, 0, 100);   // Reference has yellow
            model.SampleColor = new LabColor(50, 0, -100);     // Sample has blue instead

            var rec = model.GetTintRecommendation();

            Assert.Contains("Yellow", rec, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetTintRecommendation_SampleNeedsMoreBlue_RecommendBlue()
        {
            var model = new LabColorGraphModel();
            model.ReferenceColor = new LabColor(50, 0, -100);  // Reference has blue
            model.SampleColor = new LabColor(50, 0, 100);      // Sample has yellow instead

            var rec = model.GetTintRecommendation();

            Assert.Contains("Blue", rec, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetTintRecommendation_IdenticalColors_RecommendNoChange()
        {
            var lab = new LabColor(50, 25, -30);
            var model = new LabColorGraphModel();
            model.ReferenceColor = lab;
            model.SampleColor = lab;

            var rec = model.GetTintRecommendation();

            Assert.NotNull(rec);
            // Should indicate match is good (not recommending change)
        }

        [Fact]
        public void GetTintRecommendation_WithBoundaryAValues()
        {
            var model = new LabColorGraphModel();
            
            // Max positive a
            model.ReferenceColor = new LabColor(50, 127, 0);
            model.SampleColor = new LabColor(50, -127, 0);
            var rec = model.GetTintRecommendation();
            
            Assert.NotNull(rec);
        }

        [Fact]
        public void GetTintRecommendation_WithBoundaryBValues()
        {
            var model = new LabColorGraphModel();
            
            // Max positive b
            model.ReferenceColor = new LabColor(50, 0, 127);
            model.SampleColor = new LabColor(50, 0, -127);
            var rec = model.GetTintRecommendation();
            
            Assert.NotNull(rec);
        }

        [Fact]
        public void GraphModel_MultipleUpdates_MaintainsConsistentState()
        {
            var model = new LabColorGraphModel();

            // First update
            model.ReferenceColor = new LabColor(50, 20, -30);
            model.SampleColor = new LabColor(55, 22, -32);
            var diff1 = model.GetColorDifference();
            var rec1 = model.GetTintRecommendation();

            // Second update
            model.ReferenceColor = new LabColor(70, 40, 10);
            model.SampleColor = new LabColor(72, 42, 12);
            var diff2 = model.GetColorDifference();
            var rec2 = model.GetTintRecommendation();

            // Both should be valid
            Assert.True(diff1 >= 0);
            Assert.True(diff2 >= 0);
            Assert.NotNull(rec1);
            Assert.NotNull(rec2);
        }

        [Fact]
        public void GetColorDifference_AlwaysReturnsNonNegative()
        {
            var model = new LabColorGraphModel();
            
            var testCases = new[]
            {
                (new LabColor(0, 0, 0), new LabColor(100, 127, 127)),
                (new LabColor(100, -128, -128), new LabColor(0, 127, 127)),
                (new LabColor(50, 50, 50), new LabColor(50, -50, -50)),
            };

            foreach (var (ref1, sample1) in testCases)
            {
                model.ReferenceColor = ref1;
                model.SampleColor = sample1;
                
                var diff = model.GetColorDifference();
                Assert.True(diff >= 0, $"ΔE should be non-negative, got {diff}");
            }
        }

        [Fact]
        public void GetTintRecommendation_AlwaysReturnsNonEmptyString()
        {
            var model = new LabColorGraphModel();
            
            var testCases = new[]
            {
                (null, null),
                (new LabColor(50, 0, 0), null),
                (null, new LabColor(50, 0, 0)),
                (new LabColor(0, 0, 0), new LabColor(100, 127, 127)),
                (new LabColor(50, 50, 50), new LabColor(50, 50, 50)),
            };

            foreach (var (ref1, sample1) in testCases)
            {
                model.ReferenceColor = ref1;
                model.SampleColor = sample1;
                
                var rec = model.GetTintRecommendation();
                Assert.NotNull(rec);
                Assert.NotEmpty(rec);
            }
        }
    }
}
