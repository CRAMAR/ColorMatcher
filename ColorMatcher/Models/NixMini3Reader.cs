using System;
using System.Linq;
using System.Threading.Tasks;
using ColorMatcher.Models;
#if NET6_0_OR_GREATER && WINDOWS
using NixUniversalSDK;
#endif

namespace ColorMatcher.Models
{
#if NET6_0_OR_GREATER && WINDOWS
    /// <summary>
    /// Hardware implementation of ISensorReader for NIX Mini 3 spectrophotometer.
    /// 
    /// Interfaces with real NIX Mini 3 sensor hardware using the NixUniversalSDK library.
    /// Supports USB and Bluetooth connectivity for high-accuracy color measurements
    /// using spectrophotometric technology with D65 illumination and 2° observer.
    /// 
    /// <remarks>
    /// **Hardware Requirements**
    /// - NIX Mini 3 spectrophotometer device
    /// - Windows 10/11 with Bluetooth support (for wireless connectivity)
    /// - USB connection OR paired Bluetooth device
    /// - Valid NixUniversalSDK license activation code
    /// 
    /// **SDK Integration**
    /// - Uses DeviceScanner to discover nearby NIX devices via USB/Bluetooth
    /// - Manages device connection lifecycle (scan, connect, measure, disconnect)
    /// - Converts SDK measurement data (IMeasurementData) to application SensorReading format
    /// - Handles quality score extraction and RGB color conversion
    /// 
    /// **Connection Flow**
    /// 1. Initialize DeviceScanner
    /// 2. Scan for USB devices (fast, no Bluetooth delay)
    /// 3. Connect to first available device
    /// 4. Query device metadata (serial number, battery, firmware version)
    /// 
    /// **Measurement Quality**
    /// - SDK provides Status byte: 0x01 = success, other = errors
    /// - Quality score derived from measurement status (100 = perfect, 0 = failed)
    /// - Automatic retry logic in ReadColorWithValidationAsync for poor measurements
    /// 
    /// **License Activation**
    /// License is activated once per app domain on first NixMini3Reader instantiation.
    /// Replace the activation code with your licensed key from nixsensor.com.
    /// 
    /// **Conditional Compilation**
    /// This class only compiles for Windows targets (NET6_0_OR_GREATER && WINDOWS).
    /// On Linux/Mac, the application uses StubSensorReader for development/testing.
    /// </remarks>
    /// </summary>
    public class NixMini3Reader : ISensorReader
    {
        /// <summary>
        /// Friendly name displayed in UI and logs.
        /// </summary>
        public string DeviceName => "NIX Mini 3";

        /// <summary>
        /// Unique device identifier (serial number from hardware).
        /// Set to "NIX-UNKNOWN" until device is connected and serial number is read.
        /// </summary>
        public string DeviceId { get; private set; } = "NIX-UNKNOWN";

        /// <summary>
        /// Indicates whether sensor is currently connected and ready for measurements.
        /// True after successful ConnectAsync(), false after DisconnectAsync().
        /// </summary>
        public bool IsConnected { get; private set; }

        private IDeviceCompat? _device;
        private DeviceScanner? _scanner;
        private static bool _licenseActivated = false;

        /// <summary>
        /// Initializes the NIX Mini 3 sensor reader and activates SDK license.
        /// 
        /// License activation occurs once per application domain to comply with SDK requirements.
        /// Uses the activation code provided by Nix Sensor Ltd for your licensed application.
        /// </summary>
        public NixMini3Reader()
        {
            // Activate SDK license once per app domain (required by NixUniversalSDK)
            if (!_licenseActivated)
            {
                // TODO: Replace with your real activation code from nixsensor.com
                // Format: e=<expiry>&n=<name>&u=<uuid>
                string activationCode = "e=1&n=1&u=c2ddfe17669d4806aa7819d721d78c2e";
                LicenseManager.Activate(activationCode);
                _licenseActivated = true;
            }
        }

        /// <summary>
        /// Releases hardware resources and disconnects from sensor.
        /// Safe to call multiple times (idempotent).
        /// </summary>
        public void Dispose()
        {
            if (_device != null)
            {
                try
                {
                    _device.Disconnect();
                    _device.Dispose();
                }
                catch
                {
                    // Suppress exceptions during disposal
                }
                _device = null;
            }
            IsConnected = false;
        }

