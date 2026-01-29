using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ColorMatcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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

    private bool isUpdatingReference;
    private bool isUpdatingSample;

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
    }

    partial void OnReferenceRChanged(string value) => UpdateReferenceFromRgb();
    partial void OnReferenceGChanged(string value) => UpdateReferenceFromRgb();
    partial void OnReferenceBChanged(string value) => UpdateReferenceFromRgb();

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

    partial void OnSampleRChanged(string value) => UpdateSampleFromRgb();
    partial void OnSampleGChanged(string value) => UpdateSampleFromRgb();
    partial void OnSampleBChanged(string value) => UpdateSampleFromRgb();

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
}
