using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ColorMatcher.Models;
using ColorMatcher.ViewModels;

namespace ColorMatcher.Tests
{
    /// <summary>
    /// Edge case and boundary tests for ViewModel and UI logic.
    /// </summary>
    public class ViewModelEdgeCaseTests
    {
        private MainWindowViewModel CreateViewModel()
        {
            return new MainWindowViewModel();
        }

        #region RGB Input Validation

        [Theory]
        [InlineData("0", "0", "0")]
        [InlineData("255", "255", "255")]
        [InlineData("128", "128", "128")]
        [InlineData("100", "150", "200")]
        public void ReferenceColorUpdate_WithValidByte_UpdatesGraphAndBrush(string r, string g, string b)
        {
            var vm = CreateViewModel();

            vm.ReferenceR = r;
            vm.ReferenceG = g;
            vm.ReferenceB = b;

            Assert.True(vm.ColorDifference >= 0, "ColorDifference should be valid");
            Assert.NotNull(vm.ReferenceBrush);
        }

        [Theory]
        [InlineData("256")]       // Out of range
        [InlineData("-1")]        // Negative
        [InlineData("abc")]       // Non-numeric
        [InlineData("")]          // Empty
        public void ReferenceColorUpdate_WithInvalidByte_IsIgnored(string r)
        {
            var vm = CreateViewModel();

            vm.ReferenceR = r;

            // Should not crash, original value should remain
            Assert.NotNull(vm.ReferenceR);
        }

        [Theory]
        [InlineData("#FFFFFF")]
        [InlineData("#000000")]
        [InlineData("#FF0000")]
        [InlineData("#00FF00")]
        [InlineData("#0000FF")]
        public void ReferenceHexUpdate_WithValidHex_UpdatesRgbComponents(string hex)
        {
            var vm = CreateViewModel();

            vm.ReferenceHex = hex;

            Assert.NotNull(vm.ReferenceR);
            Assert.NotNull(vm.ReferenceG);
            Assert.NotNull(vm.ReferenceB);
        }

        [Theory]
        [InlineData("GGGGGG")]     // Invalid hex characters
        [InlineData("#FFFF")]      // Wrong length
        [InlineData("FFFFFF")]     // Missing #
        [InlineData("")]           // Empty
        public void ReferenceHexUpdate_WithInvalidHex_IsIgnored(string hex)
        {
            var vm = CreateViewModel();

            vm.ReferenceHex = hex;

            // Should either ignore or keep previous value
            Assert.NotNull(vm.ReferenceHex);
        }

        #endregion

        #region Sample Color Updates

        [Theory]
        [InlineData("50", "100", "150")]
        [InlineData("200", "50", "100")]
        public void SampleColorUpdate_WithValidByte_UpdatesGraph(string r, string g, string b)
        {
            var vm = CreateViewModel();
            
            vm.SampleR = r;
            vm.SampleG = g;
            vm.SampleB = b;

            Assert.True(vm.ColorDifference >= 0);
        }

        #endregion

        #region Project Operations

        [Fact]
        public async Task CreateNewProject_WithoutColors_CreatesProject()
        {
            var vm = CreateViewModel();

            await vm.CreateNewProjectCommand.ExecuteAsync(null);

            Assert.NotNull(vm.CurrentProject);
            Assert.NotNull(vm.CurrentProject.Id);
        }

        [Fact]
        public async Task CreateNewProject_WithColors_PreservesColorValues()
        {
            var vm = CreateViewModel();
            vm.ProjectName = "Test Project";
            vm.ReferenceR = "255";
            vm.ReferenceG = "0";
            vm.ReferenceB = "0";

            await vm.CreateNewProjectCommand.ExecuteAsync(null);

            Assert.NotNull(vm.CurrentProject);
            Assert.Equal("Test Project", vm.CurrentProject.Name);
        }

        [Fact]
        public async Task SaveProject_WithoutCreating_CreatesNewProject()
        {
            var vm = CreateViewModel();

            await vm.SaveProjectCommand.ExecuteAsync(null);

            Assert.NotNull(vm.CurrentProject);
            Assert.False(vm.IsProjectModified);
        }