        /// <summary>
        /// Discovers and connects to a NIX Mini 3 sensor device.
        /// 
        /// Connection sequence:
        /// 1. Initialize DeviceScanner
        /// 2. Scan for USB-attached devices (fast, ~100ms)
        /// 3. Connect to first available device
        /// 4. Read device metadata (serial number, firmware, battery)
        /// 
        /// <returns>True if connection successful; false if no device found or connection failed</returns>
        /// 
        /// <remarks>
        /// Prioritizes USB connections over Bluetooth for speed and reliability.
        /// If no USB device found, could be extended to scan Bluetooth devices using
        /// DeviceScanner.SearchForIdAsync() with timeout.
        /// 
        /// Connection failures may occur due to:
        /// - No NIX device attached/powered on
        /// - Device already connected to another application
        /// - USB driver issues
        /// - Bluetooth pairing required but not completed
        /// </remarks>
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                // Initialize scanner
                _scanner = new DeviceScanner();
                var scannerState = await _scanner.InitializeAsync();

                // Check for USB devices first (faster than Bluetooth scan)
                var usbDevices = await _scanner.ListUsbDevicesAsync();
                _device = usbDevices.FirstOrDefault();

                if (_device == null)
                {
                    // No USB device found
                    return false;
                }

                // Connect to the device
                var connectionStatus = await _device.ConnectAsync();

                // Check if connection was successful
                if (connectionStatus == DeviceStatus.Success)
                {
                    DeviceId = _device.SerialNumber;
                    IsConnected = true;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // Connection failed due to hardware/driver error
                IsConnected = false;
                return false;
            }
        }

        /// <summary>
        /// Disconnects from the sensor and releases hardware resources.
        /// 
        /// Performs graceful shutdown:
        /// - Closes active connection
        /// - Releases USB/Bluetooth handle
        /// - Cleans up device resources
        /// 
        /// Safe to call multiple times. Sets IsConnected to false.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_device != null)
            {
                try
                {
                    _device.Disconnect();
                    _device.Dispose();
                }
                catch
                {
                    // Suppress exceptions during disconnection
                }
                _device = null;
            }
            IsConnected = false;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Captures a single color measurement from the sensor.
        /// 
        /// Measurement process:
        /// 1. Trigger measurement on device (user places sensor on surface)
        /// 2. Wait for spectrophotometer scan completion (~500ms)
        /// 3. Receive measurement data with colorimetry and status
        /// 4. Convert SDK color format to application RGB format
        /// 5. Calculate quality score from measurement status
        /// 
        /// <returns>SensorReading with RGB color, quality score, and timestamp</returns>
        /// 
        /// <exception cref="InvalidOperationException">If sensor not connected (call ConnectAsync first)</exception>
        /// <exception cref="TimeoutException">If sensor does not respond within timeout</exception>
        /// 
        /// <remarks>
        /// Quality scoring:
        /// - 100: Perfect measurement (Status = 0x01)
        /// - 0: Failed measurement (Status != 0x01)
        /// 
        /// The SDK returns measurements in multiple color spaces (XYZ, LAB, RGB, etc.).
        /// This implementation uses ToRgbValue() extension method for sRGB conversion
        /// with D65 illuminant and 2° observer (standard conditions for color matching).
        /// 
        /// For production use, consider adding:
        /// - Temperature compensation checks (_device.TemperatureCompensationEnabled)
        /// - Field calibration status (_device.FieldCalibrationDue)
        /// - Battery level warnings (_device.BatteryLevel < 20)
        /// </remarks>
        /// </summary>
        public async Task<SensorReading> ReadColorAsync()
        {
            if (!IsConnected || _device == null)
                throw new InvalidOperationException("Sensor not connected. Call ConnectAsync() first.");

            // Trigger measurement on device
            var result = await _device.MeasureAsync();

            // Check if measurement was successful
            if (result.Status != CommandStatus.Success || result.Measurements.Count == 0)
            {
                throw new InvalidOperationException($"Measurement failed with status: {result.Status}");
            }

            // Get the first measurement (devices may support multiple scan modes)
            var measurementData = result.Measurements.Values.First();

            // Convert SDK color data to RGB (using D65 illuminant, 2° observer)
            var rgbValue = measurementData.ToRgbValue(ReferenceWhite.D65_2);

            // Convert to byte RGB (0-255 range)
            var rgb = new RgbColor(
                (byte)Math.Clamp(rgbValue.R, 0, 255),
                (byte)Math.Clamp(rgbValue.G, 0, 255),
                (byte)Math.Clamp(rgbValue.B, 0, 255));

            // Calculate quality score from measurement status
            // Status byte: 0x01 = success, others = various error conditions
            int quality = measurementData.Status == 0x01 ? 100 : 0;

            return new SensorReading(rgb, quality, DateTime.Now, DeviceName);
        }

