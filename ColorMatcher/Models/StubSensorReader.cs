using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Mock/stub implementation of ISensorReader for development and testing without physical hardware.
    /// 
    /// This implementation simulates a realistic spectrophotometer (NIX Mini 3 model) by:
    /// - Generating plausible simulated color readings
    /// - Supporting quality score variations to test validation logic
    /// - Allowing manual override for deterministic testing
    /// - Maintaining session history of readings
    /// - Simulating realistic measurement patterns and delays
    /// 
    /// <remarks>
    /// **Use Cases**
    /// 
    /// 1. **Development**: Develop and test color matching UI without physical hardware
    /// 2. **Unit Testing**: Mock sensor behavior for ViewModel and business logic tests
    /// 3. **Demo/Prototype**: Quickly prototype sensor integration without hardware setup
    /// 4. **CI/CD**: Run automated tests in build pipelines without sensor dependencies
    /// 
    /// **Realistic Simulation Features**
    /// 
    /// - **Color Clustering**: Generates colors grouped around common paint colors (red, green, blue, gray)
    ///   rather than purely random colors, matching typical color matching use cases
    /// 
    /// - **Quality Score Distribution**: 80% "good" (75-100), 20% "fair" (50-75) to simulate
    ///   realistic environmental variations (dust, lighting, angle)
    /// 
    /// - **Async Behavior**: All methods are truly async (return Tasks) to match real sensor behavior
    /// 
    /// - **Device Metadata**: Reports realistic device name, ID, and status simulating a real sensor
    /// 
    /// - **Session Tracking**: Maintains reading history and count for audit trails
    /// 
    /// **Manual Override for Testing**
    /// 
    /// Use SetNextReadingOverride() to force specific colors for deterministic tests:
    /// 
    /// ```csharp
    /// var sensor = new StubSensorReader();
    /// await sensor.ConnectAsync();
    /// sensor.SetNextReadingOverride(new RgbColor(255, 0, 0)); // Force red
    /// var reading = await sensor.ReadColorAsync(); // Returns red
    /// // reading.RgbColor will be (255, 0, 0)
    /// ```
    /// 
    /// After returning the overridden color, reverts to random generation for subsequent calls.
    /// 
    /// **Limitations**
    /// 
    /// - Does not simulate hardware communication failures or timeouts
    /// - CalibrateAsync() always succeeds (doesn't simulate calibration issues)
    /// - Doesn't model sensor drift or measurement variation over time
    /// - Battery simulation is hardcoded (always ~85%)
    /// 
    /// These limitations are acceptable for development and testing purposes.
    /// For production, real sensor implementations should handle failure scenarios.
    /// </remarks>
    /// </summary>
    public class StubSensorReader : ISensorReader
    {
        private readonly Random _random = new();
        private readonly List<SensorReading> _readingHistory = new();
        private bool _isConnected = false;
        private int _readingCount = 0;
        private RgbColor? _overrideColor;

        /// <summary>
        /// Friendly display name for this simulated sensor (NIX Mini 3 model).
        /// Used in UI and logging to identify the sensor source.
        /// </summary>
        public string DeviceName => "NIX Mini 3 (Stub)";

        /// <summary>
        /// Unique device identifier for this stub sensor.
        /// In real sensors, this would be the serial number or MAC address.
        /// </summary>
        public string DeviceId => "STUB-000001";

        /// <summary>
        /// Indicates whether the sensor is currently connected and ready for measurements.
        /// Set to true by ConnectAsync(), false by DisconnectAsync().
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Sets the next color to be returned by ReadColorAsync.
        /// 
        /// Useful for deterministic testing where you need specific color values.
        /// After the override color is returned once, reverts to random color generation.
        /// 
        /// Example:
        /// ```csharp
        /// sensor.SetNextReadingOverride(new RgbColor(255, 87, 51)); // Ferrari red
        /// var reading = await sensor.ReadColorAsync();
        /// // reading.RgbColor will be exactly (255, 87, 51)
        /// ```
        /// 
        /// <param name="color">RGB color to return on next read (cannot be null)</param>
        /// <exception cref="ArgumentNullException">If color is null</exception>
        /// </summary>
        public void SetNextReadingOverride(RgbColor color)
        {
            _overrideColor = color ?? throw new ArgumentNullException(nameof(color));
        }

        /// <summary>
        /// Gets read-only list of all color readings captured during this session.
        /// 
        /// Maintains complete audit trail of measurements in chronological order.
        /// Useful for session analysis, debugging, and verification testing.
        /// 
        /// Example:
        /// ```csharp
        /// var allReadings = sensor.ReadingHistory;
        /// var averageQuality = allReadings.Average(r => r.QualityScore ?? 0);
        /// ```
        /// </summary>
        public IReadOnlyList<SensorReading> ReadingHistory => _readingHistory.AsReadOnly();

        /// <summary>
        /// Simulates connecting to the sensor.
        /// 
        /// Initializes internal state for measurement session:
        /// - Sets IsConnected to true
        /// - Resets measurement counter to 0
        /// - Clears previous session history
        /// 
        /// Always succeeds immediately (no simulated connection failures).
        /// 
        /// <returns>Task completed with true (always succeeds)</returns>
        /// </summary>
        public Task<bool> ConnectAsync()
        {
            _isConnected = true;
            _readingCount = 0;
            _readingHistory.Clear();
            return Task.FromResult(true);
        }

        /// <summary>
        /// Simulates disconnecting from the sensor.
        /// 
        /// Sets IsConnected to false. Preserves ReadingHistory for session review.
        /// Safe to call multiple times (idempotent).
        /// </summary>
        public Task DisconnectAsync()
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a simulated color reading.
        /// 
        /// Process:
        /// 1. Verify sensor is connected (throws if not)
        /// 2. Get color (override if set, or generate realistic random color)
        /// 3. Create SensorReading with color, quality score, metadata
        /// 4. Convert RGB to LAB color space
        /// 5. Add to ReadingHistory for audit trail
        /// 6. Increment reading counter
        /// 
        /// <returns>Task completed with a SensorReading containing simulated color and metadata</returns>
        /// <exception cref="InvalidOperationException">If sensor is not connected</exception>
        /// </summary>
        public Task<SensorReading> ReadColorAsync()
        {
            if (!_isConnected)
                throw new InvalidOperationException("Sensor is not connected. Call ConnectAsync first.");

            RgbColor rgbColor;

            if (_overrideColor != null)
            {
                rgbColor = _overrideColor;
                _overrideColor = null;
            }
            else
            {
                // Generate a realistic random color with some constraints
                rgbColor = GenerateRealisticColor();
            }

            var reading = new SensorReading(rgbColor)
            {
                DeviceId = DeviceId,
                QualityScore = GenerateQualityScore(),
                Metadata = $"Reading #{++_readingCount}",
                LabColor = ColorSpaceConverter.RgbToLab(rgbColor)
            };

            _readingHistory.Add(reading);
            return Task.FromResult(reading);
        }

        /// <summary>
        /// Returns a validated color reading with automatic retry and quality checking.
        /// 
        /// Process:
        /// 1. Loop up to maxRetries times
        /// 2. Call ReadColorAsync() to get measurement
        /// 3. Check if QualityScore >= minimumQualityScore
        /// 4. If valid, return immediately
        /// 5. If not valid and retries remain, pause 100ms and try again
        /// 6. Return last reading (even if below quality threshold)
        /// 
        /// This ensures the application always gets a reading, even if quality is suboptimal.
        /// 
        /// <param name="maxRetries">Number of retry attempts (default 3)</param>
        /// <param name="minimumQualityScore">Quality threshold 0-100 (default 70)</param>
        /// <returns>Task completed with first reading that meets quality, or last reading if none do</returns>
        /// <exception cref="InvalidOperationException">If sensor is not connected</exception>
        /// </summary>
        public async Task<SensorReading> ReadColorWithValidationAsync(int maxRetries = 3, int minimumQualityScore = 70)
        {
            if (!_isConnected)
                throw new InvalidOperationException("Sensor is not connected. Call ConnectAsync first.");

            SensorReading? lastReading = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                lastReading = await ReadColorAsync();

                if (lastReading.QualityScore >= minimumQualityScore)
                {
                    return lastReading;
                }

                if (attempt < maxRetries - 1)
                {
                    // Simulate sensor recalibration delay
                    await Task.Delay(100);
                }
            }

            // Return last reading even if below minimum quality
            return lastReading ?? throw new InvalidOperationException("Failed to obtain valid sensor reading.");
        }

        /// <summary>
        /// Simulates sensor calibration procedure.
        /// 
        /// Always succeeds immediately (no simulated calibration failures).
        /// Verifies IsConnected first.
        /// 
        /// In real sensor implementations, this would:
        /// - Guide user to place calibration reference tile
        /// - Measure the reference to establish baseline
        /// - Verify calibration accuracy
        /// - Store calibration data
        /// 
        /// <returns>Task completed with true (always succeeds)</returns>
        /// <exception cref="InvalidOperationException">If sensor is not connected</exception>
        /// </summary>
        public Task<bool> CalibrateAsync()
        {
            if (!_isConnected)
                throw new InvalidOperationException("Sensor is not connected. Call ConnectAsync first.");

            // Simulate calibration process
            return Task.FromResult(true);
        }

        /// <summary>
        /// Returns simulated sensor status information.
        /// 
        /// Returns a multiline string describing current state:
        /// - Connected/disconnected status
        /// - Device name and ID
        /// - Total measurements taken in session
        /// - Simulated battery level
        /// - Simulated last calibration time
        /// 
        /// Example output when connected:
        /// "Connected: NIX Mini 3 (Stub)
        ///  Device ID: STUB-000001
        ///  Readings taken: 5
        ///  Battery: 85% (simulated)
        ///  Last calibration: Today at 10:30 AM (simulated)"
        /// 
        /// <returns>Task completed with status string suitable for UI display</returns>
        /// </summary>
        public Task<string> GetStatusAsync()
        {
            if (!_isConnected)
                return Task.FromResult("Disconnected");

            var status = $"Connected: {DeviceName}\n" +
                        $"Device ID: {DeviceId}\n" +
                        $"Readings taken: {_readingCount}\n" +
                        $"Battery: 85% (simulated)\n" +
                        $"Last calibration: Today at 10:30 AM (simulated)";

            return Task.FromResult(status);
        }

        /// <summary>
        /// Disposes the sensor resources (implements IDisposable).
        /// Ensures DisconnectAsync() is called during cleanup.
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Generates a realistic simulated color with clustering around common paint colors.
        /// 
        /// Instead of pure random colors, this implementation generates colors grouped
        /// around typical color matching scenarios:
        /// - Red-ish: High red, low green/blue
        /// - Green-ish: High green, low red/blue
        /// - Blue-ish: High blue, low red/green
        /// - Neutral/Gray: Similar R/G/B values
        /// - Random: Any color (fallback)
        /// 
        /// This makes the stub more realistic for testing real color matching workflows.
        /// 
        /// <returns>RgbColor with values in appropriate range for generated pattern</returns>
        /// </summary>
        private RgbColor GenerateRealisticColor()
        {
            // Generate colors that cluster around common paint colors
            // This makes the stub more realistic for testing

            var colorPattern = _random.Next(0, 5);
            int r, g, b;

            switch (colorPattern)
            {
                case 0: // Red-ish
                    r = _random.Next(180, 255);
                    g = _random.Next(0, 100);
                    b = _random.Next(0, 100);
                    break;

                case 1: // Green-ish
                    r = _random.Next(0, 100);
                    g = _random.Next(100, 255);
                    b = _random.Next(0, 100);
                    break;

                case 2: // Blue-ish
                    r = _random.Next(0, 100);
                    g = _random.Next(0, 100);
                    b = _random.Next(180, 255);
                    break;

                case 3: // Neutral/Gray
                    var gray = _random.Next(50, 200);
                    r = gray;
                    g = gray + _random.Next(-10, 10);
                    b = gray + _random.Next(-10, 10);
                    break;

                default: // Random
                    r = _random.Next(0, 256);
                    g = _random.Next(0, 256);
                    b = _random.Next(0, 256);
                    break;
            }

            return new RgbColor((byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Generates a quality score simulating environmental variations.
        /// 
        /// Distribution:
        /// - 80% chance: Good quality (75-100) - ideal measurement conditions
        /// - 20% chance: Fair quality (50-75) - some environmental interference
        /// 
        /// This models realistic scenarios where most measurements are good,
        /// but occasional factors (dust, angle, surface irregularities) reduce quality.
        /// 
        /// Used by ReadColorWithValidationAsync() to decide if retry is needed.
        /// 
        /// <returns>Random quality score (0-100)</returns>
        /// </summary>
        private int GenerateQualityScore()
        {
            // 80% chance of good quality (75-100), 20% chance of lower quality (50-75)
            if (_random.Next(0, 100) < 80)
            {
                return _random.Next(75, 101);
            }
            else
            {
                return _random.Next(50, 75);
            }
        }
    }
}
