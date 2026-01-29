using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ColorMatcher.Models;

namespace ColorMatcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private IColorRepository _repository;

    [ObservableProperty]
    private string referenceHex = "#FFFFFF";

    [ObservableProperty]
    private string referenceR = "255";

    [ObservableProperty]
    private string referenceG = "255";

    [ObservableProperty]
    private string referenceB = "255";

    [ObservableProperty]
    private SolidColorBrush referenceBrush = new(Colors.White);

    [ObservableProperty]
    private string sampleHex = "#808080";

    [ObservableProperty]
    private string sampleR = "128";

    [ObservableProperty]
    private string sampleG = "128";

    [ObservableProperty]
    private string sampleB = "128";

    [ObservableProperty]
    private SolidColorBrush sampleBrush = new(Colors.Gray);

    [ObservableProperty]
    private LabColorGraphModel graphModel = new();

    [ObservableProperty]
    private double colorDifference = 0;

    [ObservableProperty]
    private string tintRecommendation = "Enter both colors";

    [ObservableProperty]
    private ColorProject? currentProject;

    [ObservableProperty]
    private string projectName = "Untitled Project";

    [ObservableProperty]
    private string projectDescription = "";

    [ObservableProperty]
    private ObservableCollection<ColorProject> recentProjects = new();

    [ObservableProperty]
    private bool isProjectModified = false;

    private bool isUpdatingReference;
    private bool isUpdatingSample;

    public MainWindowViewModel()
    {
        // Initialize with in-memory repository (can be switched to FileColorRepository)
        _repository = new InMemoryColorRepository();
    }

    /// <summary>
    /// Initialize the ViewModel with a file-based repository (should be called on app startup).
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

    partial void OnReferenceRChanged(string value)
    {
        UpdateReferenceFromRgb();
        UpdateGraphData();
    }

    partial void OnReferenceGChanged(string value)
    {
        UpdateReferenceFromRgb();
        UpdateGraphData();
    }

    partial void OnReferenceBChanged(string value)
    {
        UpdateReferenceFromRgb();
        UpdateGraphData();
    }

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

    partial void OnSampleRChanged(string value)
    {
        UpdateSampleFromRgb();
        UpdateGraphData();
    }

    partial void OnSampleGChanged(string value)
    {
        UpdateSampleFromRgb();
        UpdateGraphData();
    }

    partial void OnSampleBChanged(string value)
    {
        UpdateSampleFromRgb();
        UpdateGraphData();
    }

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
}
