using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ColorMatcher.Models;

namespace ColorMatcher.Views;

public partial class LabColorGraph : UserControl
{
    private LabColorGraphModel? _model;
    private const double CenterX = 150;
    private const double CenterY = 150;
    private const double Scale = 0.8; // pixels per LAB unit

    public LabColorGraph()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }

    /// <summary>
    /// Sets the data model and redraws the graph.
    /// </summary>
    public void SetData(LabColorGraphModel model)
    {
        _model = model;
        Redraw();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void Redraw()
    {
        var canvas = this.FindControl<Canvas>("GraphCanvas");
        if (canvas == null)
            return;

        canvas.Children.Clear();

        // Draw coordinate system
        DrawCoordinateSystem(canvas);

        // Draw grid
        DrawGrid(canvas);

        // Draw points and vector if data exists
        if (_model?.ReferenceColor != null)
        {
            DrawReferencePoint(canvas, _model.ReferenceColor);
        }

        if (_model?.SampleColor != null)
        {
            DrawSamplePoint(canvas, _model.SampleColor);
        }

        // Draw connecting line if both points exist
        if (_model?.ReferenceColor != null && _model?.SampleColor != null)
        {
            DrawVectorLine(canvas, _model.ReferenceColor, _model.SampleColor);
        }

        // Draw legend
        DrawLegend(canvas);
    }

    private void DrawCoordinateSystem(Canvas canvas)
    {
        var centerX = CenterX;
        var centerY = CenterY;

        // X-axis (a-axis: green to red)
        canvas.Children.Add(new Line
        {
            StartPoint = new Point(centerX - 120, centerY),
            EndPoint = new Point(centerX + 120, centerY),
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 1,
            Opacity = 0.5
        });

        // Y-axis (b-axis: blue to yellow)
        canvas.Children.Add(new Line
        {
            StartPoint = new Point(centerX, centerY - 120),
            EndPoint = new Point(centerX, centerY + 120),
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 1,
            Opacity = 0.5
        });

        // Axis labels
        AddText(canvas, centerX + 130, centerY - 5, "a (Red→Green)", 10);
        AddText(canvas, centerX - 15, centerY - 135, "b (Blue→Yellow)", 10);
    }

    private void DrawGrid(Canvas canvas)
    {
        var centerX = CenterX;
        var centerY = CenterY;

        // Draw grid lines for a-axis (-128 to 128)
        for (int i = -120; i <= 120; i += 30)
        {
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(centerX + i, centerY - 3),
                EndPoint = new Point(centerX + i, centerY + 3),
                Stroke = new SolidColorBrush(Colors.DarkGray),
                StrokeThickness = 1
            });
        }

        // Draw grid lines for b-axis
        for (int i = -120; i <= 120; i += 30)
        {
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(centerX - 3, centerY + i),
                EndPoint = new Point(centerX + 3, centerY + i),
                Stroke = new SolidColorBrush(Colors.DarkGray),
                StrokeThickness = 1
            });
        }
    }

    private void DrawReferencePoint(Canvas canvas, LabColor color)
    {
        var x = CenterX + (color.A * Scale);
        var y = CenterY + (color.B * Scale);

        // Draw circle
        var ellipse = new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Colors.LimeGreen),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        Canvas.SetLeft(ellipse, x - 6);
        Canvas.SetTop(ellipse, y - 6);
        canvas.Children.Add(ellipse);

        // Label
        AddText(canvas, x + 8, y - 12, "Reference", 9);
    }

    private void DrawSamplePoint(Canvas canvas, LabColor color)
    {
        var x = CenterX + (color.A * Scale);
        var y = CenterY + (color.B * Scale);

        // Draw square
        var rect = new Rectangle
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(Colors.Orange),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        Canvas.SetLeft(rect, x - 6);
        Canvas.SetTop(rect, y - 6);
        canvas.Children.Add(rect);

        // Label
        AddText(canvas, x + 8, y + 2, "Sample", 9);
    }

    private void DrawVectorLine(Canvas canvas, LabColor reference, LabColor sample)
    {
        var x1 = CenterX + (reference.A * Scale);
        var y1 = CenterY + (reference.B * Scale);
        var x2 = CenterX + (sample.A * Scale);
        var y2 = CenterY + (sample.B * Scale);

        // Draw dashed line
        canvas.Children.Add(new Line
        {
            StartPoint = new Point(x1, y1),
            EndPoint = new Point(x2, y2),
            Stroke = new SolidColorBrush(Colors.Cyan),
            StrokeThickness = 2,
            StrokeDashArray = new AvaloniaList<double> { 4, 4 }
        });
    }

    private void DrawLegend(Canvas canvas)
    {
        if (_model == null)
            return;

        var deltaE = _model.GetColorDifference();
        var recommendation = _model.GetTintRecommendation();

        AddText(canvas, 10, 10, $"ΔE: {deltaE:F2}", 10);
        AddText(canvas, 10, 25, $"Recommendation: {recommendation}", 9);
    }

    private static void AddText(Canvas canvas, double x, double y, string text, int fontSize)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = new SolidColorBrush(Colors.White),
            Opacity = 0.8
        };
        Canvas.SetLeft(textBlock, x);
        Canvas.SetTop(textBlock, y);
        canvas.Children.Add(textBlock);
    }
}
