using System;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Interface for interacting with spectrophotometer and colorimeter sensors.
    /// Implementations can support different sensor models (NIX Mini 3, X-Rite, etc).
    /// </summary>
    public interface ISensorReader : IDisposable
    {
        /// <summary>
        /// Gets the friendly name of the sensor device
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Gets the device ID/serial number
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Gets whether the sensor is currently connected and ready
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Attempts to connect to the sensor device.
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Disconnects from the sensor device
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Reads a single color measurement from the sensor.
        /// Device-specific implementation may include calibration checks, retries, etc.
        /// </summary>
        /// <returns>SensorReading containing the measured color</returns>
        /// <exception cref="InvalidOperationException">If sensor is not connected</exception>
        /// <exception cref="TimeoutException">If measurement times out</exception>
        Task<SensorReading> ReadColorAsync();

        /// <summary>
        /// Reads a color measurement with retries and validation.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts (default 3)</param>
        /// <param name="minimumQualityScore">Minimum acceptable quality score 0-100 (default 70)</param>
        /// <returns>SensorReading with validated color measurement</returns>
        Task<SensorReading> ReadColorWithValidationAsync(int maxRetries = 3, int minimumQualityScore = 70);

        /// <summary>
        /// Performs sensor calibration procedure if supported.
        /// May require user interaction (e.g., placing calibration target).
        /// </summary>
        /// <returns>True if calibration successful, false otherwise</returns>
        Task<bool> CalibrateAsync();

        /// <summary>
        /// Gets sensor status information (battery level, last calibration date, etc)
        /// </summary>
        /// <returns>Status string describing current sensor state</returns>
        Task<string> GetStatusAsync();
    }
}
