using System.Collections.Generic;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Repository interface for persisting color matching projects and history.
    /// Supports CRUD operations on ColorProject entities with color history tracking.
    /// </summary>
    public interface IColorRepository
    {
        /// <summary>
        /// Creates a new color project and persists it.
        /// </summary>
        /// <param name="project">The color project to create</param>
        /// <returns>The created project with assigned ID</returns>
        Task<ColorProject> CreateProjectAsync(ColorProject project);

        /// <summary>
        /// Retrieves a project by ID.
        /// </summary>
        /// <param name="projectId">The unique project identifier</param>
        /// <returns>The color project, or null if not found</returns>
        Task<ColorProject?> GetProjectAsync(string projectId);

        /// <summary>
        /// Retrieves all projects for the current user.
        /// </summary>
        /// <returns>List of all color projects</returns>
        Task<IEnumerable<ColorProject>> GetAllProjectsAsync();

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        /// <param name="project">The project with updated values</param>
        /// <returns>The updated project</returns>
        Task<ColorProject> UpdateProjectAsync(ColorProject project);

        /// <summary>
        /// Deletes a project by ID.
        /// </summary>
        /// <param name="projectId">The project ID to delete</param>
        /// <returns>True if deletion was successful, false if project not found</returns>
        Task<bool> DeleteProjectAsync(string projectId);

        /// <summary>
        /// Adds a color entry to a project's history.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <param name="colorEntry">The color entry to add</param>
        /// <returns>The created color entry with timestamp</returns>
        Task<ColorHistoryEntry> AddColorHistoryAsync(string projectId, ColorHistoryEntry colorEntry);

        /// <summary>
        /// Retrieves color history for a specific project.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>List of color history entries ordered by date descending</returns>
        Task<IEnumerable<ColorHistoryEntry>> GetColorHistoryAsync(string projectId);

        /// <summary>
        /// Clears all color history for a project.
        /// </summary>
        /// <param name="projectId">The project ID</param>
        /// <returns>True if history was cleared successfully</returns>
        Task<bool> ClearColorHistoryAsync(string projectId);

        /// <summary>
        /// Exports a project to JSON format.
        /// </summary>
        /// <param name="projectId">The project ID to export</param>
        /// <returns>JSON string representation of the project</returns>
        Task<string> ExportProjectAsJsonAsync(string projectId);

        /// <summary>
        /// Imports a project from JSON format.
        /// </summary>
        /// <param name="jsonData">JSON string containing project data</param>
        /// <returns>The imported project</returns>
        Task<ColorProject> ImportProjectFromJsonAsync(string jsonData);
    }
}
