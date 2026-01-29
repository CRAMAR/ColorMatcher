using System.Collections.Generic;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Repository interface for persisting color matching projects and history.
    /// 
    /// Implements the Repository pattern to abstract data persistence from business logic.
    /// Supports full CRUD (Create, Read, Update, Delete) operations on ColorProject entities
    /// with automatic color history tracking. All operations are asynchronous to support
    /// various backend implementations (file I/O, database, cloud services).
    /// </summary>
    /// <remarks>
    /// Repository Pattern Benefits:
    /// - Abstraction: Business logic doesn't depend on storage implementation
    /// - Testability: Can be mocked with InMemoryColorRepository for testing
    /// - Flexibility: Easy to swap between file, database, or cloud storage
    /// - Consistency: Unified interface across all storage mechanisms
    /// 
    /// Implementations:
    /// - InMemoryColorRepository: For testing and development without persistence
    /// - FileColorRepository: JSON files in local/network filesystem
    /// 
    /// Async Methods:
    /// All methods are async Task to support:
    /// - Non-blocking I/O operations (file reads/writes)
    /// - Potential network operations (future cloud storage)
    /// - UI responsiveness in Avalonia application
    /// 
    /// Usage Example:
    /// <code>
    /// // Dependency injection setup
    /// IColorRepository repo = new FileColorRepository("/app/projects");
    /// 
    /// // Create new project
    /// var project = new ColorProject 
    /// { 
    ///     Id = Guid.NewGuid().ToString(),
    ///     Name = "Ferrari Red",
    ///     ReferenceColor = new RgbColor(255, 0, 10)
    /// };
    /// await repo.CreateProjectAsync(project);
    /// 
    /// // Add color match to history
    /// var entry = new ColorHistoryEntry(
    ///     project.ReferenceColor, 
    ///     new RgbColor(250, 5, 15),
    ///     deltaE: 2.5,
    ///     "Slightly more blue"
    /// );
    /// await repo.AddColorHistoryAsync(project.Id, entry);
    /// 
    /// // Save project with updates
    /// await repo.UpdateProjectAsync(project);
    /// </code>
    /// </remarks>
    public interface IColorRepository
    {
        /// <summary>
        /// Creates a new color project and persists it.
        /// </summary>
        /// <param name="project">The color project to create. Must have a unique Id.</param>
        /// <returns>
        /// The created project. May include auto-generated fields like timestamps.
        /// Project is now persisted and can be retrieved via GetProjectAsync.
        /// </returns>
        /// <remarks>
        /// Creates the project in the underlying storage (file, database, etc.).
        /// If a project with the same ID already exists, behavior depends on implementation:
        /// - FileColorRepository: Creates in new file (overwrites existing)
        /// - InMemoryColorRepository: Stores in dictionary
        /// </remarks>
        Task<ColorProject> CreateProjectAsync(ColorProject project);

        /// <summary>
        /// Retrieves a project by its unique identifier.
        /// </summary>
        /// <param name="projectId">The unique project identifier (typically GUID)</param>
        /// <returns>
        /// The color project if found, including full ColorHistory.
        /// Returns null if project with given ID doesn't exist.
        /// </returns>
        /// <remarks>
        /// Loads the complete project from storage including all associated color history.
        /// This is typically the first operation when user opens an existing project.
        /// </remarks>
        Task<ColorProject?> GetProjectAsync(string projectId);

        /// <summary>
        /// Retrieves all projects stored in the repository.
        /// </summary>
        /// <returns>
        /// Collection of all color projects. Empty if no projects exist.
        /// Projects are typically ordered by ModifiedAt (most recent first) for UI display.
        /// </returns>
        /// <remarks>
        /// Used for populating "Recent Projects" list in application.
        /// May be expensive for large numbers of projects - consider pagination for future enhancement.
        /// </remarks>
        Task<IEnumerable<ColorProject>> GetAllProjectsAsync();

        /// <summary>
        /// Updates an existing project with new values.
        /// </summary>
        /// <param name="project">The project with updated field values. Must have existing Id.</param>
        /// <returns>The updated project as persisted</returns>
        /// <remarks>
        /// Overwrites the project in storage with new values.
        /// ModifiedAt timestamp is automatically updated to current time.
        /// ColorHistory list is preserved (to add entries, use AddColorHistoryAsync).
        /// If project doesn't exist, behavior depends on implementation (may throw or create new).
        /// </remarks>
        Task<ColorProject> UpdateProjectAsync(ColorProject project);

        /// <summary>
        /// Deletes a project and all associated data from the repository.
        /// </summary>
        /// <param name="projectId">The project ID to delete</param>
        /// <returns>
        /// True if project was successfully deleted.
        /// False if project with given ID was not found.
        /// </returns>
        /// <remarks>
        /// Removes project file (in file-based implementation) or database record.
        /// This operation is typically permanent and non-recoverable.
        /// All associated color history is also deleted.
        /// </remarks>
        Task<bool> DeleteProjectAsync(string projectId);

        /// <summary>
        /// Adds a new color history entry to a project's history.
        /// </summary>
        /// <param name="projectId">The project ID to add history to</param>
        /// <param name="colorEntry">The color matching attempt to record</param>
        /// <returns>
        /// The created color entry, potentially with auto-generated fields like Id and CreatedAt.
        /// Entry is now persisted and included in project history.
        /// </returns>
        /// <remarks>
        /// Called each time user saves a color match attempt.
        /// Automatically loads project, adds entry, and saves back to storage.
        /// This is the primary method for building color matching history over time.
        /// </remarks>
        Task<ColorHistoryEntry> AddColorHistoryAsync(string projectId, ColorHistoryEntry colorEntry);

        /// <summary>
        /// Retrieves complete color history for a specific project.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>
        /// Collection of color history entries for this project.
        /// Typically ordered by CreatedAt (oldest to newest or reverse).
        /// Empty if project has no history.
        /// </returns>
        /// <remarks>
        /// Used to display color history in DataGrid on UI.
        /// Each entry contains reference color, sample color, ΔE, and recommendation.
        /// </remarks>
        Task<IEnumerable<ColorHistoryEntry>> GetColorHistoryAsync(string projectId);

        /// <summary>
        /// Removes all color history entries for a project.
        /// </summary>
        /// <param name="projectId">The project ID to clear history for</param>
        /// <returns>
        /// True if history was successfully cleared.
        /// False if project not found or already had no history.
        /// </returns>
        /// <remarks>
        /// Called by user "Clear History" action.
        /// Preserves project but resets ColorHistory list to empty.
        /// Project itself (name, reference color, sample color) is preserved.
        /// </remarks>
        Task<bool> ClearColorHistoryAsync(string projectId);

        /// <summary>
        /// Exports a project to JSON format for external use or backup.
        /// </summary>
        /// <param name="projectId">The project ID to export</param>
        /// <returns>
        /// JSON string containing complete project data including all history.
        /// Can be saved to file, sent via email, or stored in version control.
        /// </returns>
        /// <remarks>
        /// Used for:
        /// - Backup/archival purposes
        /// - Sharing projects with others
        /// - Version control (Git) compatibility
        /// - Data portability
        /// 
        /// Recommended JSON structure includes all project fields and complete ColorHistory.
        /// </remarks>
        Task<string> ExportProjectAsJsonAsync(string projectId);

        /// <summary>
        /// Imports a project from JSON format.
        /// </summary>
        /// <param name="jsonData">JSON string containing project data</param>
        /// <returns>The imported project with all data restored</returns>
        /// <remarks>
        /// Inverse operation of ExportProjectAsJsonAsync.
        /// Parses JSON and creates new project in repository.
        /// Used for:
        /// - Restoring from backups
        /// - Receiving shared projects
        /// - Migrating from other systems
        /// 
        /// Typically generates new ID for imported project to avoid conflicts with existing projects.
        /// </remarks>
        Task<ColorProject> ImportProjectFromJsonAsync(string jsonData);
    }
}
