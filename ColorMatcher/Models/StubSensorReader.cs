using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ColorMatcher.Models
{
    /// <summary>
    /// Stub implementation of ISensorReader for development and testing without physical hardware.
    /// Generates realistic simulated color readings with optional quality variations.
    /// Can be configured to return specific colors or random variations for testing.
    /// </summary>
    public class StubSensorReader : ISensorReader
    {
        private readonly Random _random = new();
        private readonly List<SensorReading> _readingHistory = new();
        private bool _isConnected = false;
        private int _readingCount = 0;
        private RgbColor? _overrideColor;

        public string DeviceName => "NIX Mini 3 (Stub)";
        public string DeviceId => "STUB-000001";
        public bool IsConnected => _isConnected;

        /// <summary>
        /// Sets a specific color to be returned by the next ReadColorAsync call.
        /// If not set, random colors will be generated.
        /// </summary>
        public void SetNextReadingOverride(RgbColor color)
        {
            _overrideColor = color ?? throw new ArgumentNullException(nameof(color));
        }

        /// <summary>
        /// Gets all readings that have been taken during this session
        /// </summary>
        public IReadOnlyList<SensorReading> ReadingHistory => _readingHistory.AsReadOnly();

        public Task<bool> ConnectAsync()
        {
            _isConnected = true;
            _readingCount = 0;
            _readingHistory.Clear();
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            _isConnected = false;
            return Task.CompletedTask;
        }

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

        public Task<bool> CalibrateAsync()
        {
            if (!_isConnected)
                throw new InvalidOperationException("Sensor is not connected. Call ConnectAsync first.");

            // Simulate calibration process
            return Task.FromResult(true);
        }

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

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }

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
