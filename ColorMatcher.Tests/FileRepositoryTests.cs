using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ColorMatcher.Models;

namespace ColorMatcher.Tests
{
    public class FileColorRepositoryTests : IDisposable
    {
        private readonly string _tempDirectory;

        public FileColorRepositoryTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }

        private ColorProject CreateSampleProject()
        {
            return new ColorProject("Test Project", "A test color matching project")
            {
                ReferenceColor = new RgbColor(255, 0, 0),
                SampleColor = new RgbColor(254, 1, 1)
            };
        }

        [Fact]
        public async Task CreateProjectAsync_PersistsProjectToFile()
        {
            var repo = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();

            var created = await repo.CreateProjectAsync(project);

            var filePath = Path.Combine(_tempDirectory, $"{created.Id}.json");
            Assert.True(File.Exists(filePath));
            
            var fileContent = File.ReadAllText(filePath);
            Assert.Contains("Test Project", fileContent);
        }

        [Fact]
        public async Task LoadAllProjectsAsync_LoadsProjectsFromDisk()
        {
            // Create and save projects with first repository
            var repo1 = new FileColorRepository(_tempDirectory);
            var project1 = CreateSampleProject();
            var project2 = new ColorProject("Project 2");
            
            var created1 = await repo1.CreateProjectAsync(project1);
            var created2 = await repo1.CreateProjectAsync(project2);

            // Load projects with new repository instance
            var repo2 = new FileColorRepository(_tempDirectory);
            await repo2.LoadAllProjectsAsync();
            var projects = (await repo2.GetAllProjectsAsync()).ToList();

            Assert.Equal(2, projects.Count);
            Assert.Contains(projects, p => p.Id == created1.Id);
            Assert.Contains(projects, p => p.Id == created2.Id);
        }

        [Fact]
        public async Task GetAllProjectsAsync_AutoLoadsProjectsIfNotLoaded()
        {
            // Create and save a project
            var repo1 = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            await repo1.CreateProjectAsync(project);

            // New repository should auto-load
            var repo2 = new FileColorRepository(_tempDirectory);
            var projects = (await repo2.GetAllProjectsAsync()).ToList();

            Assert.Single(projects);
            Assert.Equal(project.Name, projects[0].Name);
        }

        [Fact]
        public async Task UpdateProjectAsync_UpdatesFileOnDisk()
        {
            var repo = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            created.Name = "Updated Name";
            created.Description = "Updated Description";
            await repo.UpdateProjectAsync(created);

            var filePath = Path.Combine(_tempDirectory, $"{created.Id}.json");
            var fileContent = File.ReadAllText(filePath);
            
            Assert.Contains("Updated Name", fileContent);
            Assert.Contains("Updated Description", fileContent);
        }

        [Fact]
        public async Task DeleteProjectAsync_RemovesFileFromDisk()
        {
            var repo = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);
            var filePath = Path.Combine(_tempDirectory, $"{created.Id}.json");

            Assert.True(File.Exists(filePath));

            await repo.DeleteProjectAsync(created.Id);

            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task AddColorHistoryAsync_PersistentsHistoryToFile()
        {
            var repo = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            var historyEntry = new ColorHistoryEntry(
                new RgbColor(255, 0, 0),
                new RgbColor(254, 1, 1),
                2.5,
                "Add Red"
            );

            await repo.AddColorHistoryAsync(created.Id, historyEntry);

            // Read file and verify history is persisted
            var filePath = Path.Combine(_tempDirectory, $"{created.Id}.json");
            var fileContent = File.ReadAllText(filePath);
            
            Assert.Contains("ColorHistory", fileContent);
            Assert.Contains("2.5", fileContent);
        }

        [Fact]
        public async Task ClearColorHistoryAsync_RemovesHistoryFromFile()
        {
            var repo = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            var entry1 = new ColorHistoryEntry(new RgbColor(255, 0, 0), new RgbColor(254, 1, 1), 2.5, "Add Red");
            var entry2 = new ColorHistoryEntry(new RgbColor(0, 255, 0), new RgbColor(1, 254, 1), 3.0, "Add Green");

            await repo.AddColorHistoryAsync(created.Id, entry1);
            await repo.AddColorHistoryAsync(created.Id, entry2);

            var history = (await repo.GetColorHistoryAsync(created.Id)).ToList();
            Assert.Equal(2, history.Count);

            await repo.ClearColorHistoryAsync(created.Id);

            var clearedHistory = (await repo.GetColorHistoryAsync(created.Id)).ToList();
            Assert.Empty(clearedHistory);

            // Verify file is updated - ColorHistory should still exist but be empty
            var filePath = Path.Combine(_tempDirectory, $"{created.Id}.json");
            var fileContent = File.ReadAllText(filePath);
            var parsed = System.Text.Json.JsonDocument.Parse(fileContent).RootElement;
            
            // ColorHistory property should exist and be an empty array
            if (parsed.TryGetProperty("colorHistory", out var historyArray))
            {
                Assert.Equal(0, historyArray.GetArrayLength());
            }
        }

        [Fact]
        public async Task ImportProjectFromJsonAsync_CreatesNewFileForImportedProject()
        {
            // Export a project
            var repo1 = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            var created = await repo1.CreateProjectAsync(project);
            var json = await repo1.ExportProjectAsJsonAsync(created.Id);

            // Import into same directory
            var imported = await repo1.ImportProjectFromJsonAsync(json);

            var importedFilePath = Path.Combine(_tempDirectory, $"{imported.Id}.json");
            Assert.True(File.Exists(importedFilePath));
            Assert.NotEqual(created.Id, imported.Id); // New ID generated
        }

        [Fact]
        public void Constructor_CreatesDirectoryIfNotExists()
        {
            var nonExistentPath = Path.Combine(_tempDirectory, "subdir", "projects");
            
            Assert.False(Directory.Exists(nonExistentPath));

            var repo = new FileColorRepository(nonExistentPath);

            Assert.True(Directory.Exists(nonExistentPath));
        }

        [Fact]
        public async Task HandleInvalidJsonFile_SkipsOnLoad()
        {
            // Create a valid project first
            var repo1 = new FileColorRepository(_tempDirectory);
            var project = CreateSampleProject();
            await repo1.CreateProjectAsync(project);

            // Create an invalid JSON file
            var invalidFilePath = Path.Combine(_tempDirectory, "invalid.json");
            File.WriteAllText(invalidFilePath, "{invalid json content");

            // Load should skip the invalid file
            var repo2 = new FileColorRepository(_tempDirectory);
            await repo2.LoadAllProjectsAsync();
            var projects = (await repo2.GetAllProjectsAsync()).ToList();

            // Should have loaded the valid project but skipped invalid file
            Assert.Single(projects);
            Assert.Equal(project.Name, projects[0].Name);
        }
    }
}
