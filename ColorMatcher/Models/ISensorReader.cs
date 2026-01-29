using System;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Interface for interacting with color measurement hardware sensors (spectrophotometers, colorimeters).
    /// 
    /// Defines the contract for sensor hardware integration, enabling color matching applications
    /// to support various spectrophotometer models (NIX Mini 3, X-Rite, Nix Color, etc.) through
    /// pluggable implementations.
    /// 
    /// <remarks>
    /// **Sensor Abstraction Pattern**
    /// 
    /// This interface enables the application to:
    /// 1. Support multiple hardware models without changing application code
    /// 2. Use mock/stub implementations for development/testing without hardware
    /// 3. Abstract away device-specific communication protocols (USB, Bluetooth, networked)
    /// 4. Provide consistent color measurement API regardless of sensor model
    /// 
    /// **Typical Usage Pattern**
    /// 
    /// 1. **Connection Phase**
    ///    - Detect available sensors (device-specific, not in interface)
    ///    - Call ConnectAsync() to establish communication
    ///    - Check IsConnected property to verify ready state
    /// 
    /// 2. **Optional Calibration**
    ///    - Call CalibrateAsync() if required (may need user interaction)
    ///    - Verify calibration via GetStatusAsync()
    /// 
    /// 3. **Measurement Phase**
    ///    - Call ReadColorAsync() to capture single color reading
    ///    - Or use ReadColorWithValidationAsync() for automatic quality checks and retries
    ///    - Get SensorReading with RGB color, quality metrics, timestamps
    /// 
    /// 4. **Status Monitoring**
    ///    - Call GetStatusAsync() periodically for battery/calibration info
    /// 
    /// 5. **Disconnection**
    ///    - Call DisconnectAsync() when done
    ///    - Or dispose of sensor to trigger cleanup via IDisposable
    /// 
    /// **Error Handling**
    /// 
    /// Methods throw specific exceptions:
    /// - InvalidOperationException: Sensor not connected or not ready
    /// - TimeoutException: Hardware did not respond in time
    /// - Device-specific exceptions: Hardware communication failures
    /// 
    /// **Implementation Examples**
    /// 
    /// - StubSensorReader: Simulates realistic color readings for development
    /// - NixMini3Reader: Real hardware supporting NIX Mini 3 spectrophotometer (example impl)
    /// - XRiteColorReaderReader: Real hardware supporting X-Rite models (example impl)
    /// </remarks>
    /// </summary>
    public interface ISensorReader : IDisposable
    {
        /// <summary>
        /// Gets the friendly name/model of the sensor device.
        /// 
        /// Examples: "NIX Mini 3", "X-Rite i1 Display", "Nix Color Pro"
        /// Used for UI display and user identification of which sensor is connected.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Gets the unique device identifier (serial number, MAC address, etc).
        /// 
        /// Used for logging, session tracking, and distinguishing between multiple
        /// sensors when available. Examples: "STUB-000001", "NIX-A1B2C3D4E5F6", "00:1A:2B:3C:4D:5E"
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Gets whether the sensor is currently connected and ready for measurements.
        /// 
        /// True after successful ConnectAsync() call. Set to false after DisconnectAsync().
        /// Used to enable/disable sensor-dependent UI controls and to guard against reading
        /// from disconnected sensors.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Attempts to establish communication with the sensor device.
        /// 
        /// Device-specific implementation handles:
        /// - USB/Bluetooth/network discovery and connection
        /// - Initial handshake and version negotiation
        /// - Resource allocation
        /// - Sensor initialization
        /// 
        /// This is typically the first method called after creating a sensor instance.
        /// Connection attempts should have reasonable timeout (recommended &lt;10 seconds).
        /// 
        /// <returns>True if connection successful and sensor ready for measurements; false if connection failed</returns>
        /// 
        /// <remarks>
        /// Throws InvalidOperationException if already connected. Implementers may allow
        /// reconnection after DisconnectAsync(), but this is device-specific.
        /// </remarks>
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the sensor device and releases hardware resources.
        /// 
        /// Device-specific implementation handles:
        /// - Closing communication ports/connections
        /// - Releasing USB/hardware handles
        /// - Saving session data if applicable
        /// - Graceful shutdown procedures
        /// 
        /// Safe to call multiple times (should be idempotent).
        /// Sets IsConnected to false.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Reads a single color measurement from the sensor.
        /// 
        /// Returns one SensorReading containing:
        /// - RGB color captured by sensor
        /// - Quality metrics from sensor (if available)
        /// - Timestamp of measurement
        /// - Optional device metadata (illumination angle, measurement mode, etc)
        /// 
        /// Device-specific implementation may include:
        /// - Calibration verification before measurement
        /// - Single measurement or averaged readings
        /// - Illumination standardization (D65 light, specific angle, etc)
        /// 
        /// Measurement time typically &lt;1 second for spectrophotometers.
        /// 
        /// <returns>SensorReading containing the measured color and metadata</returns>
        /// 
        /// <exception cref="InvalidOperationException">If sensor is not connected (call ConnectAsync first)</exception>
        /// <exception cref="TimeoutException">If sensor does not respond within timeout period</exception>
        /// 
        /// <remarks>
        /// Does NOT perform validation or retries. For applications requiring high-confidence
        /// measurements, use ReadColorWithValidationAsync() instead.
        /// </remarks>
        /// </summary>
        Task<SensorReading> ReadColorAsync();

        /// <summary>
        /// Reads a color measurement with automatic validation, retries, and quality checking.
        /// 
        /// This is the recommended method for production measurement loops. It handles:
        /// - Retrying if measurement quality is below minimumQualityScore
        /// - Short delay between retries (e.g., sensor recalibration)
        /// - Returning best valid measurement found
        /// 
        /// **Quality Score Interpretation**
        /// - 100: Perfect, ideal measurement conditions
        /// - 75-90: Good, acceptable for most applications
        /// - 50-75: Fair, may have slight measurement errors
        /// - &lt;50: Poor, may be unreliable
        /// 
        /// Recommended minimumQualityScore = 70 for color matching applications.
        /// 
        /// <param name="maxRetries">Maximum number of retry attempts. Default: 3.
        /// Recommended range: 1-5. Each retry takes ~100-500ms plus measurement time.</param>
        /// <param name="minimumQualityScore">Minimum acceptable quality score (0-100). Default: 70.
        /// Recommended for general use. Lower values (50) for fast measurement, higher (85+) for precision.</param>
        /// <returns>Best SensorReading found that meets quality requirements, or last reading if none qualify</returns>
        /// 
        /// <exception cref="InvalidOperationException">
        /// Propagated from <see cref="ReadColorAsync"/> if the sensor is not connected (call ConnectAsync first).
        /// </exception>
        /// 
        /// <remarks>
        /// If no reading reaches minimumQualityScore after all retries, returns the last
        /// attempted reading anyway (does not throw exception). This ensures the application
        /// can always proceed with a measurement, even if quality is suboptimal.
        /// </remarks>
        /// </summary>
        Task<SensorReading> ReadColorWithValidationAsync(int maxRetries = 3, int minimumQualityScore = 70);

        /// <summary>
        /// Performs sensor calibration procedure if supported by the device.
        /// 
        /// Many spectrophotometers require periodic calibration against reference standards
        /// (white reference tile, gray reference, light trap, etc.) to maintain accuracy.
        /// 
        /// Device-specific implementation handles:
        /// - Guiding user to place calibration target (if hardware requires)
        /// - Performing calibration measurements
        /// - Verifying calibration success
        /// - Storing calibration data
        /// 
        /// Typical calibration time: 5-30 seconds depending on device.
        /// 
        /// <returns>True if calibration successful; false if calibration failed or not supported</returns>
        /// 
        /// <remarks>
        /// Recommended to call periodically (daily for frequent use, weekly for casual use)
        /// and always before important color measurements. Some devices require user interaction
        /// (placing calibration tile), so this should be called with user awareness.
        /// </remarks>
        /// </summary>
        Task<bool> CalibrateAsync();

        /// <summary>
        /// Gets human-readable status information about the sensor's current state.
        /// 
        /// Returns multiline string with status details such as:
        /// - Connection status
        /// - Battery level (if applicable)
        /// - Last calibration date/time
        /// - Measurement count
        /// - Device firmware version
        /// - Any error conditions or warnings
        /// 
        /// Example output:
        /// "Connected: NIX Mini 3
        ///  Device ID: NIX-123456
        ///  Readings taken: 42
        ///  Battery: 85%
        ///  Last calibration: Today at 10:30 AM
        ///  Status: Ready"
        /// 
        /// <returns>Multiline status string suitable for UI display or logging</returns>
        /// 
        /// <remarks>
        /// Called periodically for monitoring or before important measurements. Low cost
        /// operation typically &lt;100ms. Safe to call even if not connected (returns "Disconnected").
        /// </remarks>
        /// </summary>
        Task<string> GetStatusAsync();
    }
}
