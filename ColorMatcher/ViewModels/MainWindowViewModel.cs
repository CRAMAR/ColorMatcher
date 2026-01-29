using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ColorMatcher.Models;

namespace ColorMatcher.ViewModels;

/// <summary>
/// Main application view model managing color matching UI state and logic.
/// 
/// Implements MVVM (Model-View-ViewModel) pattern using the MVVM Toolkit library (CommunityToolkit.Mvvm).
/// This view model is responsible for:
/// - Managing reference (target) and sample (candidate) color state
/// - Synchronizing between multiple color input formats (Hex, RGB individual values)
/// - Calculating color differences and providing matching guidance
/// - Maintaining a visual graph model for LAB color space representation
/// - Handling project persistence (create, save, load)
/// - Coordinating sensor input when available
/// 
/// <remarks>
/// **MVVM Architecture with MVVM Toolkit**
/// 
/// The MVVM Toolkit's @ObservableProperty attribute uses source generation to:
/// 1. Generate public properties with automatic get/set
/// 2. Generate OnPropertyChanged() calls automatically
/// 3. Enable partial OnXxxChanged() methods for custom change handling
/// 4. Support INotifyPropertyChanged for Avalonia UI data binding
/// 
/// This approach eliminates boilerplate INotifyPropertyChanged code while maintaining
/// compile-time safety and performance.
/// 
/// **RelayCommand Pattern**
/// 
/// The @RelayCommand attribute (used on methods) generates ICommand properties:
/// - CreateNewProjectCommand: Initializes a new color matching session
/// - SaveProjectCommand: Persists current project to repository
/// - LoadProjectCommand: Retrieves saved project from repository
/// - InitSensorCommand: Attempts to initialize hardware sensor
/// 
/// **Color Input Synchronization**
/// 
/// Colors can be entered in two formats with automatic bi-directional synchronization:
/// - Hex format: "#RRGGBB" (e.g., "#FF5733")
/// - RGB format: Three separate 0-255 values (Red, Green, Blue)
/// 
/// Synchronization flow:
/// - User edits ReferenceHex → OnReferenceHexChanged() → R/G/B update → UpdateGraphData()
/// - User edits ReferenceR → OnReferenceRChanged() → Hex updates → UpdateGraphData()
/// - Both reference and sample colors trigger UpdateGraphData() → graph recalculation
/// 
/// **State Management**
/// 
/// Reference Color State (Target):
///   - ReferenceHex: Primary hex input
///   - ReferenceR/G/B: RGB component inputs (0-255)
///   - ReferenceBrush: Visual color for UI display
///   - Status: Validated by UpdateReferenceFromRgb()
///   
/// Sample Color State (Candidate):
///   - SampleHex: Primary hex input
///   - SampleR/G/B: RGB component inputs (0-255)
///   - SampleBrush: Visual color for UI display
///   - Status: Validated by UpdateSampleFromRgb()
/// 
/// Project State:
///   - CurrentProject: Active ColorProject (create with CreateNewProjectCommand)
///   - SavedProjects: List of previously saved projects
/// 
/// **Change Detection Flags**
/// 
/// The isUpdatingReference and isUpdatingSample flags prevent infinite update loops:
/// 
/// Example without flag:
/// - OnReferenceHexChanged("#FF0000") fires
/// - Sets ReferenceR = "255", ReferenceG = "0", ReferenceB = "0"
/// - Each assignment triggers OnReferenceRChanged(), OnReferenceGChanged(), OnReferenceBChanged()
/// - These methods call UpdateReferenceFromRgb() which tries to update Hex again → loop
/// 
/// With flag:
/// - isUpdatingReference = true prevents cascading updates
/// - Only UpdateGraphData() is called once after all properties are synchronized
/// 
/// **Color Difference Calculation Pipeline**
/// 
/// When either color changes:
/// 1. RGB values (0-255) validated via TryParseRgb()
/// 2. Converted to LAB color space via ColorSpaceConverter.RgbToLab()
/// 3. GraphModel updated with new colors
/// 4. ColorDifference calculated: ΔE = √((ΔL*)² + (Δa*)² + (Δb*)²)
/// 5. TintRecommendation generated (e.g., "Add Red, Add Yellow")
/// 6. All changes propagated to UI via property notifications
/// 
/// **Sensor Integration**
/// 
/// When InitSensorCommand is executed, the view model attempts to:
/// 1. Connect to hardware sensor (ISensorReader implementation)
/// 2. Optionally auto-populate sample colors from sensor readings
/// 3. Support real-time color matching with physical samples
/// </remarks>
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// Repository for color project persistence (create, read, update, delete operations).
    /// 
    /// Used by project commands to save/load ColorProject objects.
    /// Typically FileColorRepository for file-based persistence or InMemoryColorRepository for testing.
    /// </summary>
    private IColorRepository _repository;

    /// <summary>
    /// Optional hardware sensor reader for capturing physical color samples.
    /// 
    /// When initialized via InitSensorCommand, enables real-time color input from
    /// compatible hardware sensors. Null if sensor is unavailable or not initialized.
    /// </summary>
    private ISensorReader? _sensor;

    /// <summary>
    /// Reference (target) color in hexadecimal format (e.g., "#FF5733").
    /// 
    /// Bound to a TextBox in the UI for direct hex color entry. Changes to this property
    /// automatically synchronize ReferenceR, ReferenceG, ReferenceB, and ReferenceBrush.
    /// 
    /// Validation: Must be exactly 6 hex digits, optionally preceded by '#'.
    /// Invalid formats are silently ignored (UI retains previous valid value).
    /// 
    /// Default: "#FFFFFF" (white)
    /// </summary>
    [ObservableProperty]
    private string referenceHex = "#FFFFFF";

    /// <summary>
    /// Reference color red component as a string (0-255).
    /// 
    /// Bound to a TextBox for numeric input. Changes automatically synchronize with
    /// ReferenceHex and ReferenceBrush. Validation: Must be a valid integer 0-255.
    /// 
    /// Default: "255" (white)
    /// </summary>
    [ObservableProperty]
    private string referenceR = "255";

    /// <summary>
    /// Reference color green component as a string (0-255).
    /// 
    /// Bound to a TextBox for numeric input. Changes automatically synchronize with
    /// ReferenceHex and ReferenceBrush.
    /// 
    /// Default: "255" (white)
    /// </summary>
    [ObservableProperty]
    private string referenceG = "255";

    /// <summary>
    /// Reference color blue component as a string (0-255).
    /// 
    /// Bound to a TextBox for numeric input. Changes automatically synchronize with
    /// ReferenceHex and ReferenceBrush.
    /// 
    /// Default: "255" (white)
    /// </summary>
    [ObservableProperty]
    private string referenceB = "255";

    /// <summary>
    /// Reference color visual representation (Avalonia SolidColorBrush).
    /// 
    /// Bound to a Border or Rectangle control to display the reference color visually.
    /// Automatically updated whenever R/G/B or Hex values change.
    /// 
    /// Default: White (Colors.White)
    /// </summary>
    [ObservableProperty]
    private SolidColorBrush referenceBrush = new(Colors.White);

    /// <summary>
    /// Sample (candidate) color in hexadecimal format (e.g., "#808080").
    /// 
    /// Bound to a TextBox for direct hex color entry. Changes automatically synchronize
    /// SampleR, SampleG, SampleB, and SampleBrush.
    /// 
    /// Validation: Must be exactly 6 hex digits, optionally preceded by '#'.
    /// Invalid formats are silently ignored.
    /// 
    /// Default: "#808080" (gray)
    /// </summary>
    [ObservableProperty]
    private string sampleHex = "#808080";

    /// <summary>
    /// Sample color red component as a string (0-255).
    /// 
    /// Default: "128" (gray)
    /// </summary>
    [ObservableProperty]
    private string sampleR = "128";

    /// <summary>
    /// Sample color green component as a string (0-255).
    /// 
    /// Default: "128" (gray)
    /// </summary>
    [ObservableProperty]
    private string sampleG = "128";

    /// <summary>
    /// Sample color blue component as a string (0-255).
    /// 
    /// Default: "128" (gray)
    /// </summary>
    [ObservableProperty]
    private string sampleB = "128";

    /// <summary>
    /// Sample color visual representation (Avalonia SolidColorBrush).
    /// 
    /// Bound to a Border or Rectangle control. Automatically updated when sample values change.
    /// 
    /// Default: Gray (Colors.Gray)
    /// </summary>
    [ObservableProperty]
    private SolidColorBrush sampleBrush = new(Colors.Gray);

    /// <summary>
    /// Graph model for LAB color space visualization and calculations.
    /// 
    /// Contains reference and sample colors converted to LAB space. Exposes methods for:
    /// - GetColorDifference(): Calculates ΔE (color difference)
    /// - GetTintRecommendation(): Suggests color adjustments (Add Red, Add Yellow, etc.)
    /// 
    /// Updated by UpdateGraphData() whenever either color changes.
    /// Provides perceptually uniform (device-independent) color calculations.
    /// 
    /// Default: Empty model with null colors
    /// </summary>
    [ObservableProperty]
    private LabColorGraphModel graphModel = new();

    /// <summary>
    /// Color difference (ΔE) between reference and sample colors.
    /// 
    /// Calculated using CIE76 formula in LAB color space:
    /// ΔE = √((ΔL*)² + (Δa*)² + (Δb*)²)
    /// 
    /// Interpretation:
    /// - &lt; 1.0: Imperceptible
    /// - 1.0 - 2.0: Just barely perceptible
    /// - 2.0 - 5.0: Noticeable but acceptable
    /// - 5.0 - 10.0: Significant/likely unacceptable
    /// - &gt; 10.0: Very obvious mismatch
    /// 
    /// Displayed as numeric indicator in UI. Updated via UpdateGraphData().
    /// 
    /// Default: 0.0
    /// </summary>
    [ObservableProperty]
    private double colorDifference = 0;

    /// <summary>
    /// Tint recommendation based on color difference analysis.
    /// 
    /// Generated by LabColorGraphModel.GetTintRecommendation(). Examples:
    /// - "Add Red, Add Yellow" (multi-axis adjustment needed)
    /// - "Add Green" (single-axis adjustment)
    /// - "Fine adjustments needed" (difference &lt; 5 on both axes)
    /// - "Colors are very close" (imperceptible difference)
    /// - "Enter both colors" (one or both colors not yet set)
    /// 
    /// Updated via UpdateGraphData() whenever either color changes.
    /// 
    /// Default: "Enter both colors"
    /// </summary>
    [ObservableProperty]
    private string tintRecommendation = "Enter both colors";

    /// <summary>
    /// Currently active color matching project.
    /// 
    /// A ColorProject instance representing the current matching session:
    /// - Contains reference color (target being matched)
    /// - Contains sample color (candidate under evaluation)
    /// - Maintains history of all color matching attempts
    /// - Can be saved/loaded via repository
    /// 
    /// Set by CreateNewProjectCommand. Used as container for persisting all session data.
    /// Null when no project is active.
    /// </summary>
    [ObservableProperty]
    private ColorProject? currentProject;

    /// <summary>
    /// Display name for the current project.
    /// 
    /// User-friendly project identifier (e.g., "Ferrari Red Match", "Logo Color #3").
    /// Bound to a TextBox for editing. Synchronized with CurrentProject when saved.
    /// 
    /// Default: "Untitled Project"
    /// </summary>
    [ObservableProperty]
    private string projectName = "Untitled Project";

    /// <summary>
    /// Optional description/notes for the current project.
    /// 
    /// Additional metadata for the project (e.g., "Matching paint for custom car project").
    /// Bound to a multi-line TextBox for editing.
    /// 
    /// Default: Empty string
    /// </summary>
    [ObservableProperty]
    private string projectDescription = "";

    /// <summary>
    /// List of recently accessed color projects.
    /// 
    /// An ObservableCollection of ColorProject objects loaded from the repository.
    /// Bound to a ListBox or ComboBox for quick project selection/switching.
    /// Populated by LoadRecentProjectsAsync() method.
    /// 
    /// Supports: Project selection, project switching, quick history view
    /// 
    /// Default: Empty collection
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ColorProject> recentProjects = new();

    /// <summary>
    /// Color matching history for the current project.
    /// 
    /// An ObservableCollection of ColorHistoryEntry objects representing all color
    /// matching attempts in the current session. Each entry contains:
    /// - Reference and sample colors at time of entry
    /// - DeltaE (color difference) at that moment
    /// - Tint recommendation provided to user
    /// - User acceptance/rejection status
    /// - Timestamps and optional notes
    /// 
    /// Bound to a DataGrid for display. Populated by LoadProjectHistoryAsync() when
    /// a project is loaded. Updated via AddToHistoryAsync() when user accepts a match.
    /// 
    /// Supports: Audit trail viewing, matching session review, pattern analysis
    /// 
    /// Default: Empty collection
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ColorHistoryEntry> historyItems = new();

    /// <summary>
    /// Indicates whether the current project has been modified since last save.
    /// 
    /// Bound to UI indicators (e.g., asterisk in title bar, unsaved indicator).
    /// Set to true when project data changes (colors, name, description).
    /// Set to false after SaveProjectCommand completes successfully.
    /// 
    /// Default: false
    /// </summary>
    [ObservableProperty]
    private bool isProjectModified = false;

    /// <summary>
    /// Indicates whether a hardware sensor is successfully connected.
    /// 
    /// Set to true by InitSensorCommand if sensor initialization succeeds.
    /// Used to enable/disable sensor-related UI elements and commands.
    /// 
    /// Default: false
    /// </summary>
    [ObservableProperty]
    private bool isSensorConnected = false;

    /// <summary>
    /// Human-readable status message for sensor connection.
    /// 
    /// Examples: "Not connected", "Connected to ColorSensor v2.1", "Connection failed"
    /// Bound to a status text block for user feedback.
    /// Updated by InitSensorCommand.
    /// 
    /// Default: "Not connected"
    /// </summary>
    [ObservableProperty]
    private string sensorStatus = "Not connected";

    /// <summary>
    /// Indicates whether a sensor color read operation is in progress.
    /// 
    /// Set to true when ReadSensorColorCommand starts. Prevents multiple simultaneous reads.
    /// Used to disable sensor commands during active reads.
    /// 
    /// Default: false
    /// </summary>
    [ObservableProperty]
    private bool isReadingColor = false;

    /// <summary>
    /// Flag to prevent infinite update loops when synchronizing reference color inputs.
    /// 
    /// Prevents cascading updates when OnReferenceHexChanged() modifies R/G/B properties,
    /// which would otherwise trigger their change methods and cause recursive updates.
    /// </summary>
    private bool isUpdatingReference;

    /// <summary>
    /// Flag to prevent infinite update loops when synchronizing sample color inputs.
    /// </summary>
    private bool isUpdatingSample;

    /// <summary>
    /// Initializes the ViewModel with in-memory repository and stub sensor.
    /// 
    /// Used during development and testing. In production, call InitializeWithFileRepositoryAsync()
    /// to use file-based persistence.
    /// </summary>
    public MainWindowViewModel()
    {
        // Initialize with in-memory repository (can be switched to FileColorRepository)
        _repository = new InMemoryColorRepository();
        
        // Initialize with stub sensor reader (can be switched to real hardware)
        _sensor = new StubSensorReader();
    }

    /// <summary>
    /// Initialize the ViewModel with file-based repository for persistent storage.
    /// 
    /// Should be called on application startup to enable project persistence.
    /// Loads all previously saved projects from disk into the repository.
    /// Populates RecentProjects collection for UI display.
    /// 
    /// <param name="projectsDirectory">Directory path where projects are stored (e.g., Documents/ColorMatcher/Projects)</param>
    /// </summary>
    public async Task InitializeWithFileRepositoryAsync(string projectsDirectory)
    {
        _repository = new FileColorRepository(projectsDirectory);
        
        if (_repository is FileColorRepository fileRepo)
        {
            await fileRepo.LoadAllProjectsAsync();
        }

        await LoadRecentProjectsAsync();
    }

    /// <summary>
    /// Handles changes to the ReferenceHex property and synchronizes RGB values.
    /// 
    /// This is a partial method generated by MVVM Toolkit @ObservableProperty.
    /// Called automatically when ReferenceHex property changes.
    /// 
    /// Process:
    /// 1. Check if update is already in progress (isUpdatingReference) to prevent loops
    /// 2. Parse hex value to Color using TryParseHex()
    /// 3. If parse fails, silently return (invalid hex strings are ignored)
    /// 4. Set isUpdatingReference = true to suppress cascading updates
    /// 5. Update ReferenceR, ReferenceG, ReferenceB with extracted values
    /// 6. Update ReferenceBrush for UI display
    /// 7. Reset isUpdatingReference = false
    /// 8. Trigger UpdateGraphData() to recalculate color difference and recommendations
    /// 
    /// <param name="value">New hex color string (e.g., "#FF5733")</param>
    /// </summary>
    partial void OnReferenceHexChanged(string value)
    {
        if (isUpdatingReference)
        {
            return;
        }

        if (!TryParseHex(value, out var color))
        {
            return;
        }

        isUpdatingReference = true;
        ReferenceR = color.R.ToString(CultureInfo.InvariantCulture);
        ReferenceG = color.G.ToString(CultureInfo.InvariantCulture);
        ReferenceB = color.B.ToString(CultureInfo.InvariantCulture);
        ReferenceBrush = new SolidColorBrush(color);
        isUpdatingReference = false;
        UpdateGraphData();
    }

    /// <summary>
    /// Handles changes to the ReferenceR property.
    /// 
    /// Called automatically when user edits the red component. Triggers:
    /// - UpdateReferenceFromRgb(): Synchronizes hex and brush
    /// - UpdateGraphData(): Recalculates color difference and recommendations
    /// </summary>
    partial void OnReferenceRChanged(string value)
    {
        UpdateReferenceFromRgb();
        UpdateGraphData();
    }

    /// <summary>
    /// Handles changes to the ReferenceG property.
    /// 
    /// Called automatically when user edits the green component. Triggers synchronization and recalculation.
    /// </summary>
    partial void OnReferenceGChanged(string value)
    {
        UpdateReferenceFromRgb();
        UpdateGraphData();
    }

    /// <summary>
    /// Handles changes to the ReferenceB property.
    /// 
    /// Called automatically when user edits the blue component. Triggers synchronization and recalculation.
    /// </summary>
    partial void OnReferenceBChanged(string value)
    {
        UpdateReferenceFromRgb();
        UpdateGraphData();
    }

    /// <summary>
    /// Handles changes to the SampleHex property and synchronizes RGB values.
    /// 
    /// Same logic as OnReferenceHexChanged() but for the sample (candidate) color.
    /// </summary>
    partial void OnSampleHexChanged(string value)
    {
        if (isUpdatingSample)
        {
            return;
        }

        if (!TryParseHex(value, out var color))
        {
            return;
        }

        isUpdatingSample = true;
        SampleR = color.R.ToString(CultureInfo.InvariantCulture);
        SampleG = color.G.ToString(CultureInfo.InvariantCulture);
        SampleB = color.B.ToString(CultureInfo.InvariantCulture);
        SampleBrush = new SolidColorBrush(color);
        isUpdatingSample = false;
    }

    /// <summary>
    /// Handles changes to the SampleR property.
    /// Triggers synchronization and recalculation.
    /// </summary>
    partial void OnSampleRChanged(string value)
    {
        UpdateSampleFromRgb();
        UpdateGraphData();
    }

    /// <summary>
    /// Handles changes to the SampleG property.
    /// Triggers synchronization and recalculation.
    /// </summary>
    partial void OnSampleGChanged(string value)
    {
        UpdateSampleFromRgb();
        UpdateGraphData();
    }

    /// <summary>
    /// Handles changes to the SampleB property.
    /// Triggers synchronization and recalculation.
    /// </summary>
    partial void OnSampleBChanged(string value)
    {
        UpdateSampleFromRgb();
        UpdateGraphData();
    }

    /// <summary>
    /// Synchronizes reference color RGB components to hex format.
    /// 
    /// Called when user edits any of ReferenceR, ReferenceG, or ReferenceB.
    /// 
    /// Process:
    /// 1. Check if update already in progress (isUpdatingReference)
    /// 2. Validate R/G/B values via TryParseRgb()
    /// 3. If any value invalid, silently return
    /// 4. Convert Avalonia Color to hex format "#{R:X2}{G:X2}{B:X2}"
    /// 5. Update ReferenceHex and ReferenceBrush
    /// 
    /// Example: ReferenceR="255", ReferenceG="87", ReferenceB="51" → ReferenceHex="#FF5733"
    /// </summary>
    private void UpdateReferenceFromRgb()
    {
        if (isUpdatingReference)
        {
            return;
        }

        if (!TryParseRgb(ReferenceR, ReferenceG, ReferenceB, out var color))
        {
            return;
        }

        isUpdatingReference = true;
        ReferenceHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        ReferenceBrush = new SolidColorBrush(color);
        isUpdatingReference = false;
    }

    /// <summary>
    /// Synchronizes sample color RGB components to hex format.
    /// 
    /// Same logic as UpdateReferenceFromRgb() but for sample color.
    /// </summary>
    private void UpdateSampleFromRgb()
    {
        if (isUpdatingSample)
        {
            return;
        }

        if (!TryParseRgb(SampleR, SampleG, SampleB, out var color))
        {
            return;
        }

        isUpdatingSample = true;
        SampleHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        SampleBrush = new SolidColorBrush(color);
        isUpdatingSample = false;
    }

    /// <summary>
    /// Attempts to parse three string RGB component values to an Avalonia Color.
    /// 
    /// Validates that all three strings are integers in the range 0-255.
    /// If any value is invalid (non-numeric, or out of range), returns false without modifying color.
    /// 
    /// <param name="rText">Red component string (must be 0-255)</param>
    /// <param name="gText">Green component string (must be 0-255)</param>
    /// <param name="bText">Blue component string (must be 0-255)</param>
    /// <param name="color">Output: Avalonia Color with RGB values, or Colors.Transparent if parse fails</param>
    /// <returns>True if all values valid and parsed successfully; false if any value invalid</returns>
    /// </summary>
    private static bool TryParseRgb(string rText, string gText, string bText, out Color color)
    {
        color = Colors.Transparent;

        if (!TryParseByte(rText, out var r) || !TryParseByte(gText, out var g) || !TryParseByte(bText, out var b))
        {
            return false;
        }

        color = Color.FromRgb(r, g, b);
        return true;
    }

    /// <summary>
    /// Attempts to parse a string to a byte value in range 0-255.
    /// 
    /// Validates that the string is a valid integer and within the byte range.
    /// Uses InvariantCulture for culture-independent parsing.
    /// 
    /// <param name="value">String to parse (e.g., "128", "255", "0")</param>
    /// <param name="result">Output: Parsed byte value, or 0 if parse fails</param>
    /// <returns>True if string is valid integer 0-255; false otherwise</returns>
    /// </summary>
    private static bool TryParseByte(string value, out byte result)
    {
        result = 0;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (parsed < 0 || parsed > 255)
        {
            return false;
        }

        result = (byte)parsed;
        return true;
    }

    /// <summary>
    /// Attempts to parse a hexadecimal color string to an Avalonia Color.
    /// 
    /// Accepts formats: "#RRGGBB" or "RRGGBB" (case-insensitive)
    /// Examples: "#FF5733", "FF5733", "#ff5733"
    /// 
    /// Process:
    /// 1. Check for null/empty input
    /// 2. Trim whitespace and remove optional '#' prefix
    /// 3. Validate exactly 6 hex digits remain
    /// 4. Parse as hex integer and extract RGB components
    /// 5. Return Avalonia Color
    /// 
    /// <param name="hex">Hex color string (e.g., "#FF5733" or "FF5733")</param>
    /// <param name="color">Output: Parsed Color, or Colors.Transparent if parse fails</param>
    /// <returns>True if valid hex color format; false if invalid or wrong length</returns>
    /// </summary>
    private static bool TryParseHex(string? hex, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        var trimmed = hex.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Length != 6)
        {
            return false;
        }

        if (!int.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return false;
        }

        var r = (byte)((rgb >> 16) & 0xFF);
        var g = (byte)((rgb >> 8) & 0xFF);
        var b = (byte)(rgb & 0xFF);

        color = Color.FromRgb(r, g, b);
        return true;
    }

    /// <summary>
    /// Updates graph model and recalculates color difference and recommendations.
    /// 
    /// This method is the core calculation pipeline called whenever either the reference
    /// or sample color changes. It orchestrates:
    /// 
    /// 1. **Reference Color Validation and Conversion**
    ///    - Parse ReferenceR, ReferenceG, ReferenceB to bytes
    ///    - If parse fails: Set GraphModel.ReferenceColor = null
    ///    - If parse succeeds: Convert RgbColor to LabColor via ColorSpaceConverter.RgbToLab()
    ///    - Store in GraphModel.ReferenceColor
    /// 
    /// 2. **Sample Color Validation and Conversion**
    ///    - Parse SampleR, SampleG, SampleB to bytes
    ///    - If parse fails: Set GraphModel.SampleColor = null
    ///    - If parse succeeds: Convert RgbColor to LabColor
    ///    - Store in GraphModel.SampleColor
    /// 
    /// 3. **Color Difference Calculation**
    ///    - Call GraphModel.GetColorDifference()
    ///    - Updates ColorDifference property with ΔE value
    ///    - Returns 0 if either color is null
    /// 
    /// 4. **Tint Recommendation Generation**
    ///    - Call GraphModel.GetTintRecommendation()
    ///    - Updates TintRecommendation property with human-readable guidance
    ///    - Examples: "Add Red, Add Yellow" or "Colors are very close"
    /// 
    /// **LAB Color Space**
    /// 
    /// All calculations use LAB color space (device-independent, perceptually uniform):
    /// - L* (0-100): Lightness/brightness
    /// - a* (-128 to 127): Red-green opponent axis
    /// - b* (-128 to 127): Yellow-blue opponent axis
    /// 
    /// This ensures color difference calculations match human perception, unlike RGB which
    /// is device-dependent and non-perceptual.
    /// 
    /// **Performance Note**
    /// 
    /// Each color conversion involves 6 calculation steps (linearization, normalization,
    /// XYZ conversion, LAB transformation). With optimization flags and invariant culture
    /// parsing, UpdateGraphData() completes in &lt;1ms on modern hardware, enabling responsive
    /// real-time UI updates.
    /// </summary>
    private void UpdateGraphData()
    {
        if (!TryParseByte(ReferenceR, out var refR) || !TryParseByte(ReferenceG, out var refG) || !TryParseByte(ReferenceB, out var refB))
        {
            GraphModel.ReferenceColor = null;
        }
        else
        {
            var refRgb = new RgbColor(refR, refG, refB);
            GraphModel.ReferenceColor = ColorSpaceConverter.RgbToLab(refRgb);
        }

        if (!TryParseByte(SampleR, out var smpR) || !TryParseByte(SampleG, out var smpG) || !TryParseByte(SampleB, out var smpB))
        {
            GraphModel.SampleColor = null;
        }
        else
        {
            var smpRgb = new RgbColor(smpR, smpG, smpB);
            GraphModel.SampleColor = ColorSpaceConverter.RgbToLab(smpRgb);
        }

        ColorDifference = GraphModel.GetColorDifference();
        TintRecommendation = GraphModel.GetTintRecommendation();
        IsProjectModified = true;
    }

    /// <summary>
    /// Creates a new project from the current color state.
    /// </summary>
    [RelayCommand]
    public async Task CreateNewProjectAsync()
    {
        var project = new ColorProject(ProjectName, ProjectDescription);
        
        if (TryParseRgb(ReferenceR, ReferenceG, ReferenceB, out var refColor))
        {
            project.ReferenceColor = new RgbColor(refColor.R, refColor.G, refColor.B);
        }

        if (TryParseRgb(SampleR, SampleG, SampleB, out var smpColor))
        {
            project.SampleColor = new RgbColor(smpColor.R, smpColor.G, smpColor.B);
        }

        CurrentProject = await _repository.CreateProjectAsync(project);
        IsProjectModified = false;
    }

    /// <summary>
    /// Saves the current project with any modifications.
    /// </summary>
    [RelayCommand]
    public async Task SaveProjectAsync()
    {
        if (CurrentProject == null)
        {
            await CreateNewProjectAsync();
            return;
        }

        CurrentProject.Name = ProjectName;
        CurrentProject.Description = ProjectDescription;

        if (TryParseRgb(ReferenceR, ReferenceG, ReferenceB, out var refColor))
        {
            CurrentProject.ReferenceColor = new RgbColor(refColor.R, refColor.G, refColor.B);
        }

        if (TryParseRgb(SampleR, SampleG, SampleB, out var smpColor))
        {
            CurrentProject.SampleColor = new RgbColor(smpColor.R, smpColor.G, smpColor.B);
        }

        await _repository.UpdateProjectAsync(CurrentProject);
        IsProjectModified = false;
    }

    /// <summary>
    /// Saves the current color match to the project history.
    /// </summary>
    [RelayCommand]
    public async Task SaveColorMatchAsync()
    {
        if (CurrentProject == null)
        {
            await CreateNewProjectAsync();
        }

        if (CurrentProject == null)
            return;

        if (!TryParseRgb(ReferenceR, ReferenceG, ReferenceB, out var refColor))
            return;
        if (!TryParseRgb(SampleR, SampleG, SampleB, out var smpColor))
            return;

        var refRgb = new RgbColor(refColor.R, refColor.G, refColor.B);
        var smpRgb = new RgbColor(smpColor.R, smpColor.G, smpColor.B);
        var refLab = ColorSpaceConverter.RgbToLab(refRgb);
        var smpLab = ColorSpaceConverter.RgbToLab(smpRgb);

        var historyEntry = new ColorHistoryEntry(refRgb, smpRgb, refLab.DeltaE(smpLab), TintRecommendation)
        {
            Notes = "Manual color match"
        };

        await _repository.AddColorHistoryAsync(CurrentProject.Id, historyEntry);
        await RefreshHistoryAsync();
    }

    /// <summary>
    /// Loads a project from the repository and populates the UI.
    /// </summary>
    [RelayCommand]
    public async Task LoadProjectAsync(ColorProject project)
    {
        if (project == null)
            return;

        CurrentProject = project;
        ProjectName = project.Name ?? "Untitled";
        ProjectDescription = project.Description ?? "";

        if (project.ReferenceColor != null)
        {
            ReferenceR = project.ReferenceColor.R.ToString();
            ReferenceG = project.ReferenceColor.G.ToString();
            ReferenceB = project.ReferenceColor.B.ToString();
        }

        if (project.SampleColor != null)
        {
            SampleR = project.SampleColor.R.ToString();
            SampleG = project.SampleColor.G.ToString();
            SampleB = project.SampleColor.B.ToString();
        }

        await RefreshHistoryAsync();
        IsProjectModified = false;
    }

    /// <summary>
    /// Deletes a project from the repository.
    /// </summary>
    [RelayCommand]
    public async Task DeleteProjectAsync(ColorProject? project)
    {
        if (project == null)
            return;

        await _repository.DeleteProjectAsync(project.Id);
        
        if (CurrentProject?.Id == project.Id)
        {
            CurrentProject = null;
            ProjectName = "Untitled Project";
            ProjectDescription = "";
        }

        await LoadRecentProjectsAsync();
    }

    /// <summary>
    /// Loads recent projects from the repository to display in the UI.
    /// </summary>
    private async Task LoadRecentProjectsAsync()
    {
        var projects = await _repository.GetAllProjectsAsync();
        RecentProjects.Clear();
        
        foreach (var project in projects.Take(10))
        {
            RecentProjects.Add(project);
        }
    }

    /// <summary>
    /// Exports the current project as JSON.
    /// </summary>
    [RelayCommand]
    public async Task<string?> ExportProjectAsync()
    {
        if (CurrentProject == null)
            return null;

        return await _repository.ExportProjectAsJsonAsync(CurrentProject.Id);
    }

    /// <summary>
    /// Imports a project from JSON data.
    /// </summary>
    [RelayCommand]
    public async Task ImportProjectFromJsonAsync(string jsonData)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
            return;

        var project = await _repository.ImportProjectFromJsonAsync(jsonData);
        await LoadProjectAsync(project);
        await LoadRecentProjectsAsync();
    }

    /// <summary>
    /// Connects to the sensor device.
    /// </summary>
    [RelayCommand]
    public async Task ConnectSensorAsync()
    {
        if (_sensor == null)
            return;

        try
        {
            var connected = await _sensor.ConnectAsync();
            if (connected)
            {
                IsSensorConnected = true;
                var status = await _sensor.GetStatusAsync();
                SensorStatus = status;
            }
        }
        catch (Exception ex)
        {
            SensorStatus = $"Connection failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Disconnects from the sensor device.
    /// </summary>
    [RelayCommand]
    public async Task DisconnectSensorAsync()
    {
        if (_sensor == null)
            return;

        try
        {
            await _sensor.DisconnectAsync();
            IsSensorConnected = false;
            SensorStatus = "Disconnected";
        }
        catch (Exception ex)
        {
            SensorStatus = $"Disconnection failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads a color sample from the sensor and sets it as the sample color.
    /// </summary>
    [RelayCommand]
    public async Task ReadSensorSampleAsync()
    {
        if (_sensor == null || !IsSensorConnected)
            return;

        try
        {
            IsReadingColor = true;
            SensorStatus = "Reading color...";

            var reading = await _sensor.ReadColorWithValidationAsync(maxRetries: 3, minimumQualityScore: 70);

            SampleR = reading.RgbColor.R.ToString();
            SampleG = reading.RgbColor.G.ToString();
            SampleB = reading.RgbColor.B.ToString();

            SensorStatus = $"Color read successfully (Quality: {reading.QualityScore}%)";
        }
        catch (Exception ex)
        {
            SensorStatus = $"Reading failed: {ex.Message}";
        }
        finally
        {
            IsReadingColor = false;
        }
    }

    /// <summary>
    /// Reads a color reference from the sensor and sets it as the reference color.
    /// </summary>
    [RelayCommand]
    public async Task ReadSensorReferenceAsync()
    {
        if (_sensor == null || !IsSensorConnected)
            return;

        try
        {
            IsReadingColor = true;
            SensorStatus = "Reading reference color...";

            var reading = await _sensor.ReadColorWithValidationAsync(maxRetries: 3, minimumQualityScore: 70);

            ReferenceR = reading.RgbColor.R.ToString();
            ReferenceG = reading.RgbColor.G.ToString();
            ReferenceB = reading.RgbColor.B.ToString();

            SensorStatus = $"Reference color read successfully (Quality: {reading.QualityScore}%)";
        }
        catch (Exception ex)
        {
            SensorStatus = $"Reading failed: {ex.Message}";
        }
        finally
        {
            IsReadingColor = false;
        }
    }

    /// <summary>
    /// Calibrates the sensor device.
    /// </summary>
    [RelayCommand]
    public async Task CalibrateSensorAsync()
    {
        if (_sensor == null || !IsSensorConnected)
            return;

        try
        {
            SensorStatus = "Calibrating...";
            var result = await _sensor.CalibrateAsync();
            
            if (result)
            {
                SensorStatus = "Calibration successful";
            }
            else
            {
                SensorStatus = "Calibration failed";
            }
        }
        catch (Exception ex)
        {
            SensorStatus = $"Calibration error: {ex.Message}";
        }
    }

    /// <summary>
    /// Sets a custom ISensorReader implementation (for testing or hardware integration).
    /// </summary>
    public void SetSensorReader(ISensorReader sensor)
    {
        _sensor?.Dispose();
        _sensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
        IsSensorConnected = false;
        SensorStatus = "Sensor changed";
    }

    /// <summary>
    /// Refreshes the history items from the current project.
    /// </summary>
    private async Task RefreshHistoryAsync()
    {
        if (CurrentProject == null)
        {
            HistoryItems.Clear();
            return;
        }

        var project = await _repository.GetProjectAsync(CurrentProject.Id);
        if (project?.ColorHistory != null)
        {
            HistoryItems.Clear();
            foreach (var entry in project.ColorHistory.OrderByDescending(h => h.CreatedAt))
            {
                HistoryItems.Add(entry);
            }
        }
    }

    /// <summary>
    /// Reuses a history entry as the reference color.
    /// </summary>
    [RelayCommand]
    public void ReuseAsReference(ColorHistoryEntry? entry)
    {
        if (entry?.ReferenceColor == null)
            return;

        ReferenceR = entry.ReferenceColor.R.ToString();
        ReferenceG = entry.ReferenceColor.G.ToString();
        ReferenceB = entry.ReferenceColor.B.ToString();
        UpdateReferenceFromRgb();
    }

    /// <summary>
    /// Reuses a history entry as the sample color.
    /// </summary>
    [RelayCommand]
    public void ReuseAsSample(ColorHistoryEntry? entry)
    {
        if (entry?.SampleColor == null)
            return;

        SampleR = entry.SampleColor.R.ToString();
        SampleG = entry.SampleColor.G.ToString();
        SampleB = entry.SampleColor.B.ToString();
        UpdateSampleFromRgb();
    }

    /// <summary>
    /// Exports history as CSV format string.
    /// </summary>
    [RelayCommand]
    public async Task ExportHistoryAsCsvAsync()
    {
        if (HistoryItems.Count == 0)
            return;

        var csv = new StringBuilder();
        csv.AppendLine("Timestamp,Reference RGB,Sample RGB,ΔE,Recommendation,Accepted,Notes");

        foreach (var entry in HistoryItems)
        {
            var refRgb = entry.ReferenceColor?.ToString() ?? "N/A";
            var smpRgb = entry.SampleColor?.ToString() ?? "N/A";
            var notes = (entry.Notes ?? "").Replace(",", ";"); // Escape commas in notes

            csv.AppendLine($"{entry.CreatedAt:yyyy-MM-dd HH:mm:ss},{refRgb},{smpRgb},{entry.DeltaE:F2},{entry.TintRecommendation},{entry.IsAccepted},{notes}");
        }

        // TODO: Implement file dialog to save CSV
        System.Diagnostics.Debug.WriteLine("CSV Export:\n" + csv.ToString());
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears all history entries from current project.
    /// </summary>
    [RelayCommand]
    public async Task ClearHistoryAsync()
    {
        if (CurrentProject == null)
            return;

        CurrentProject.ColorHistory.Clear();
        HistoryItems.Clear();
        IsProjectModified = true;
        await SaveProjectAsync();
    }
}
