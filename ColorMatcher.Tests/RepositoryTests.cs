using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ColorMatcher.Models;

namespace ColorMatcher.Tests
{
    public class InMemoryColorRepositoryTests
    {
        private InMemoryColorRepository CreateRepository()
        {
            return new InMemoryColorRepository();
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
        public async Task CreateProjectAsync_WithValidProject_ReturnsProjectWithId()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();

            var result = await repo.CreateProjectAsync(project);

            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal(project.Name, result.Name);
            Assert.True(result.CreatedAt != default);
        }

        [Fact]
        public async Task CreateProjectAsync_WithNullProject_ThrowsArgumentNullException()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.CreateProjectAsync(null!));
        }

        [Fact]
        public async Task GetProjectAsync_WithExistingId_ReturnsProject()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            var result = await repo.GetProjectAsync(created.Id);

            Assert.NotNull(result);
            Assert.Equal(created.Id, result.Id);
            Assert.Equal(created.Name, result.Name);
        }

        [Fact]
        public async Task GetProjectAsync_WithNonExistentId_ReturnsNull()
        {
            var repo = CreateRepository();

            var result = await repo.GetProjectAsync("nonexistent-id");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllProjectsAsync_WithMultipleProjects_ReturnsAllOrdered()
        {
            var repo = CreateRepository();
            var project1 = new ColorProject("Project 1");
            var project2 = new ColorProject("Project 2");
            var project3 = new ColorProject("Project 3");

            await repo.CreateProjectAsync(project1);
            await Task.Delay(10); // Small delay to ensure different timestamps
            await repo.CreateProjectAsync(project2);
            await Task.Delay(10);
            await repo.CreateProjectAsync(project3);

            var results = (await repo.GetAllProjectsAsync()).ToList();

            Assert.Equal(3, results.Count);
            Assert.Equal(project3.Id, results[0].Id); // Most recent first
            Assert.Equal(project2.Id, results[1].Id);
            Assert.Equal(project1.Id, results[2].Id);
        }

        [Fact]
        public async Task UpdateProjectAsync_WithValidProject_UpdatesModifiedAt()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);
            var originalModified = created.ModifiedAt;

            await Task.Delay(10);
            created.Name = "Updated Name";
            var updated = await repo.UpdateProjectAsync(created);

            Assert.Equal("Updated Name", updated.Name);
            Assert.True(updated.ModifiedAt > originalModified);
        }

        [Fact]
        public async Task UpdateProjectAsync_WithNonExistentProject_ThrowsKeyNotFoundException()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => repo.UpdateProjectAsync(project));
        }

        [Fact]
        public async Task DeleteProjectAsync_WithExistingProject_RemovesIt()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            var deleted = await repo.DeleteProjectAsync(created.Id);
            var retrieved = await repo.GetProjectAsync(created.Id);

            Assert.True(deleted);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task DeleteProjectAsync_WithNonExistentId_ReturnsFalse()
        {
            var repo = CreateRepository();

            var result = await repo.DeleteProjectAsync("nonexistent-id");

            Assert.False(result);
        }

        [Fact]
        public async Task AddColorHistoryAsync_AddsEntryToProject()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);
            var historyEntry = new ColorHistoryEntry(
                new RgbColor(255, 0, 0),
                new RgbColor(254, 1, 1),
                2.5,
                "Add Red"
            );

            var result = await repo.AddColorHistoryAsync(created.Id, historyEntry);

            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal(2.5, result.DeltaE);
            Assert.True(result.CreatedAt != default);
        }

        [Fact]
        public async Task GetColorHistoryAsync_ReturnsEntriesInDescendingOrder()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            var entry1 = new ColorHistoryEntry(new RgbColor(255, 0, 0), new RgbColor(254, 1, 1), 2.5, "Add Red");
            var entry2 = new ColorHistoryEntry(new RgbColor(0, 255, 0), new RgbColor(1, 254, 1), 3.0, "Add Green");
            var entry3 = new ColorHistoryEntry(new RgbColor(0, 0, 255), new RgbColor(1, 1, 254), 2.8, "Add Blue");

            await repo.AddColorHistoryAsync(created.Id, entry1);
            await Task.Delay(5);
            await repo.AddColorHistoryAsync(created.Id, entry2);
            await Task.Delay(5);
            await repo.AddColorHistoryAsync(created.Id, entry3);

            var history = (await repo.GetColorHistoryAsync(created.Id)).ToList();

            Assert.Equal(3, history.Count);
            Assert.Equal(entry3.Id, history[0].Id); // Most recent first
            Assert.Equal(entry2.Id, history[1].Id);
            Assert.Equal(entry1.Id, history[2].Id);
        }

        [Fact]
        public async Task ClearColorHistoryAsync_RemovesAllHistory()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            await repo.AddColorHistoryAsync(created.Id, 
                new ColorHistoryEntry(new RgbColor(255, 0, 0), new RgbColor(254, 1, 1), 2.5, "Add Red"));
            await repo.AddColorHistoryAsync(created.Id, 
                new ColorHistoryEntry(new RgbColor(0, 255, 0), new RgbColor(1, 254, 1), 3.0, "Add Green"));

            var cleared = await repo.ClearColorHistoryAsync(created.Id);
            var history = (await repo.GetColorHistoryAsync(created.Id)).ToList();

            Assert.True(cleared);
            Assert.Empty(history);
        }

        [Fact]
        public async Task ExportProjectAsJsonAsync_ReturnsValidJson()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);

            var json = await repo.ExportProjectAsJsonAsync(created.Id);

            Assert.NotNull(json);
            Assert.Contains("\"name\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(created.Name!, json);
        }

        [Fact]
        public async Task ImportProjectFromJsonAsync_CreatesNewProjectWithGeneratedId()
        {
            var repo = CreateRepository();
            var project = CreateSampleProject();
            var created = await repo.CreateProjectAsync(project);
            var json = await repo.ExportProjectAsJsonAsync(created.Id);

            var imported = await repo.ImportProjectFromJsonAsync(json);

            Assert.NotNull(imported);
            Assert.NotEqual(created.Id, imported.Id); // New ID generated
            Assert.Equal(created.Name, imported.Name);
            Assert.Equal(created.Description, imported.Description);
        }

        [Fact]
        public async Task ImportProjectFromJsonAsync_WithInvalidJson_ThrowsInvalidOperationException()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                repo.ImportProjectFromJsonAsync("{invalid json"));
        }
    }
}
