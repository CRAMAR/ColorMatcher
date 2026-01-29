using System;
using Avalonia.Controls;
using ColorMatcher.ViewModels;

namespace ColorMatcher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var graph = this.FindControl<LabColorGraph>("LabGraph");
            if (graph != null)
            {
                // Initial setup
                graph.SetData(vm.GraphModel);

                // Subscribe to changes
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.GraphModel))
                    {
                        graph.SetData(vm.GraphModel);
                    }
                };
            }
        }
    }
}