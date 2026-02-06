using CommunityToolkit.Mvvm.ComponentModel;

namespace ColorMatcher.ViewModels
{
/// <summary>
/// Base class for all view models in the ColorMatcher application.
/// 
/// Inherits from MVVM Toolkit's ObservableObject, providing automatic INotifyPropertyChanged
/// support for property change notifications to the Avalonia UI framework.
/// 
/// <remarks>
/// **MVVM Pattern in ColorMatcher**
/// 
/// This application uses the Model-View-ViewModel (MVVM) architectural pattern:
/// 
/// - **Model**: Color data, business logic (ColorProject, ColorHistoryEntry, ColorSpaceConverter)
/// - **ViewModel**: State management, command handling (MainWindowViewModel)
/// - **View**: UI presentation layer (Avalonia XAML and code-behind)
/// 
/// **ViewModelBase Responsibilities**
/// 
/// By inheriting from ObservableObject, all view models automatically get:
/// 1. INotifyPropertyChanged implementation for property change notifications
/// 2. Relay command support via [RelayCommand] attribute
/// 3. Observable property support via [ObservableProperty] attribute
/// 4. Built-in change detection for MVVM Toolkit features
/// 
/// **Usage**
/// 
/// All view model classes should inherit from ViewModelBase and use [ObservableProperty]
/// for properties that need to notify the UI:
/// 
/// ```csharp
/// public partial class MyViewModel : ViewModelBase
/// {
///     // Automatically generates INotifyPropertyChanged notifications
///     [ObservableProperty]
///     private string myProperty = "initial value";
///     
///     // Automatically generates ICommand from method
///     [RelayCommand]
///     private async Task MyCommandAsync()
///     {
///         // Command logic
///     }
/// }
/// ```
/// 
/// **Why MVVM Toolkit**
/// 
/// Rather than manual INotifyPropertyChanged implementation with backing fields,
/// the MVVM Toolkit uses source generation (C# 11) to:
/// - Eliminate boilerplate code
/// - Provide compile-time safety
/// - Enable clean, readable view model code
/// </remarks>
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
}
