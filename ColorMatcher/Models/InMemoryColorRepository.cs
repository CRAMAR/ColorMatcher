using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// In-memory implementation of IColorRepository for development, testing, and prototyping.
    /// 
    /// Stores all projects and history in memory using a Dictionary&lt;string, ColorProject&gt;.
    /// All data is lost when the application exits (no persistence to disk).
    /// 
    /// <remarks>
    /// **Use Cases**
    /// 
    /// 1. **Development**: Quick prototyping without file I/O overhead
    /// 2. **Unit Testing**: Mock repository for isolated ViewModel/business logic tests
    /// 3. **Integration Testing**: Test entire workflows without database/file system
    /// 4. **Demo/Prototype**: Temporary projects without persistence requirements
    /// 5. **Performance Testing**: Baseline measurements without storage latency
    /// 
    /// **Characteristics**
    /// 
    /// - **Speed**: Ultra-fast, all operations &lt;1ms
    /// - **Simplicity**: No file system or database concerns
    /// - **Thread-Safety**: Synchronized via internal lock for concurrent access
    /// - **Non-Durable**: All data lost on exit (feature, not bug, for testing)
    /// - **Memory**: Stores complete projects in memory (fine for test datasets)
    /// 
    /// **Data Structure**
    /// 
    /// Uses Dictionary&lt;ProjectId, ColorProject&gt; for O(1) lookups:
    /// - Key: Project GUID (unique identifier)
    /// - Value: Complete ColorProject including all ColorHistoryEntry records
    /// - Thread-safe access via internal lock
    /// 
    /// **Thread Safety**
    /// 
    /// All operations protected by a lock to ensure consistency:
    /// - Prevents corruption when multiple threads access simultaneously
    /// - Serializes modifications (updates wait for lock release)
    /// - Safe for concurrent reads and writes (lock held for entire operation)
    /// 
    /// **Typical Usage in Tests**
    /// 
    /// ```csharp
    /// // Test setup
    /// var repository = new InMemoryColorRepository();
    /// var viewModel = new MainWindowViewModel { Repository = repository };
    /// 
    /// // Create and test
    /// var project = new ColorProject { Id = Guid.NewGuid().ToString(), Name = "Test" };
    /// await repository.CreateProjectAsync(project);
    /// var retrieved = await repository.GetProjectAsync(project.Id);
    /// Assert.NotNull(retrieved);
    /// 
    /// // On test exit: all data discarded (clean state for next test)
    /// ```
    /// 
    /// **FileColorRepository Relationship**
    /// 
    /// FileColorRepository uses InMemoryColorRepository internally:
    /// 1. Loads all projects from disk JSON files
    /// 2. Caches in InMemoryColorRepository for fast access
    /// 3. For new projects, writes to both memory and disk
    /// 
    /// This hybrid design provides both performance (memory cache) and durability (disk backup).
    /// 
    /// **Performance Characteristics**
    /// 
    /// - CreateProjectAsync: O(1) dictionary insert
    /// - GetProjectAsync: O(1) dictionary lookup
    /// - GetAllProjectsAsync: O(n) enumeration of n projects
    /// - DeleteProjectAsync: O(1) dictionary removal
    /// - All operations complete &lt;1ms on modern hardware
    /// </remarks>
    /// </summary>
    public class InMemoryColorRepository : IColorRepository
    {
        /// <summary>
        /// Dictionary storing all projects with ProjectId as key.
        /// Provides O(1) lookup performance for GetProjectAsync().
        /// </summary>
        private readonly Dictionary<string, ColorProject> _projects = new Dictionary<string, ColorProject>();

        /// <summary>
        /// Lock for thread-safe access to the projects dictionary.
        /// Ensures consistency when multiple threads access simultaneously.
        /// </summary>
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
