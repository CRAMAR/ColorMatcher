# ColorMatcher AI Agent Instructions

## Project Overview
Professional color matching app for paint/dye industries. Uses spectrophotometer sensors and CIE LAB color space for perceptually-accurate ΔE calculations. Built with .NET 10 + Avalonia UI (cross-platform).

## Core Architecture

### MVVM Pattern with Source Generators
Uses CommunityToolkit.Mvvm with **C# source generators** (not runtime reflection):
```csharp
[ObservableProperty]  // Generates public property + INotifyPropertyChanged
private string referenceHex = "#FFFFFF";

[RelayCommand]  // Generates public ICommand property
private async Task SaveProjectAsync() { }
```
**Critical**: Property names are auto-generated. `referenceHex` field becomes `ReferenceHex` property accessible in XAML/code.

### Color Space Conversion Pipeline (6 Steps)
RGB → LAB conversion in `ColorSpaceConverter.cs`:
1. RGB (0-255) → normalized (0-1)
2. sRGB gamma correction (threshold: 0.04045)
3. Linear RGB → XYZ (D65 illuminant: 0.95047, 1.00000, 1.08883)
4. XYZ normalization by D65 reference white
5. XYZ → LAB (f(t) function with threshold: 0.008856)
6. Final LAB (L: 0-100, a/b: -128 to +127)

**Never modify these constants** without color science justification. Tests validate against known values.

### Data Flow & Change Detection
`MainWindowViewModel` uses **flags to prevent circular updates**:
- `isUpdatingReference` / `isUpdatingSample` prevent RGB ↔ Hex sync loops
- Pattern: Set flag → Update properties → Clear flag
- **Always check these flags** before triggering dependent updates in ViewModel

```csharp
// Example from MainWindowViewModel
private void UpdateReferenceHexFromRgb()
{
    if (isUpdatingReference) return;
    isUpdatingReference = true;
    // ... update logic
    isUpdatingReference = false;
}
```

## Testing Strategy

### Test Organization (165 tests)
- `ColorSpaceConverterTests.cs`: Core RGB↔LAB accuracy (tolerance: 0.5)
- `EdgeCaseTests.cs`: Boundary values, round-trips, stress tests
- `ViewModelEdgeCaseTests.cs`: UI validation, concurrent updates, null safety
- `ColorHistoryViewModelTests.cs`: History operations, export/reuse
- `Integration/`: Full workflow tests

### Running Tests
```bash
dotnet test                           # All tests
dotnet test --filter ColorSpaceConverter  # Specific class
dotnet test --logger "console;verbosity=normal"  # Detailed output
```

**When adding color logic**: Write tests with known LAB values first. Use tolerance checks (Math.Abs(expected - actual) < tolerance).

## Critical Developer Workflows

### Project Structure (Important!)
- Main app: `ColorMatcher/ColorMatcher.csproj`
- Tests: `ColorMatcher.Tests/ColorMatcher.Tests.csproj`
- Solution: `ColorMatcher.slnx` (XML-based, .NET 10 format)

**Building locally:**
```bash
dotnet restore ColorMatcher.slnx
dotnet build ColorMatcher.slnx --configuration Release
dotnet run --project ColorMatcher/ColorMatcher.csproj
```

### CI/CD Workflow
GitHub Actions (`.github/workflows/ci.yml`) triggered by:
- Pushes to `main` (tests + build)
- PRs to `main` (tests only)
- Tags matching `alpha-*` (tests + build + release)

**Creating releases:**
```bash
git tag -a alpha-0.1.0 -m "Release description"
git push origin alpha-0.1.0
```
Automatically builds Linux/Windows binaries and creates pre-release.

### Avalonia UI Requirements
**Must include `Avalonia.Controls.DataGrid`** package for DataGrid controls (color history view):
```xml
<PackageReference Include="Avalonia.Controls.DataGrid" Version="11.3.11" />
```
This is a separate package from base Avalonia (regression in .NET 10 support).

## Repository Pattern
`IColorRepository` abstracts persistence:
- `FileColorRepository`: JSON file storage (production)
- `InMemoryColorRepository`: RAM storage (testing)

Projects saved to platform-specific AppData: `~/.config/ColorMatcher/Projects/{ProjectId}.json`

## Sensor Integration
`ISensorReader` interface for hardware abstraction:
- `StubSensorReader`: Simulated random colors (development)
- Future: `NixMini3Reader`, `XRiteReader` implementations

**Quality loop pattern** in `ReadWithRetries()`: Retry up to N times, return best reading that meets quality threshold.

## Color Science Context

### ΔE Interpretation (CIE76)
- < 1.0: Imperceptible (excellent match)
- 1-2: Barely noticeable (very good)
- 2-5: Noticeable (good, minor adjustments)
- 5-10: Significant (major adjustments)
- \>10: Obvious mismatch (start over)

### Tint Recommendations
`LabColorGraphModel.GetTintRecommendation()` uses quadrant analysis:
- a* axis: Green (negative) ↔ Red (positive)
- b* axis: Blue (negative) ↔ Yellow (positive)
- Threshold: 5 units before recommending tint adjustment
- Returns human-readable guidance: "Add Red, Add Yellow"

## Common Pitfalls

1. **Don't bypass change detection flags** in ViewModel - causes infinite update loops
2. **RGB values are bytes (0-255)** - validate input range in UI bindings
3. **Hex validation** uses regex `^#[0-9A-Fa-f]{6}$` - enforce this pattern
4. **LAB round-trip accuracy** ~±1 RGB unit due to color space precision limits (expected)
5. **Protected main branch** - all changes require PRs (use auto-merge for maintenance)

## Documentation Standard
All public APIs have XML doc comments with:
- `<summary>` explaining purpose
- Algorithm details for color science methods (equations, thresholds)
- `<param>` and `<returns>` with value ranges
- `<code>` examples for complex patterns
- `<remarks>` for usage notes and gotchas

Example: See `ColorSpaceConverter.cs` for D65 constants, gamma correction formulas.

## Current Status
- ✅ 165 tests passing
- ✅ Core color matching complete
- ✅ Project persistence working
- ✅ Color history UI implemented
- 🚧 Hardware sensor drivers (stub only)
- 📋 Planned: Export to CSV, hardware integration guides
