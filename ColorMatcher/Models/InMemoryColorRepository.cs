using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// In-memory implementation of IColorRepository for development and testing.
    /// All data is stored in memory and lost on application exit.
    /// For production use, implement a file-based or database-backed variant.
    /// </summary>
    public class InMemoryColorRepository : IColorRepository
    {
        private readonly Dictionary<string, ColorProject> _projects = new Dictionary<string, ColorProject>();
        private readonly object _lock = new object();

        public Task<ColorProject> CreateProjectAsync(ColorProject project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            lock (_lock)
            {
                if (_projects.ContainsKey(project.Id))
                    throw new InvalidOperationException($"Project with ID {project.Id} already exists.");

                project.CreatedAt = DateTime.UtcNow;
                project.ModifiedAt = DateTime.UtcNow;
                _projects[project.Id] = project;

                return Task.FromResult(project);
            }
        }

        public Task<ColorProject?> GetProjectAsync(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentNullException(nameof(projectId));

            lock (_lock)
            {
                _projects.TryGetValue(projectId, out var project);
                return Task.FromResult<ColorProject?>(project);
            }
        }

        public Task<IEnumerable<ColorProject>> GetAllProjectsAsync()
        {
            lock (_lock)
            {
                var projects = _projects.Values.OrderByDescending(p => p.ModifiedAt).ToList();
                return Task.FromResult<IEnumerable<ColorProject>>(projects);
            }
        }

        public Task<ColorProject> UpdateProjectAsync(ColorProject project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            lock (_lock)
            {
                if (!_projects.ContainsKey(project.Id))
                    throw new KeyNotFoundException($"Project with ID {project.Id} not found.");

                project.ModifiedAt = DateTime.UtcNow;
                _projects[project.Id] = project;

                return Task.FromResult(project);
            }
        }

        public Task<bool> DeleteProjectAsync(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentNullException(nameof(projectId));

            lock (_lock)
            {
                return Task.FromResult(_projects.Remove(projectId));
            }
        }

        public Task<ColorHistoryEntry> AddColorHistoryAsync(string projectId, ColorHistoryEntry colorEntry)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentNullException(nameof(projectId));
            if (colorEntry == null)
                throw new ArgumentNullException(nameof(colorEntry));

            lock (_lock)
            {
                if (!_projects.ContainsKey(projectId))
                    throw new KeyNotFoundException($"Project with ID {projectId} not found.");

                colorEntry.CreatedAt = DateTime.UtcNow;
                _projects[projectId].ColorHistory.Add(colorEntry);
                _projects[projectId].ModifiedAt = DateTime.UtcNow;

                return Task.FromResult(colorEntry);
            }
        }

        public Task<IEnumerable<ColorHistoryEntry>> GetColorHistoryAsync(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentNullException(nameof(projectId));

            lock (_lock)
            {
                if (!_projects.ContainsKey(projectId))
                    throw new KeyNotFoundException($"Project with ID {projectId} not found.");

                var history = _projects[projectId].ColorHistory
                    .OrderByDescending(h => h.CreatedAt)
                    .ToList();

                return Task.FromResult<IEnumerable<ColorHistoryEntry>>(history);
            }
        }

        public Task<bool> ClearColorHistoryAsync(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentNullException(nameof(projectId));

            lock (_lock)
            {
                if (!_projects.ContainsKey(projectId))
                    throw new KeyNotFoundException($"Project with ID {projectId} not found.");

                _projects[projectId].ColorHistory.Clear();
                _projects[projectId].ModifiedAt = DateTime.UtcNow;

                return Task.FromResult(true);
            }
        }

        public Task<string> ExportProjectAsJsonAsync(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentNullException(nameof(projectId));

            lock (_lock)
            {
                if (!_projects.ContainsKey(projectId))
                    throw new KeyNotFoundException($"Project with ID {projectId} not found.");

                var project = _projects[projectId];
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(project, options);

                return Task.FromResult(json);
            }
        }

        public Task<ColorProject> ImportProjectFromJsonAsync(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
                throw new ArgumentNullException(nameof(jsonData));

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var project = JsonSerializer.Deserialize<ColorProject>(jsonData, options);

                if (project == null)
                    throw new InvalidOperationException("Failed to deserialize project from JSON.");

                // Generate new ID to avoid conflicts on import
                project.Id = Guid.NewGuid().ToString();
                project.CreatedAt = DateTime.UtcNow;
                project.ModifiedAt = DateTime.UtcNow;

                lock (_lock)
                {
                    _projects[project.Id] = project;
                }

                return Task.FromResult(project);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Invalid JSON format for project.", ex);
            }
        }
    }
}
