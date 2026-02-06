# Testing with Physical NIX Mini 3 Device

## Prerequisites

### Hardware Requirements
- **Device**: NIX Mini 3 spectrophotometer
- **Platform**: Windows 10/11 (64-bit)
- **Connection**: USB cable OR Bluetooth 4.0+
- **Battery**: Device charged >20%

### Software Requirements
- .NET 10 SDK installed on Windows
- Visual Studio 2022 (optional, for debugging)
- NIX device drivers (should install automatically when connected)

## Step 1: Build for Windows

**On Windows machine, clone and build:**

```bash
git clone <your-repo-url>
cd ColorMatcher
dotnet restore ColorMatcher.slnx
dotnet build ColorMatcher.slnx --configuration Release --framework net10.0-windows
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
ColorMatcher net10.0-windows -> ColorMatcher/bin/Release/net10.0-windows/ColorMatcher.dll
```

## Step 2: Replace License Activation Code

**Edit:** `ColorMatcher/Models/NixMini3Reader.cs` (line ~93)

```csharp
// TODO: Replace with your real activation code from nixsensor.com
string activationCode = "YOUR_ACTIVATION_CODE_HERE";
```

**Get activation code:**
1. Visit https://www.nixsensor.com/sdk-doc-windows
2. Contact Nix Sensor for SDK license
3. Replace placeholder with format: `e=<expiry>&n=<name>&u=<uuid>`

**Rebuild after updating license:**
```bash
dotnet build ColorMatcher/ColorMatcher.csproj --configuration Release --framework net10.0-windows
```

## Step 3: Connect NIX Mini 3 Device

### USB Connection (Recommended)
1. Power on NIX Mini 3
2. Connect USB cable to device and PC
3. Windows should detect device and install drivers automatically
4. Check Device Manager → Universal Serial Bus devices → "NIX Mini 3"

### Bluetooth Connection (Alternative)
1. Power on NIX Mini 3
2. Windows Settings → Bluetooth & devices → Add device
3. Select "Bluetooth"
4. Choose "NIX Mini 3" from list
5. Wait for pairing to complete

## Step 4: Run ColorMatcher Application

```bash
cd ColorMatcher
dotnet run --configuration Release --framework net10.0-windows
```

**Expected on launch:**
- Application window opens
- "Connect Sensor" button visible
- Device status shows "Disconnected"

## Step 5: Test Connection

### In the Application:
1. Click **"Connect Sensor"** button
2. Wait 2-5 seconds for device discovery

**Success Indicators:**
- Button changes to "Disconnect Sensor"
- Device status updates with:
  - Serial Number
  - Firmware version
  - Battery level
  - Connection state

**Failure Scenarios:**

| Issue | Symptom | Solution |
|-------|---------|----------|
| No device found | "Connection failed" message | Check USB cable, try different port |
| License error | Exception on Connect | Verify activation code is correct |
| Driver missing | Device not detected | Reinstall drivers from nixsensor.com |
| Device busy | Connection timeout | Close other apps using NIX device |

## Step 6: Test Color Measurement

### Basic Measurement:
1. Ensure sensor is connected
2. Select a **Reference Color** (any color, e.g., white paper)
3. Place NIX Mini 3 on reference surface
4. Click **"Measure Reference"** button
5. Wait for measurement (~1 second)

**Expected Results:**
- RGB values update in Reference section
- Hex color code displays
- LAB values show in graph
- Quality score displays (should be 80-100 for good measurement)

### Sample Measurement:
1. Select a different colored surface
2. Click **"Measure Sample"** button
3. Review ΔE (color difference) calculation

**Quality Score Interpretation:**
- **100**: Perfect measurement
- **80-99**: Excellent (typical for good conditions)
- **60-79**: Good (acceptable)
- **40-59**: Fair (may need retry)
- **<40**: Poor (retry required)

## Step 7: Test Retry Logic

**Test automatic retry:**
1. Deliberately create poor measurement conditions:
   - Glossy/reflective surface
   - Sensor not flush against surface
   - Ambient light leaking in
2. Click "Measure Reference"
3. Watch for automatic retries (up to 3 attempts)
4. Best quality reading will be used

## Step 8: Check Device Status

**View comprehensive status:**
1. While connected, click status/info area (implementation dependent)
2. Verify displayed information:
   - Device name: "NIX Mini 3"
   - Serial number (format: NIX-XXXXXX)
   - Firmware version
   - Hardware version
   - Battery level (percentage)
   - Charging status
   - Lifetime scan count
   - Calibration status

**Expected Status Output:**
```
Status: Connected
Device: NIX Mini 3
Serial Number: NIX-A1B2C3
Device ID: <unique-id>
Firmware: 2.x.x
Hardware: 3.x.x

Battery: 85%
Charging: No

Lifetime Scans: 1,234

Calibration Enabled: True
Last Calibration: 2/5/2026 10:30 AM
```

## Step 9: Performance Testing

### Measurement Speed:
1. Take 10 consecutive measurements
2. Time total duration
3. **Expected**: ~1-2 seconds per measurement

### Quality Consistency:
1. Measure same surface 10 times without moving sensor
2. Record RGB values and quality scores
3. **Expected**: RGB variance <±2 units, Quality >90%

### Battery Drain:
1. Note starting battery level
2. Take 50 measurements
3. Check battery level
4. **Expected**: <5% drain for 50 measurements

## Troubleshooting Common Issues

### Connection Failures

**"No device found"**
```
Cause: USB not detected or Bluetooth not paired
Fix: 
  - Try different USB port
  - Check Device Manager for "Unknown device"
  - Re-pair Bluetooth
  - Restart NIX device (hold button 10 seconds)
```

**"License activation failed"**
```
Cause: Invalid or expired activation code
Fix:
  - Verify activation code format
  - Contact Nix Sensor for new license
  - Check internet connection (if online activation required)
```

