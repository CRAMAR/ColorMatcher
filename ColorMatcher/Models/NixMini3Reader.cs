using System;
using System.Linq;
using System.Threading.Tasks;
using ColorMatcher.Models;
using NixUniversalSDK;

namespace ColorMatcher.Models
{
    public class NixMini3Reader : ISensorReader
    {
        public string DeviceName => "NIX Mini 3";
        public string DeviceId { get; private set; } = "NIX-UNKNOWN";
        public bool IsConnected { get; private set; }

        private IDeviceCompat? _device;
        private static bool _licenseActivated = false;

        public NixMini3Reader()
        {
            // Activate license once per app domain
            if (!_licenseActivated)
            {
                // TODO: Replace with your real activation code
                string activationCode = "e=1&n=1&u=c2ddfe17669d4806aa7819d721d78c2e";
                LicenseManager.Activate(activationCode);
                _licenseActivated = true;
            }
        }

        public void Dispose()
        {
            _device?.Dispose();
        }

        public async Task<bool> ConnectAsync()
        {
            // Discover devices
            var devices = await DeviceScanner.ScanAsync();
            _device = devices.FirstOrDefault();
            if (_device == null)
                return false;
            await _device.OpenAsync();
            DeviceId = _device.SerialNumber;
            IsConnected = true;
            return true;
        }

        public async Task DisconnectAsync()
        {
            if (_device != null)
            {
                await _device.CloseAsync();
                _device.Dispose();
                _device = null;
            }
            IsConnected = false;
        }

        public async Task<SensorReading> ReadColorAsync()
        {
            if (!IsConnected || _device == null)
                throw new InvalidOperationException("Sensor not connected");
            var measurement = await _device.MeasureAsync();
            // Use measurement.Colorimetry for RGB, measurement.Quality for quality score
            var rgb = new RgbColor(
                (byte)measurement.Colorimetry.RGB.R,
                (byte)measurement.Colorimetry.RGB.G,
                (byte)measurement.Colorimetry.RGB.B);
            int quality = measurement.Quality;
            return new SensorReading(rgb, quality, DateTime.Now, DeviceName);
        }

        public async Task<SensorReading> ReadColorWithValidationAsync(int maxRetries = 3, int minimumQualityScore = 70)
        {
            SensorReading? best = null;
            for (int i = 0; i < maxRetries; i++)
            {
                var reading = await ReadColorAsync();
                if (reading.QualityScore >= minimumQualityScore)
                    return reading;
                if (best == null || reading.QualityScore > best.QualityScore)
                    best = reading;
                await Task.Delay(100);
            }
            return best!;
        }

        public async Task<bool> CalibrateAsync()
        {
            if (!IsConnected || _device == null)
                return false;
            // If the SDK/device supports calibration, call it here
            // Example: await _device.CalibrateAsync();
            await Task.Delay(1000); // Placeholder
            return true;
        }

        public async Task<string> GetStatusAsync()
        {
            if (!IsConnected || _device == null)
                return "Disconnected";
            // Query device for status info
            // Example: battery, last calibration, etc.
            var battery = _device.BatteryLevel;
            var status = $"Connected: {DeviceName}\nDevice ID: {DeviceId}\nBattery: {battery}%\nStatus: Ready";
            await Task.CompletedTask;
            return status;
        }
    }
}
