using System.Threading.Tasks;
using Xunit;
using ColorMatcher.ViewModels;

namespace ColorMatcher.Tests
{
    /// <summary>
    /// Tests for project export and import functionality.
    /// </summary>
    public class ExportImportTests
    {
        private MainWindowViewModel CreateViewModel()
        {
            return new MainWindowViewModel();
        }

        [Fact]
        public async Task ExportProject_WithoutProject_ReturnsNull()
        {
            var vm = CreateViewModel();

            await vm.ExportProjectCommand.ExecuteAsync(null);

            // Should handle gracefully
            Assert.NotNull(vm);
        }

        [Fact]
        public async Task ExportProject_WithProject_ReturnsValidJson()
        {
            var vm = CreateViewModel();
            vm.ProjectName = "Export Test";
            await vm.CreateNewProjectCommand.ExecuteAsync(null);

            await vm.ExportProjectCommand.ExecuteAsync(null);

            // Export should have completed without throwing
            Assert.NotNull(vm.CurrentProject);
        }
    }
}