**"Device already in use"**
```
Cause: Another application has device handle
Fix:
  - Close Nix Color app (official app)
  - Check Task Manager for hung processes
  - Disconnect and reconnect USB
```

### Measurement Issues

**Quality score always 0**
```
Cause: Measurement failed or sensor error
Fix:
  - Check sensor aperture is clean
  - Ensure surface is opaque
  - Verify device calibration is current
  - Try measuring white reference tile
```

**RGB values all 0 or 255**
```
Cause: Extreme lighting or sensor malfunction
Fix:
  - Measure in normal lighting conditions
  - Check sensor LEDs are working
  - Perform device calibration
```

**Inconsistent readings**
```
Cause: Poor measurement technique or surface issues
Fix:
  - Hold sensor steady for full measurement cycle
  - Ensure aperture fully contacts surface
  - Use matte, opaque surfaces
  - Avoid glossy or textured surfaces
```

## Advanced Testing

### Calibration Testing (if reference tile available)

**Note:** Current implementation has placeholder for calibration. Full implementation requires:

1. QR code scanning from reference tile
2. UI for calibration workflow
3. Validation of tile string

**Manual calibration test:**
```csharp
// In NixMini3Reader.cs, modify CalibrateAsync():
public async Task<bool> CalibrateAsync()
{
    if (!IsConnected || _device == null || !_device.SupportsFieldCalibration)
        return false;
    
    // Replace with actual tile string from QR code
    string tileString = "SCAN_YOUR_TILE_QR_CODE";
    
    if (_device.IsTileStringValid(tileString) == true)
    {
        var result = await _device.RunFieldCalibrationAsync(tileString);
        return result.Status == CommandStatus.Success;
    }
    
    return false;
}
```

### Color Accuracy Verification

**Test with known color standards:**
1. Use Pantone Color Guide or standard color swatches
2. Measure each standard
3. Compare RGB values to published specifications
4. **Expected**: ΔE <2.0 for color-matched samples

### Stress Testing

**Rapid measurement cycle:**
```csharp
// Test code (in test project or console app)
for (int i = 0; i < 100; i++)
{
    var reading = await sensor.ReadColorAsync();
    Console.WriteLine($"Measurement {i+1}: RGB({reading.RgbColor.R}, {reading.RgbColor.G}, {reading.RgbColor.B}) Quality: {reading.QualityScore}%");
    await Task.Delay(100); // Small delay between measurements
}
```

**Expected**: No memory leaks, consistent performance, no connection drops

## Debugging Tips

### Enable Detailed Logging

**Add to NixMini3Reader.cs:**
```csharp
using System.Diagnostics;

public async Task<bool> ConnectAsync()
{
    Debug.WriteLine("Starting device scan...");
    _scanner = new DeviceScanner();
    var scannerState = await _scanner.InitializeAsync();
    Debug.WriteLine($"Scanner state: {scannerState}");
    
    var usbDevices = await _scanner.ListUsbDevicesAsync();
    Debug.WriteLine($"Found {usbDevices.Count()} USB devices");
    
    // ... rest of method
}
```

### Monitor SDK Events

**Subscribe to device events:**
```csharp
_device.Connected += (sender, args) => Debug.WriteLine("Device connected");
_device.Disconnected += (sender, args) => Debug.WriteLine($"Device disconnected: {args.Status}");
_device.BatteryStateChanged += (sender, args) => Debug.WriteLine($"Battery: {args.BatteryLevel}%");
```

### Visual Studio Debugging

1. Open solution in Visual Studio 2022
2. Set breakpoints in:
   - `NixMini3Reader.ConnectAsync()` - line ~140
   - `NixMini3Reader.ReadColorAsync()` - line ~220
3. Run with debugger (F5)
4. Step through SDK calls
5. Inspect `DeviceResult`, `IMeasurementData` objects

## Success Criteria

✅ **Connection Test Passed:**
- Device connects within 5 seconds
- Status shows accurate device information
- No exceptions thrown

✅ **Measurement Test Passed:**
- Measurements complete in <2 seconds
- Quality scores >70% for good surfaces
- RGB values within expected range (0-255)
- Consistent readings for same surface

✅ **Reliability Test Passed:**
- 100 consecutive measurements without failure
- No memory leaks or performance degradation
- Connection remains stable

✅ **Battery Management:**
- Battery level displays accurately
- Low battery warning when <20%
- Device operates normally until <10%

## Next Steps After Testing

1. **Document any issues** found during testing
2. **Collect sample measurements** for regression testing
3. **Create unit tests** for NixMini3Reader using mock hardware
4. **Update user documentation** with real-world usage examples
5. **Consider adding**:
   - Calibration reminder UI
   - Battery status indicator
   - Measurement history
   - Export measurement data

## Support Resources

- **NIX Sensor Documentation**: https://nixsensor.github.io/nix-universal-sdk-windows-doc/
- **SDK API Reference**: https://nixsensor.github.io/nix-universal-sdk-windows-doc/api/NixUniversalSDK.html
- **Technical Support**: support@nixsensor.com
- **ColorMatcher Issues**: [Project GitHub Issues]

## Appendix: Test Checklist

```
□ Prerequisites verified (Windows, device, drivers)
□ Project built for net10.0-windows
□ License activation code updated
□ Device connected (USB/Bluetooth)
□ Application launches successfully
□ Sensor connection established
□ Device status displays correctly
□ Reference measurement works
□ Sample measurement works
□ ΔE calculation correct
□ Quality scoring accurate
□ Retry logic functions
□ Battery monitoring works
□ Disconnect/reconnect cycle tested
□ Performance acceptable
□ No crashes or exceptions
□ Documentation updated with findings
```