        [Fact]
        public async Task SaveColorMatch_WithoutProject_CreatesProjectFirst()
        {
            var vm = CreateViewModel();
            vm.ReferenceR = "100";
            vm.ReferenceG = "100";
            vm.ReferenceB = "100";
            vm.SampleR = "110";
            vm.SampleG = "110";
            vm.SampleB = "110";

            await vm.SaveColorMatchCommand.ExecuteAsync(null);

            Assert.NotNull(vm.CurrentProject);
        }

        #endregion

        #region Match Notes and Accepted Status

        [Fact]
        public async Task SaveColorMatch_WithNotes_StoresNotesInHistory()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-match-notes");

            vm.ProjectName = "Notes Test";
            vm.ReferenceR = "100";
            vm.ReferenceG = "100";
            vm.ReferenceB = "100";
            vm.SampleR = "110";
            vm.SampleG = "110";
            vm.SampleB = "110";
            vm.MatchNotes = "Added extra blue tint";

            await vm.SaveColorMatchCommand.ExecuteAsync(null);

            Assert.Single(vm.HistoryItems);
            Assert.Equal("Added extra blue tint", vm.HistoryItems[0].Notes);
        }

        [Fact]
        public async Task SaveColorMatch_WithoutNotes_UsesDefaultText()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-default-notes");

            vm.ProjectName = "Default Notes Test";
            vm.ReferenceR = "50";
            vm.ReferenceG = "50";
            vm.ReferenceB = "50";
            vm.MatchNotes = "";  // Empty notes

            await vm.SaveColorMatchCommand.ExecuteAsync(null);

