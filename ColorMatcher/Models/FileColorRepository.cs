using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// File-based implementation of IColorRepository that persists projects to JSON files.
    /// Each project is stored as a separate JSON file in the specified directory.
    /// </summary>
    public class FileColorRepository : IColorRepository
    {
        private readonly string _projectsDirectory;
        private readonly InMemoryColorRepository _memoryRepository;
        private readonly object _lock = new object();
        private bool _isLoaded = false;

        /// <summary>
        /// Creates a new FileColorRepository with the specified directory path.
        /// The directory will be created if it doesn't exist.
        /// </summary>
        /// <param name="projectsDirectory">Path to directory where projects will be stored</param>
        public FileColorRepository(string projectsDirectory)
        {
            if (string.IsNullOrWhiteSpace(projectsDirectory))
                throw new ArgumentNullException(nameof(projectsDirectory));

            _projectsDirectory = projectsDirectory;
            _memoryRepository = new InMemoryColorRepository();

            // Create directory if it doesn't exist
            Directory.CreateDirectory(_projectsDirectory);
        }

        /// <summary>
        /// Loads all projects from disk into memory.
        /// Should be called once on application startup.
        /// </summary>
        public async Task LoadAllProjectsAsync()
        {
            lock (_lock)
            {
                if (_isLoaded)
                    return;

                try
                {
                    var projectFiles = Directory.GetFiles(_projectsDirectory, "*.json");
                    foreach (var file in projectFiles)
                    {
                        try
                        {
                            var json = File.ReadAllText(file);
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var project = JsonSerializer.Deserialize<ColorProject>(json, options);
                            
                            if (project != null)
                            {
                                _memoryRepository.CreateProjectAsync(project).GetAwaiter().GetResult();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load project from {file}: {ex.Message}");
                        }
                    }

                    _isLoaded = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load projects from directory: {ex.Message}");
                }
            }
        }

        private string GetProjectFilePath(string projectId)
        {
            // Sanitize filename to prevent path traversal
            var safeName = System.Text.RegularExpressions.Regex.Replace(projectId, @"[^a-zA-Z0-9_-]", "_");
            return Path.Combine(_projectsDirectory, $"{safeName}.json");
        }

        private async Task SaveProjectToFileAsync(ColorProject project)
        {
            var filePath = GetProjectFilePath(project.Id);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(project, options);
            
            await File.WriteAllTextAsync(filePath, json);
        }

        private void DeleteProjectFile(string projectId)
        {
            var filePath = GetProjectFilePath(projectId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public async Task<ColorProject> CreateProjectAsync(ColorProject project)
        {
            var result = await _memoryRepository.CreateProjectAsync(project);
            await SaveProjectToFileAsync(result);
            return result;
        }

        public async Task<ColorProject?> GetProjectAsync(string projectId)
        {
            return await _memoryRepository.GetProjectAsync(projectId);
        }

        public async Task<IEnumerable<ColorProject>> GetAllProjectsAsync()
        {
            if (!_isLoaded)
            {
                await LoadAllProjectsAsync();
            }

            return await _memoryRepository.GetAllProjectsAsync();
        }

        public async Task<ColorProject> UpdateProjectAsync(ColorProject project)
        {
            var result = await _memoryRepository.UpdateProjectAsync(project);
            await SaveProjectToFileAsync(result);
            return result;
        }

        public async Task<bool> DeleteProjectAsync(string projectId)
        {
            lock (_lock)
            {
                DeleteProjectFile(projectId);
            }

            return await _memoryRepository.DeleteProjectAsync(projectId);
        }

        public async Task<ColorHistoryEntry> AddColorHistoryAsync(string projectId, ColorHistoryEntry colorEntry)
        {
            var result = await _memoryRepository.AddColorHistoryAsync(projectId, colorEntry);
            
            var project = await _memoryRepository.GetProjectAsync(projectId);
            if (project != null)
            {
                await SaveProjectToFileAsync(project);
            }

            return result;
        }

        public async Task<IEnumerable<ColorHistoryEntry>> GetColorHistoryAsync(string projectId)
        {
            return await _memoryRepository.GetColorHistoryAsync(projectId);
        }

        public async Task<bool> ClearColorHistoryAsync(string projectId)
        {
            var result = await _memoryRepository.ClearColorHistoryAsync(projectId);
            
            var project = await _memoryRepository.GetProjectAsync(projectId);
            if (project != null)
            {
                await SaveProjectToFileAsync(project);
            }

            return result;
        }

        public async Task<string> ExportProjectAsJsonAsync(string projectId)
        {
            return await _memoryRepository.ExportProjectAsJsonAsync(projectId);
        }

        public async Task<ColorProject> ImportProjectFromJsonAsync(string jsonData)
        {
            var result = await _memoryRepository.ImportProjectFromJsonAsync(jsonData);
            await SaveProjectToFileAsync(result);
            return result;
        }
    }
}
