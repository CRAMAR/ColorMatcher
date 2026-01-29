using System;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Represents a single color matching attempt in project history.
    /// </summary>
    public class ColorHistoryEntry
    {
        /// <summary>
        /// Unique identifier for this history entry
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Reference color at time of this entry
        /// </summary>
        public RgbColor? ReferenceColor { get; set; }

        /// <summary>
        /// Sample color that was evaluated
        /// </summary>
        public RgbColor? SampleColor { get; set; }

        /// <summary>
        /// Delta E (color difference) at time of entry
        /// </summary>
        public double DeltaE { get; set; }

        /// <summary>
        /// Tint recommendation that was provided
        /// </summary>
        public string? TintRecommendation { get; set; }

        /// <summary>
        /// Whether this match was marked as acceptable by user
        /// </summary>
        public bool IsAccepted { get; set; }

        /// <summary>
        /// Timestamp when this entry was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Optional user notes about this match attempt
        /// </summary>
        public string? Notes { get; set; }

        public ColorHistoryEntry()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }

        public ColorHistoryEntry(RgbColor referenceColor, RgbColor sampleColor, double deltaE, string tintRecommendation)
            : this()
        {
            ReferenceColor = referenceColor;
            SampleColor = sampleColor;
            DeltaE = deltaE;
            TintRecommendation = tintRecommendation;
        }
    }
}