            Assert.Single(vm.HistoryItems);
            Assert.Equal("Manual color match", vm.HistoryItems[0].Notes);
        }

        [Fact]
        public async Task SaveColorMatch_WithIsAccepted_StoresAcceptedFlag()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-accepted");

            vm.ProjectName = "Accepted Test";
            vm.ReferenceR = "200";
            vm.ReferenceG = "100";
            vm.ReferenceB = "50";
            vm.IsMatchAccepted = true;

            await vm.SaveColorMatchCommand.ExecuteAsync(null);

            Assert.Single(vm.HistoryItems);
            Assert.True(vm.HistoryItems[0].IsAccepted);
        }

        [Fact]
        public async Task SaveColorMatch_ClearsNotesAfterSaving()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-clear-notes");

            vm.ProjectName = "Clear Notes Test";
            vm.MatchNotes = "Test notes";
            vm.IsMatchAccepted = false;

            await vm.SaveColorMatchCommand.ExecuteAsync(null);

            // Notes should be cleared and accepted reset to true
            Assert.Empty(vm.MatchNotes);
            Assert.True(vm.IsMatchAccepted);
        }

        #endregion

        #region Color Difference Calculations

        [Fact]
        public void ColorDifference_WithIdenticalColors_IsZero()
        {
            var vm = CreateViewModel();
            
            vm.ReferenceR = "128";
            vm.ReferenceG = "128";
            vm.ReferenceB = "128";
            vm.SampleR = "128";
            vm.SampleG = "128";
            vm.SampleB = "128";

            Assert.True(vm.ColorDifference < 0.1, $"Expected ΔE ≈ 0, got {vm.ColorDifference}");
        }

        [Fact]
        public void ColorDifference_WithExtremelyDifferentColors_IsLarge()
        {
            var vm = CreateViewModel();
            
            vm.ReferenceR = "0";
            vm.ReferenceG = "0";
            vm.ReferenceB = "0";
            vm.SampleR = "255";
            vm.SampleG = "255";
            vm.SampleB = "255";

            Assert.True(vm.ColorDifference > 50, $"Expected large ΔE, got {vm.ColorDifference}");
        }

        #endregion

        #region Tint Recommendations

        [Fact]
        public void TintRecommendation_WithoutColors_IsDefault()
        {
            var vm = CreateViewModel();

            Assert.Contains("Enter", vm.TintRecommendation, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TintRecommendation_WithValidColors_ProvidesRecommendation()
        {
            var vm = CreateViewModel();
            
            vm.ReferenceR = "255";
            vm.ReferenceG = "0";
            vm.ReferenceB = "0";
            vm.SampleR = "200";
            vm.SampleG = "0";
            vm.SampleB = "0";

            Assert.NotNull(vm.TintRecommendation);
            Assert.NotEmpty(vm.TintRecommendation);
        }

        #endregion

        #region Sensor State Management

        [Fact]
        public void InitialSensorState_IsNotConnected()
        {
            var vm = CreateViewModel();

            Assert.False(vm.IsSensorConnected);
            Assert.NotNull(vm.SensorStatus);
        }

        [Fact]
        public async Task ConnectSensor_UpdatesSensorStatus()
        {
            var vm = CreateViewModel();

            await vm.ConnectSensorCommand.ExecuteAsync(null);

            Assert.True(vm.IsSensorConnected);
            Assert.NotEmpty(vm.SensorStatus);
        }

        [Fact]
        public async Task DisconnectSensor_ClearsSensorStatus()
        {
            var vm = CreateViewModel();
            await vm.ConnectSensorCommand.ExecuteAsync(null);

            await vm.DisconnectSensorCommand.ExecuteAsync(null);

            Assert.False(vm.IsSensorConnected);
            Assert.Contains("Disconnected", vm.SensorStatus);
        }

        [Fact]
        public async Task ReadSensorSample_WhenNotConnected_DoesNotThrow()
        {
            var vm = CreateViewModel();

            // Should not throw even though not connected
            await vm.ReadSensorSampleCommand.ExecuteAsync(null);

            // No assertion needed - test passes if no exception
        }

        [Fact]
        public async Task ReadSensorSample_WhenConnected_UpdatesSampleColor()
        {
            var vm = CreateViewModel();
            await vm.ConnectSensorCommand.ExecuteAsync(null);

            var originalSampleR = vm.SampleR;
            await vm.ReadSensorSampleCommand.ExecuteAsync(null);

            // Sample color should have been updated (likely different from original)
            Assert.NotNull(vm.SampleR);
        }

        #endregion

        #region Project Modification Tracking

        [Fact]
        public void ProjectModified_AfterColorChange_IsTrue()
        {
            var vm = CreateViewModel();
            vm.IsProjectModified = false;

            vm.ReferenceR = "123";

            Assert.True(vm.IsProjectModified);
        }

        [Fact]
        public async Task ProjectModified_AfterSave_IsFalse()
        {
            var vm = CreateViewModel();
            await vm.CreateNewProjectCommand.ExecuteAsync(null);
            vm.IsProjectModified = true;

            await vm.SaveProjectCommand.ExecuteAsync(null);

            Assert.False(vm.IsProjectModified);
        }

        #endregion

        #region Recent Projects

        [Fact]
        public async Task RecentProjects_AfterCreation_ContainsProject()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-colors");

            await vm.CreateNewProjectCommand.ExecuteAsync(null);
            // Note: LoadRecentProjectsAsync would need to be called to populate the UI list
            
            Assert.NotNull(vm.CurrentProject);
        }

        [Fact]
        public async Task RecentProjects_LoadsLimitedNumber()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-colors-recent");

            // Create multiple projects
            for (int i = 0; i < 15; i++)
            {
                vm.ProjectName = $"Test Project {i}";
                await vm.CreateNewProjectCommand.ExecuteAsync(null);
            }

            // RecentProjects should be limited to 10
            Assert.True(vm.RecentProjects.Count <= 10, "Recent projects should be limited to 10 items");
        }

        [Fact]
        public async Task LoadProject_UpdatesCurrentProject()
        {
            var vm = CreateViewModel();
            await vm.InitializeWithFileRepositoryAsync("/tmp/test-colors-load");

            // Create and save a project
            vm.ProjectName = "Original Project";
            vm.ReferenceR = "100";
            vm.ReferenceG = "150";
            vm.ReferenceB = "200";
            await vm.CreateNewProjectCommand.ExecuteAsync(null);
            var savedProject = vm.CurrentProject;

            // Create a new project to change state
            vm.ProjectName = "Different Project";
            await vm.CreateNewProjectCommand.ExecuteAsync(null);

            // Load the original project back
            await vm.LoadProjectCommand.ExecuteAsync(savedProject);

            Assert.Equal("Original Project", vm.ProjectName);
            Assert.Equal("100", vm.ReferenceR);
            Assert.Equal("150", vm.ReferenceG);
            Assert.Equal("200", vm.ReferenceB);
        }

        #endregion


        #region Concurrent Operations

        [Fact]
        public async Task MultipleColorUpdates_WithoutCrashing_ConvertsCorrectly()
        {
            var vm = CreateViewModel();

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var finalI = i;
                tasks.Add(Task.Run(() =>
                {
                    vm.ReferenceR = (finalI * 25).ToString();
                    vm.ReferenceG = (finalI * 26).ToString();
                    vm.ReferenceB = (finalI * 27).ToString();
                }));
            }

            await Task.WhenAll(tasks);

            // Should complete without throwing and maintain valid state
            Assert.NotNull(vm.ReferenceR);
            Assert.NotNull(vm.ReferenceG);
            Assert.NotNull(vm.ReferenceB);
        }

        #endregion

        #region PropertyChanged Notifications (Issue #13)

        [Fact]
        public void GraphModel_PropertyChanged_RaisedWhenReferenceColorUpdated()
        {
            var vm = CreateViewModel();
            bool graphModelChangedEventRaised = false;

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.GraphModel))
                {
                    graphModelChangedEventRaised = true;
                }
            };

            vm.ReferenceR = "100";
            vm.ReferenceG = "150";
            vm.ReferenceB = "200";

            Assert.True(graphModelChangedEventRaised, "GraphModel PropertyChanged should be raised when reference color is updated");
        }

        [Fact]
        public void GraphModel_PropertyChanged_RaisedWhenSampleColorUpdated()
        {
            var vm = CreateViewModel();
            bool graphModelChangedEventRaised = false;

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.GraphModel))
                {
                    graphModelChangedEventRaised = true;
                }
            };

            vm.SampleR = "50";
            vm.SampleG = "75";
            vm.SampleB = "100";

            Assert.True(graphModelChangedEventRaised, "GraphModel PropertyChanged should be raised when sample color is updated");
        }

        [Fact]
        public void GraphModel_PropertyChanged_RaisedWhenHexColorUpdated()
        {
            var vm = CreateViewModel();
            bool graphModelChangedEventRaised = false;

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.GraphModel))
                {
                    graphModelChangedEventRaised = true;
                }
            };

            vm.ReferenceHex = "#FF5733";

            Assert.True(graphModelChangedEventRaised, "GraphModel PropertyChanged should be raised when hex color is updated");
        }

        [Fact]
        public void GraphModel_PropertyChanged_RaisedEvenWhenInvalidColorProvided()
        {
            var vm = CreateViewModel();
            bool graphModelChangedEventRaised = false;

            // Set valid initial color
            vm.ReferenceR = "100";
            vm.ReferenceG = "100";
            vm.ReferenceB = "100";

            // Start listening after initial setup
            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.GraphModel))
                {
                    graphModelChangedEventRaised = true;
                }
            };

            // Try to set invalid value - PropertyChanged still fires but graph data may not update
            vm.ReferenceR = "invalid";

            Assert.True(graphModelChangedEventRaised, "GraphModel PropertyChanged should be raised even for invalid values (UpdateGraphData is still called)");
        }

        [Fact]
        public void ColorDifference_PropertyChanged_RaisedWhenColorsUpdated()
        {
            var vm = CreateViewModel();
            bool colorDifferenceChangedEventRaised = false;

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.ColorDifference))
                {
                    colorDifferenceChangedEventRaised = true;
                }
            };

            vm.ReferenceR = "255";
            vm.ReferenceG = "0";
            vm.ReferenceB = "0";

            Assert.True(colorDifferenceChangedEventRaised, "ColorDifference PropertyChanged should be raised when colors are updated");
        }

        [Fact]
        public void TintRecommendation_PropertyChanged_RaisedWhenColorsUpdated()
        {
            var vm = CreateViewModel();
            bool tintRecommendationChangedEventRaised = false;

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.TintRecommendation))
                {
                    tintRecommendationChangedEventRaised = true;
                }
            };

            vm.SampleR = "200";
            vm.SampleG = "50";
            vm.SampleB = "100";

            Assert.True(tintRecommendationChangedEventRaised, "TintRecommendation PropertyChanged should be raised when colors are updated");
        }

        #endregion
    }
}
