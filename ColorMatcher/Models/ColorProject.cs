using System;
using System.Collections.Generic;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Represents a color matching project with reference and sample colors.
    /// </summary>
    public class ColorProject
    {
        /// <summary>
        /// Unique identifier for the project
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User-friendly project name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Optional project description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Reference color for matching (typically the target paint/gelcoat color)
        /// </summary>
        public RgbColor? ReferenceColor { get; set; }

        /// <summary>
        /// Current sample color being evaluated
        /// </summary>
        public RgbColor? SampleColor { get; set; }

        /// <summary>
        /// Project creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Color matching history for this project
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
