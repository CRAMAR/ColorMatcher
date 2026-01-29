using System;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Encapsulates a color measurement captured from a spectrophotometer or colorimeter sensor.
    /// 
    /// Represents a single sensor reading with color values, quality metrics, and metadata.
    /// Acts as a data transfer object between sensor implementations and the application logic.
    /// 
    /// <remarks>
    /// **SensorReading Lifecycle**
    /// 
    /// 1. **Creation**: Sensor implementation creates instance via ReadColorAsync()
    ///    - Passes measured RgbColor (required)
    ///    - Optionally sets LabColor (calculated or from sensor)
    ///    - Sets Timestamp (UTC time of measurement)
    /// 
    /// 2. **Enrichment**: Sensor adds metadata
    ///    - DeviceId: Which physical sensor made the measurement
    ///    - QualityScore: Confidence metric (0-100)
    ///    - Metadata: Device-specific info (illumination angle, mode, etc)
    /// 
    /// 3. **Validation**: Application checks quality
    ///    - Examine QualityScore vs requirements
    ///    - Decide to accept or retry measurement
    ///    - Use Timestamp to order measurements in session
    /// 
    /// 4. **Processing**: Application converts to ColorHistoryEntry
    ///    - Extract RGB/LAB colors
    ///    - Store with reference/sample colors
    ///    - Preserve metadata in history notes
    /// 
    /// **Color Representation**
    /// 
    /// SensorReading provides colors in two formats:
    /// 
    /// - **RgbColor**: Device-dependent additive color model
    ///   - Always present (measured directly from sensor)
    ///   - Ranges 0-255 for each component
    ///   - Format understood by UI and graphics systems
    /// 
    /// - **LabColor**: Device-independent perceptually uniform space
    ///   - Optional (may be null if sensor doesn't provide)
    ///   - More useful for color difference calculations
    ///   - If null, application can convert via ColorSpaceConverter.RgbToLab()
    /// 
    /// **Quality Metrics**
    /// 
    /// QualityScore (0-100) indicates measurement confidence:
    /// - 100: Perfect, ideal conditions (rare)
    /// - 75-90: Good, acceptable for most applications
    /// - 50-75: Fair, some uncertainty
    /// - &lt;50: Poor, potentially unreliable
    /// 
    /// Typical quality variations:
    /// - Surface quality: Smooth surfaces score higher than textured
    /// - Lighting: Stable lighting scores higher than variable
    /// - Calibration: Post-calibration measurements score higher
    /// - Angle: Measurements within 45° of normal score higher
    /// 
    /// **Metadata Field**
    /// 
    /// Device-specific sensor metadata examples:
    /// - "Illumination: D65, 2° Observer, 45° angle"
    /// - "Mode: Reflectance, Averaged 3 measurements"
    /// - "Calibration offset: +2.5%"
    /// - "Reading #42 in session" (for tracking order)
    /// </remarks>
    /// </summary>
    public class SensorReading
    {
        /// <summary>
        /// The RGB color captured by the sensor.
        /// 
        /// Represents the physical color measurement in the RGB color space (additive model).
        /// Values range 0-255 for each R, G, B component.
        /// 
        /// This is the primary color representation and is always provided by sensor implementations.
        /// Device-dependent: Same physical color may yield slightly different RGB values
        /// on different sensor models or with different illumination standards.
        /// 
        /// Required - never null. Passed to constructor.
        /// </summary>
        public RgbColor RgbColor { get; set; }

        /// <summary>
        /// Optional LAB color representation of the measurement.
        /// 
        /// Perceptually uniform, device-independent color space:
        /// - L* (0-100): Lightness
        /// - a* (-128 to +127): Red-green axis
        /// - b* (-128 to +127): Yellow-blue axis
        /// 
        /// May be provided by sensor (if it performs LAB conversion) or can be calculated
        /// by application via ColorSpaceConverter.RgbToLab(RgbColor).
        /// 
        /// Useful for color difference calculations and comparison operations.
        /// Null if not provided by sensor and not calculated by application.
        /// </summary>
        public LabColor? LabColor { get; set; }

        /// <summary>
        /// Timestamp when the color measurement was captured (UTC).
        /// 
        /// Automatically set to DateTime.UtcNow in constructor.
        /// Used for:
        /// - Ordering measurements in a session
        /// - Filtering measurements by time window
        /// - Correlating with external events
        /// - Session audit trail
        /// 
        /// Always in UTC to avoid timezone ambiguity in cloud/networked scenarios.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Sensor device identifier (serial number, MAC address, device name, etc).
        /// 
        /// Used to distinguish which physical sensor made the measurement when multiple
        /// sensors are in use. Examples:
        /// - "STUB-000001" (stub for testing)
        /// - "NIX-ABC123DEF456" (NIX Mini 3 serial number)
        /// - "00:1A:2B:3C:4D:5E" (Bluetooth MAC address)
        /// 
        /// Optional - may be null if sensor doesn't provide identification.
        /// Set by sensor implementation, not by user.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Sensor-provided quality/confidence metric (0-100 scale).
        /// 
        /// Indicates measurement confidence based on environmental factors, sensor calibration state,
        /// and measurement conditions.
        /// 
        /// Interpretation:
        /// - 100: Optimal conditions, maximum confidence
        /// - 75-90: Good conditions, acceptable for normal use
        /// - 50-75: Fair conditions, some uncertainty
        /// - &lt;50: Poor conditions, potentially unreliable
        /// 
        /// Optional - may be null if sensor doesn't provide quality metrics.
        /// Used by ReadColorWithValidationAsync() to decide if retry is needed.
        /// Example: If minimumQualityScore=70 and QualityScore=65, ReadColorWithValidationAsync()
        /// will retry the measurement.
        /// </summary>
        public int? QualityScore { get; set; }

        /// <summary>
        /// Optional device-specific metadata about the measurement.
        /// 
        /// Device implementation may include contextual information:
        /// - Illumination standard: "D65, 2° Observer, 45° measurement angle"
        /// - Measurement mode: "Reflectance, 3-measurement average"
        /// - Device settings: "Calibration offset +2.5%, UV filter on"
        /// - Session tracking: "Measurement #42 of 100 in current session"
        /// 
        /// Free-form string, format depends on sensor implementation.
        /// Not parsed by application; used for logging and debugging.
        /// May be null if sensor doesn't provide metadata.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Creates a new SensorReading with the specified RGB color.
        /// 
        /// Automatically sets:
        /// - Timestamp to current UTC time
        /// - LabColor to null (can be set later or calculated by application)
        /// 
        /// <param name="rgbColor">Measured RGB color (required, cannot be null)</param>
        /// <exception cref="ArgumentNullException">If rgbColor is null</exception>
        /// </summary>
        public SensorReading(RgbColor rgbColor)
        {
            RgbColor = rgbColor ?? throw new ArgumentNullException(nameof(rgbColor));
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new SensorReading with RGB and optional LAB color representations.
        /// 
        /// Convenience constructor for sensor implementations that already have LAB conversion.
        /// Automatically sets Timestamp to current UTC time.
        /// 
        /// <param name="rgbColor">Measured RGB color (required)</param>
        /// <param name="labColor">Optional corresponding LAB color (can be null)</param>
        /// </summary>
        public SensorReading(RgbColor rgbColor, LabColor? labColor) : this(rgbColor)
        {
            LabColor = labColor;
        }

    }
}
