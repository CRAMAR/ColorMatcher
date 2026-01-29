using System;
using System.Threading.Tasks;
using Xunit;
using ColorMatcher.Models;

namespace ColorMatcher.Tests
{
    public class StubSensorReaderTests
    {
        [Fact]
        public void Constructor_SetPropertiesCorrectly()
        {
            using var sensor = new StubSensorReader();

            Assert.Equal("NIX Mini 3 (Stub)", sensor.DeviceName);
            Assert.Equal("STUB-000001", sensor.DeviceId);
            Assert.False(sensor.IsConnected);
        }

        [Fact]
        public async Task ConnectAsync_SetsConnectedState()
        {
            using var sensor = new StubSensorReader();
            
            Assert.False(sensor.IsConnected);
            
            var result = await sensor.ConnectAsync();
            
            Assert.True(result);
            Assert.True(sensor.IsConnected);
        }

        [Fact]
        public async Task DisconnectAsync_ClearsConnectedState()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            await sensor.DisconnectAsync();

            Assert.False(sensor.IsConnected);
        }

        [Fact]
        public async Task ReadColorAsync_WhenNotConnected_ThrowsException()
        {
            using var sensor = new StubSensorReader();

            await Assert.ThrowsAsync<InvalidOperationException>(() => sensor.ReadColorAsync());
        }

        [Fact]
        public async Task ReadColorAsync_WhenConnected_ReturnsSensorReading()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            var reading = await sensor.ReadColorAsync();

            Assert.NotNull(reading);
            Assert.NotNull(reading.RgbColor);
            Assert.Equal(sensor.DeviceId, reading.DeviceId);
            Assert.NotNull(reading.QualityScore);
            Assert.True(reading.QualityScore >= 0 && reading.QualityScore <= 100);
        }

        [Fact]
        public async Task ReadColorAsync_PopulatesLabColor()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            var reading = await sensor.ReadColorAsync();

            Assert.NotNull(reading.LabColor);
            Assert.True(reading.LabColor.L >= 0 && reading.LabColor.L <= 100);
        }

        [Fact]
        public async Task SetNextReadingOverride_ReturnsSpecifiedColor()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            var expectedColor = new RgbColor(255, 128, 64);
            sensor.SetNextReadingOverride(expectedColor);

            var reading = await sensor.ReadColorAsync();

            Assert.Equal(expectedColor.R, reading.RgbColor.R);
            Assert.Equal(expectedColor.G, reading.RgbColor.G);
            Assert.Equal(expectedColor.B, reading.RgbColor.B);
        }

        [Fact]
        public async Task SetNextReadingOverride_WithNullColor_ThrowsException()
        {
            using var sensor = new StubSensorReader();

            Assert.Throws<ArgumentNullException>(() => sensor.SetNextReadingOverride(null!));
        }

        [Fact]
        public async Task ReadingHistory_TracksAllReadings()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            await sensor.ReadColorAsync();
            await sensor.ReadColorAsync();
            await sensor.ReadColorAsync();

            Assert.Equal(3, sensor.ReadingHistory.Count);
            Assert.NotNull(sensor.ReadingHistory[0]);
            Assert.NotNull(sensor.ReadingHistory[1]);
            Assert.NotNull(sensor.ReadingHistory[2]);
        }

        [Fact]
        public async Task ConnectAsync_ClearsReadingHistory()
        {
            using var sensor = new StubSensorReader();
            
            await sensor.ConnectAsync();
            await sensor.ReadColorAsync();
            await sensor.ReadColorAsync();
            Assert.Equal(2, sensor.ReadingHistory.Count);

            // Connect again
            await sensor.ConnectAsync();
            
            Assert.Empty(sensor.ReadingHistory);
        }

        [Fact]
        public async Task ReadColorWithValidationAsync_ReturnsReadingAboveQualityThreshold()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            var reading = await sensor.ReadColorWithValidationAsync(maxRetries: 1, minimumQualityScore: 0);

            Assert.NotNull(reading);
            Assert.True(reading.QualityScore >= 0);
        }

        [Fact]
        public async Task ReadColorWithValidationAsync_RetriesOnLowQuality()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            // This will retry up to 3 times with minimumQualityScore of 0, so it should succeed
            var reading = await sensor.ReadColorWithValidationAsync(maxRetries: 3, minimumQualityScore: 0);

            Assert.NotNull(reading);
            Assert.NotNull(reading.QualityScore);
        }

        [Fact]
        public async Task ReadColorWithValidationAsync_WhenNotConnected_ThrowsException()
        {
            using var sensor = new StubSensorReader();

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                sensor.ReadColorWithValidationAsync());
        }

        [Fact]
        public async Task CalibrateAsync_WhenConnected_ReturnsTrue()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            var result = await sensor.CalibrateAsync();

            Assert.True(result);
        }

        [Fact]
        public async Task CalibrateAsync_WhenNotConnected_ThrowsException()
        {
            using var sensor = new StubSensorReader();

            await Assert.ThrowsAsync<InvalidOperationException>(() => sensor.CalibrateAsync());
        }

        [Fact]
        public async Task GetStatusAsync_WhenDisconnected_ReturnsDisconnectedStatus()
        {
            using var sensor = new StubSensorReader();

            var status = await sensor.GetStatusAsync();

            Assert.Equal("Disconnected", status);
        }

        [Fact]
        public async Task GetStatusAsync_WhenConnected_ReturnsSensorInfo()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();
            await sensor.ReadColorAsync();

            var status = await sensor.GetStatusAsync();

            Assert.Contains("Connected", status);
            Assert.Contains("NIX Mini 3", status);
            Assert.Contains("STUB-000001", status);
            Assert.Contains("Readings taken: 1", status);
        }

        [Fact]
        public async Task GeneratedColors_HaveVariety()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            var colors = new System.Collections.Generic.List<RgbColor>();
            for (int i = 0; i < 20; i++)
            {
                var reading = await sensor.ReadColorAsync();
                colors.Add(reading.RgbColor);
            }

            // Check that we got some variety in colors
            var uniqueColors = new System.Collections.Generic.HashSet<string>();
            foreach (var color in colors)
            {
                uniqueColors.Add($"{color.R},{color.G},{color.B}");
            }

            // Should have at least 10 unique colors out of 20 readings
            Assert.True(uniqueColors.Count >= 10);
        }

        [Fact]
        public async Task QualityScores_AreInValidRange()
        {
            using var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            for (int i = 0; i < 50; i++)
            {
                var reading = await sensor.ReadColorAsync();
                
                Assert.NotNull(reading.QualityScore);
                Assert.True(reading.QualityScore >= 0 && reading.QualityScore <= 100,
                    $"Quality score {reading.QualityScore} is out of valid range [0, 100]");
            }
        }

        [Fact]
        public async Task Dispose_CleansUpResources()
        {
            var sensor = new StubSensorReader();
            await sensor.ConnectAsync();

            sensor.Dispose();

            Assert.False(sensor.IsConnected);
        }
    }
}
