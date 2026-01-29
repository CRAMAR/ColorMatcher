using System;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Represents a color reading from a spectrophotometer or colorimeter sensor.
    /// Contains RGB values and optional LAB values, along with sensor metadata.
    /// </summary>
    public class SensorReading
    {
        /// <summary>
        /// The RGB color captured by the sensor
        /// </summary>
        public RgbColor RgbColor { get; set; }

        /// <summary>
        /// Optional LAB color representation (may be calculated or provided by sensor)
        /// </summary>
        public LabColor? LabColor { get; set; }

        /// <summary>
        /// Timestamp when the reading was taken
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Sensor device identifier (e.g., serial number, device name)
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Sensor-provided quality metric (0-100, 100 = perfect)
        /// </summary>
        public int? QualityScore { get; set; }

        /// <summary>
        /// Optional metadata from the sensor (e.g., illumination angle, measurement mode)
        /// </summary>
        public string? Metadata { get; set; }

        public SensorReading(RgbColor rgbColor)
        {
            RgbColor = rgbColor ?? throw new ArgumentNullException(nameof(rgbColor));
            Timestamp = DateTime.UtcNow;
        }

        public SensorReading(RgbColor rgbColor, LabColor? labColor) : this(rgbColor)
        {
            LabColor = labColor;
        }
    }
}
