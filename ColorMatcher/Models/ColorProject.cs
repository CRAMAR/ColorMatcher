using System;
using System.Collections.Generic;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Represents a color matching project with reference and sample colors.
    /// 
    /// A ColorProject is the primary data container in ColorMatcher, holding all information
    /// related to a single color matching session including target (reference) color, evaluation
    /// (sample) colors, and complete history of all attempts.
    /// </summary>
    /// <remarks>
    /// Project Lifecycle:
    /// 1. Created with unique ID, name, description
    /// 2. Reference color set (target paint/gelcoat color to match)
    /// 3. Sample colors added and compared (generates history entries)
    /// 4. Each comparison creates ColorHistoryEntry with delta-E and recommendation
    /// 5. Project persisted to JSON file for later retrieval
    /// 6. Can be updated with new samples or cleared to start over
    /// 
    /// Persistence:
    /// - Projects are stored as JSON files with naming pattern: {ProjectId}.json
    /// - FileColorRepository handles all persistence operations
    /// - ModifiedAt timestamp is automatically updated on save
    /// - ColorHistory is preserved across sessions
    /// 
    /// Example Usage:
    /// <code>
    /// var project = new ColorProject 
    /// { 
    ///     Id = Guid.NewGuid().ToString(),
    ///     Name = "Ferrari Red Paint Match",
    ///     Description = "Matching Ferrari 488 GTB red to gelcoat",
    ///     ReferenceColor = new RgbColor(255, 0, 10),
    ///     CreatedAt = DateTime.Now,
    ///     ModifiedAt = DateTime.Now
    /// };
    /// await repository.CreateProjectAsync(project);
    /// </code>
    /// </remarks>
    public class ColorProject
    {
        /// <summary>
        /// Unique identifier for the project (GUID recommended).
        /// Used for file naming and lookup in repository.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User-friendly project name. Typically describes the item being matched.
        /// Example: "Ferrari Red Paint" or "Yacht Hull Gelcoat"
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Optional detailed description of the project.
        /// Can include notes about the item, location, application method, etc.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Reference color for matching (the target color to match).
        /// Typically represents the actual paint/gelcoat color being analyzed.
        /// Usually obtained from spectrophotometer or visual sample.
        /// </summary>
        public RgbColor? ReferenceColor { get; set; }

        /// <summary>
        /// Current sample color being evaluated for matching.
        /// Represents a candidate color being tested against the reference.
        /// Updated as new samples are evaluated.
        /// </summary>
        public RgbColor? SampleColor { get; set; }

        /// <summary>
        /// Project creation timestamp in UTC.
        /// Set automatically when project is created via repository.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last modification timestamp in UTC.
        /// Updated automatically whenever project is saved.
        /// Useful for sorting and filtering recent projects.
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Complete color matching history for this project.
        /// Contains ColorHistoryEntry for each color comparison attempt.
        /// Ordered chronologically (oldest to newest).
        /// Persisted with project to provide audit trail of matching attempts.
        /// </summary>
        public List<ColorHistoryEntry> ColorHistory { get; set; } = new List<ColorHistoryEntry>();

        /// <summary>
        /// Project-specific metadata (e.g., batch number, surface type)
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public ColorProject()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
            ModifiedAt = DateTime.UtcNow;
        }

        public ColorProject(string name) : this()
        {
            Name = name;
        }

        public ColorProject(string name, string description) : this(name)
        {
            Description = description;
        }
    }
}
