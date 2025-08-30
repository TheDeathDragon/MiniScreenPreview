using Microsoft.Win32;
using MiniScreenPreview.Models;
using MiniScreenPreview.Services;
using MiniScreenPreview.ViewModels;
using MiniScreenPreview.Windows;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MiniScreenPreview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DataService _dataService;

        // Static commands for keyboard shortcuts
        public static readonly RoutedCommand NewProjectCommand = new RoutedCommand();
        public static readonly RoutedCommand OpenProjectCommand = new RoutedCommand();
        public static readonly RoutedCommand SaveProjectCommand = new RoutedCommand();
        public static readonly RoutedCommand SaveProjectAsCommand = new RoutedCommand();
        public static readonly RoutedCommand ExitCommand = new RoutedCommand();
        public static readonly RoutedCommand ShowAllInfoCommand = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _dataService = new DataService();
            DataContext = _viewModel;

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;

            // Bind keyboard shortcuts to command handlers
            CommandBindings.Add(new CommandBinding(NewProjectCommand, (s, e) => NewProject_Click(s, e)));
            CommandBindings.Add(new CommandBinding(OpenProjectCommand, (s, e) => OpenProject_Click(s, e)));
            CommandBindings.Add(new CommandBinding(SaveProjectCommand, (s, e) => SaveProject_Click(s, e)));
            CommandBindings.Add(new CommandBinding(SaveProjectAsCommand, (s, e) => SaveProjectAs_Click(s, e)));
            CommandBindings.Add(new CommandBinding(ExitCommand, (s, e) => Exit_Click(s, e)));
            CommandBindings.Add(new CommandBinding(ShowAllInfoCommand, (s, e) => ShowAllInfo_Click(s, e)));
        }

        private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveImageUp();
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveImageDown();
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var lastOpenedPath = await _dataService.LoadLastOpenedFilePathAsync();
            if (!string.IsNullOrEmpty(lastOpenedPath))
            {
                await LoadProject(lastOpenedPath);
            }
        }

        private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        await SaveCurrentProject();
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
        }

        private void ResetProperties_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedImageResource != null)
            {
                var selectedImage = _viewModel.SelectedImageResource;

                // Reset to default values
                selectedImage.X = 0;
                selectedImage.Y = 0;
                selectedImage.Scale = 1.0;
                selectedImage.Rotation = 0.0;
                selectedImage.Opacity = 1.0;
                selectedImage.IsVisible = true;
            }
        }

        private void ShowAllInfo_Click(object sender, RoutedEventArgs e)
        {
            var infoWindow = new InfoWindow(_viewModel)
            {
                Owner = this
            };
            infoWindow.ShowDialog();
        }

        #region Menu Event Handlers

        private async void NewProject_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before creating a new project?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        await SaveCurrentProject();
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }

            _viewModel.NewProject();
        }

        private async void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before opening another project?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        await SaveCurrentProject();
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Mini Screen Preview Files (*.mspj)|*.mspj|All Files (*.*)|*.*",
                Title = "Open Project"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadProject(openFileDialog.FileName);
            }
        }

        private async void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            await SaveCurrentProject();
        }

        private async void SaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            await SaveProjectAs();
        }

        private async void CloseProject_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        await SaveCurrentProject();
                        break;
                    case MessageBoxResult.Cancel:
                        return;
                }
            }

            _viewModel.NewProject();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Mini Screen Preview\n" +
                "Version 1.0\n\n" +
                "A tool for designing layouts for secondary screen displays.\n\n" +
                "Features:\n" +
                "• Drag and drop image positioning\n" +
                "• Real-time preview with zoom\n" +
                "• Layer management\n" +
                "• Rotation, scaling, and opacity controls\n" +
                "• Mouse wheel and keyboard shortcuts\n" +
                "• Project save/load functionality\n\n" +
                "© 2025 Shiro",
                "About Mini Screen Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        private void SetBorderColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string color)
            {
                _viewModel.SelectionBorderColor = color;
            }
        }

        #endregion

        #region Drag and Drop

        private void ImageListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Any(IsImageFile))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void ImageListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Any(IsImageFile))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void ImageListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var imageFiles = files.Where(IsImageFile).ToArray();
                
                foreach (var fileName in imageFiles)
                {
                    var imageResource = new ImageResource
                    {
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        FilePath = fileName,
                        Layer = _viewModel.ImageResources.Count
                    };
                    _viewModel.ImageResources.Add(imageResource);
                }
            }
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || 
                   extension == ".bmp" || extension == ".gif" || extension == ".tiff";
        }

        #endregion

        #region Project Management

        private async Task SaveCurrentProject()
        {
            if (string.IsNullOrEmpty(_viewModel.CurrentProjectPath))
            {
                await SaveProjectAs();
            }
            else
            {
                await _dataService.SaveProjectDataAsync(_viewModel, _viewModel.CurrentProjectPath!);
                _viewModel.SaveOriginalState();
            }
        }

        private async Task SaveProjectAs()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Mini Screen Preview Files (*.mspj)|*.mspj|All Files (*.*)|*.*",
                Title = "Save Project As",
                DefaultExt = "mspj"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await _dataService.SaveProjectDataAsync(_viewModel, saveFileDialog.FileName);
                _viewModel.SetProjectPath(saveFileDialog.FileName);
                await _dataService.SaveLastOpenedFilePathAsync(saveFileDialog.FileName);
            }
        }

        private async Task LoadProject(string filePath)
        {
            try
            {
                var projectData = await _dataService.LoadProjectDataAsync(filePath);
                if (projectData != null)
                {
                    _dataService.ApplyProjectData(projectData, _viewModel);
                    _viewModel.SetProjectPath(filePath);
                    await _dataService.SaveLastOpenedFilePathAsync(filePath);
                }
                else
                {
                    MessageBox.Show("Failed to load project file.", "Load Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project: {ex.Message}", "Load Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    public static class BooleanConverter
    {
        public static IValueConverter IsNotNull { get; } = new IsNotNullConverter();
        public static IValueConverter StringEquality { get; } = new StringEqualityConverter();
        public static IValueConverter Inverted { get; } = new InvertedBooleanConverter();
    }

    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter != null)
            {
                return parameter.ToString() ?? string.Empty;
            }
            return Binding.DoNothing;
        }
    }

    public class InvertedBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}