        /// <summary>
        /// Reads color measurement with automatic retry logic for quality assurance.
        /// 
        /// Implements quality-aware measurement loop:
        /// 1. Take measurement
        /// 2. Check if quality meets minimumQualityScore threshold
        /// 3. If quality insufficient, retry up to maxRetries times
        /// 4. Return best quality reading found
        /// 
        /// <param name="maxRetries">Maximum retry attempts (default: 3, range: 1-5 recommended)</param>
        /// <param name="minimumQualityScore">Minimum acceptable quality (default: 70, range: 0-100)</param>
        /// <returns>Best SensorReading that meets quality threshold, or last reading if none qualify</returns>
        /// 
        /// <exception cref="InvalidOperationException">If sensor not connected</exception>
        /// 
        /// <remarks>
        /// Typical retry scenarios:
        /// - User moved sensor during measurement
        /// - Surface was reflective/glossy causing glare
        /// - Ambient lighting interference
        /// - Sensor aperture not fully covered by sample
        /// 
        /// Short delay (100ms) between retries allows user to reposition sensor
        /// or adjust measurement conditions without significant time penalty.
        /// 
        /// If no measurement reaches minimumQualityScore after all retries, returns
        /// the best attempt rather than throwing exception, ensuring application
        /// can always proceed (with user warning about low quality).
        /// </remarks>
        /// </summary>
        public async Task<SensorReading> ReadColorWithValidationAsync(int maxRetries = 3, int minimumQualityScore = 70)
        {
            SensorReading? bestReading = null;
            int highestQuality = 0;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var reading = await ReadColorAsync();

                    // If this reading meets quality threshold, return immediately
                    if (reading.QualityScore >= minimumQualityScore)
                        return reading;

                    // Track best reading in case no measurement meets threshold
                    if (reading.QualityScore > highestQuality)
                    {
                        bestReading = reading;
                        highestQuality = reading.QualityScore;
                    }

                    // Short delay before retry (allows user to adjust sensor position)
                    if (attempt < maxRetries - 1)
                        await Task.Delay(100);
                }
                catch
                {
                    // Measurement failed completely, retry
                    if (attempt < maxRetries - 1)
                        await Task.Delay(100);
                    else
                        throw;
                }
            }

            // Return best reading found (may be below quality threshold)
            return bestReading ?? throw new InvalidOperationException("All measurement attempts failed");
        }

        /// <summary>
        /// Performs in-field calibration if supported by the device.
        /// 
        /// NIX Mini 3 supports field calibration using reference tile:
        /// 1. User places sensor on provided white reference tile
        /// 2. Scan tile QR code to get calibration data string
        /// 3. Call RunFieldCalibrationAsync(tileString)
        /// 4. Device performs calibration measurement and stores correction factors
        /// 
        /// <returns>True if calibration supported and successful; false otherwise</returns>
        /// 
        /// <remarks>
        /// **When to calibrate:**
        /// - Daily before important measurements
        /// - When FieldCalibrationDue flag is true
        /// - After significant temperature change (>5°C)
        /// - When measurement drift is suspected
        /// 
        /// **NIX Mini 3 calibration:**
        /// Requires reference tile with QR code. Tile string format is device-specific.
        /// Use _device.IsTileStringValid(tileString) to validate QR code data before calibration.
        /// 
        /// **Current implementation:**
        /// Placeholder returns false (calibration requires user interaction and QR code input).
        /// Production implementation should:
        /// 1. Prompt user to place sensor on tile
        /// 2. Scan/input tile QR code string
        /// 3. Call _device.RunFieldCalibrationAsync(tileString)
        /// 4. Check result.Status == CommandStatus.Success
        /// </remarks>
        /// </summary>
        public async Task<bool> CalibrateAsync()
        {
            if (!IsConnected || _device == null)
                return false;

            // Check if device supports field calibration
            if (!_device.SupportsFieldCalibration)
                return false;

            // Field calibration requires reference tile string from QR code
            // This is a placeholder - real implementation needs user interaction
            // to scan tile QR code and provide tileString parameter

            // Example of how to perform calibration (requires tileString):
            // var tileString = await GetTileStringFromUser(); // Scan QR code
            // if (_device.IsTileStringValid(tileString) == true)
            // {
            //     var result = await _device.RunFieldCalibrationAsync(tileString);
            //     return result.Status == CommandStatus.Success;
            // }

            await Task.CompletedTask;
            return false; // Calibration not implemented (requires UI for tile scanning)
        }

        /// <summary>
        /// Gets comprehensive status information about the sensor's current state.
        /// 
        /// Status information includes:
        /// - Connection state
        /// - Device identification (name, serial number, firmware version)
        /// - Battery level and charging state
        /// - Lifetime scan count
        /// - Calibration status and recommendations
        /// - Temperature compensation status
        /// 
        /// <returns>Multiline status string suitable for UI display or logging</returns>
        /// 
        /// <remarks>
        /// Status check is lightweight operation (&lt;50ms), safe to call frequently
        /// for UI updates or monitoring.
        /// 
        /// Battery monitoring:
        /// - NIX Mini 3 reports battery level 0-100%
        /// - Recommend recharging below 20% to avoid mid-measurement shutdowns
        /// - ExtPowerState indicates if device is currently charging
        /// 
        /// Calibration monitoring:
        /// - FieldCalibrationDue flag set based on time and temperature drift
        /// - Recommend calibration when flag is true
        /// - ReferenceDate shows when last calibration was performed
        /// </remarks>
        /// </summary>
        public async Task<string> GetStatusAsync()
        {
            if (!IsConnected || _device == null)
                return "Status: Disconnected\n\nConnect to a NIX Mini 3 sensor to view device status.";

            var statusLines = new System.Text.StringBuilder();
            statusLines.AppendLine($"Status: Connected");
            statusLines.AppendLine($"Device: {_device.Name}");
            statusLines.AppendLine($"Serial Number: {_device.SerialNumber}");
            statusLines.AppendLine($"Device ID: {_device.Id}");
            statusLines.AppendLine($"Firmware: {_device.FirmwareVersion}");
            statusLines.AppendLine($"Hardware: {_device.HardwareVersion}");

            // Battery status
            if (_device.BatteryLevel.HasValue)
            {
                statusLines.AppendLine($"\nBattery: {_device.BatteryLevel}%");
                if (_device.ExtPowerState)
                    statusLines.AppendLine("Charging: Yes");
                
                if (_device.BatteryLevel < 20)
                    statusLines.AppendLine("⚠ Low battery - recharge recommended");
            }

            // Measurement count
            if (_device.ScanCount.HasValue)
                statusLines.AppendLine($"\nLifetime Scans: {_device.ScanCount:N0}");

            // Calibration status
            if (_device.SupportsFieldCalibration)
            {
                statusLines.AppendLine($"\nCalibration Enabled: {_device.FieldCalibrationEnabled}");
                if (_device.ReferenceDate.HasValue)
                    statusLines.AppendLine($"Last Calibration: {_device.ReferenceDate.Value:g}");
                if (_device.FieldCalibrationDue)
                    statusLines.AppendLine("⚠ Calibration recommended");
            }

            // Temperature compensation
            if (_device.SupportsTemperatureCompensation)
                statusLines.AppendLine($"\nTemp Compensation: {_device.TemperatureCompensationEnabled}");

            await Task.CompletedTask;
            return statusLines.ToString();
        }
    }
#endif
}
