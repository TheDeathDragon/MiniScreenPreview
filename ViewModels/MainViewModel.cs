using Microsoft.Win32;
using MiniScreenPreview.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace MiniScreenPreview.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ImageResource> _imageResources;
        private ImageResource? _selectedImageResource;
        private double _previewWidth;
        private double _previewHeight;
        private double _zoomLevel;
        private ICommand? _addImageCommand;
        private ICommand? _removeImageCommand;
        private ObservableCollection<SizePreset> _sizePresets;
        private SizePreset? _selectedSizePreset;
        private string? _currentProjectPath;
        private bool _hasUnsavedChanges;
        private string? _originalStateHash;
        private bool _showSelectionBorder = true;
        private string _selectionBorderColor = "Red";

        public ObservableCollection<ImageResource> ImageResources
        {
            get => _imageResources;
            set
            {
                _imageResources = value;
                OnPropertyChanged();
            }
        }

        public ImageResource? SelectedImageResource
        {
            get => _selectedImageResource;
            set
            {
                _selectedImageResource = value;
                OnPropertyChanged();
            }
        }

        public double PreviewWidth
        {
            get => _previewWidth;
            set
            {
                _previewWidth = Math.Max(100, value);
                OnPropertyChanged();
                UpdateSelectedPreset();
                CheckForUnsavedChanges();
            }
        }

        public double PreviewHeight
        {
            get => _previewHeight;
            set
            {
                _previewHeight = Math.Max(100, value);
                OnPropertyChanged();
                UpdateSelectedPreset();
                CheckForUnsavedChanges();
            }
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = Math.Max(0.1, Math.Min(5.0, value));
                OnPropertyChanged();
                CheckForUnsavedChanges();
            }
        }

        public ICommand AddImageCommand
        {
            get => _addImageCommand ??= new RelayCommand(ExecuteAddImage);
        }

        public ICommand RemoveImageCommand
        {
            get => _removeImageCommand ??= new RelayCommand(ExecuteRemoveImage, CanRemoveImage);
        }

        public ObservableCollection<SizePreset> SizePresets
        {
            get => _sizePresets;
            set
            {
                _sizePresets = value;
                OnPropertyChanged();
            }
        }

        public SizePreset? SelectedSizePreset
        {
            get => _selectedSizePreset;
            set
            {
                _selectedSizePreset = value;
                OnPropertyChanged();
                ApplySizePreset();
            }
        }

        public string? CurrentProjectPath
        {
            get => _currentProjectPath;
            set
            {
                _currentProjectPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                _hasUnsavedChanges = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public string WindowTitle
        {
            get
            {
                var title = "Mini Screen Preview";
                if (!string.IsNullOrEmpty(_currentProjectPath))
                {
                    var fileName = Path.GetFileName(_currentProjectPath);
                    title = $"{fileName} - {title}";
                    if (_hasUnsavedChanges)
                        title = $"*{title}";
                }
                else if (_hasUnsavedChanges)
                {
                    title = $"*Untitled - {title}";
                }
                return title;
            }
        }

        public bool ShowSelectionBorder
        {
            get => _showSelectionBorder;
            set
            {
                _showSelectionBorder = value;
                OnPropertyChanged();
            }
        }

        public string SelectionBorderColor
        {
            get => _selectionBorderColor;
            set
            {
                _selectionBorderColor = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            _imageResources = new ObservableCollection<ImageResource>();
            _previewWidth = 360;
            _previewHeight = 360;
            _zoomLevel = 1.0;

            _sizePresets = new ObservableCollection<SizePreset>
            {
                new SizePreset("240x240", 240, 240),
                new SizePreset("240x280", 240, 280),
                new SizePreset("240x284", 240, 284),
                new SizePreset("360x360", 360, 360)
            };

            _selectedSizePreset = _sizePresets.FirstOrDefault(p => p.Width == 360 && p.Height == 360);

            _imageResources.CollectionChanged += OnImageResourcesCollectionChanged;

            SaveOriginalState();
        }

        private void ExecuteAddImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.webp;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    var imageResource = new ImageResource
                    {
                        Name = Path.GetFileNameWithoutExtension(fileName),
                        FilePath = fileName,
                        Layer = ImageResources.Count
                    };
                    ImageResources.Add(imageResource);
                }
            }
        }

        private void ExecuteRemoveImage()
        {
            if (SelectedImageResource != null)
            {
                ImageResources.Remove(SelectedImageResource);
                SelectedImageResource = null;
            }
        }

        private bool CanRemoveImage()
        {
            return SelectedImageResource != null;
        }

        public void MoveImageUp()
        {
            if (SelectedImageResource != null && SelectedImageResource.Layer < ImageResources.Max(r => r.Layer))
            {
                var currentLayer = SelectedImageResource.Layer;
                var upperImage = ImageResources.FirstOrDefault(r => r.Layer == currentLayer + 1);
                if (upperImage != null)
                {
                    upperImage.Layer = currentLayer;
                    SelectedImageResource.Layer = currentLayer + 1;
                    RefreshImageResourcesOrder();
                }
            }
        }

        public void MoveImageDown()
        {
            if (SelectedImageResource != null && SelectedImageResource.Layer > ImageResources.Min(r => r.Layer))
            {
                var currentLayer = SelectedImageResource.Layer;
                var lowerImage = ImageResources.FirstOrDefault(r => r.Layer == currentLayer - 1);
                if (lowerImage != null)
                {
                    lowerImage.Layer = currentLayer;
                    SelectedImageResource.Layer = currentLayer - 1;
                    RefreshImageResourcesOrder();
                }
            }
        }

        private void RefreshImageResourcesOrder()
        {
            var selectedItem = SelectedImageResource;
            var sortedItems = ImageResources.OrderBy(r => r.Layer).ToList();

            ImageResources.Clear();
            foreach (var item in sortedItems)
            {
                ImageResources.Add(item);
            }

            SelectedImageResource = selectedItem;
        }

        private void ApplySizePreset()
        {
            if (_selectedSizePreset != null)
            {
                var oldHasUnsavedChanges = _hasUnsavedChanges;
                _previewWidth = _selectedSizePreset.Width;
                _previewHeight = _selectedSizePreset.Height;
                OnPropertyChanged(nameof(PreviewWidth));
                OnPropertyChanged(nameof(PreviewHeight));
                _hasUnsavedChanges = oldHasUnsavedChanges;
            }
        }

        private void UpdateSelectedPreset()
        {
            var matchingPreset = _sizePresets?.FirstOrDefault(p =>
                Math.Abs(p.Width - _previewWidth) < 0.1 &&
                Math.Abs(p.Height - _previewHeight) < 0.1);

            if (_selectedSizePreset != matchingPreset)
            {
                _selectedSizePreset = matchingPreset;
                OnPropertyChanged(nameof(SelectedSizePreset));
            }
        }

        public void NewProject()
        {
            ImageResources.Clear();
            SelectedImageResource = null;
            PreviewWidth = 360;
            PreviewHeight = 360;
            ZoomLevel = 1.0;
            CurrentProjectPath = null;
            SaveOriginalState();
        }

        public void SetProjectPath(string path)
        {
            CurrentProjectPath = path;
            SaveOriginalState();
        }

        public void MarkAsModified()
        {
            HasUnsavedChanges = true;
        }

        private void OnImageResourcesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (ImageResource item in e.OldItems)
                {
                    item.PropertyChanged -= OnImageResourcePropertyChanged;
                }
                
                // Adjust Layer indices of other images when removing an image
                if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    ReorganizeLayers();
                }
            }

            if (e.NewItems != null)
            {
                foreach (ImageResource item in e.NewItems)
                {
                    item.PropertyChanged += OnImageResourcePropertyChanged;
                }
                
                // Set correct Layer value when adding an image
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (ImageResource newItem in e.NewItems)
                    {
                        newItem.Layer = ImageResources.IndexOf(newItem);
                    }
                }
            }

            CheckForUnsavedChanges();
        }

        private void ReorganizeLayers()
        {
            // Pause event listening to avoid recursive triggering
            foreach (var image in ImageResources)
            {
                image.PropertyChanged -= OnImageResourcePropertyChanged;
            }

            // Reorganize Layer indices to ensure continuity (0, 1, 2, 3...)
            for (int i = 0; i < ImageResources.Count; i++)
            {
                ImageResources[i].Layer = i;
            }

            // Resume event listening
            foreach (var image in ImageResources)
            {
                image.PropertyChanged += OnImageResourcePropertyChanged;
            }
        }

        private void OnImageResourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageResource.Layer) && sender is ImageResource changedImage)
            {
                HandleLayerChange(changedImage);
            }
            CheckForUnsavedChanges();
        }

        private void HandleLayerChange(ImageResource changedImage)
        {
            var targetLayer = changedImage.Layer;
            var maxLayer = ImageResources.Count - 1;

            // If exceeds maximum Layer range, set to maximum value
            if (targetLayer > maxLayer)
            {
                changedImage.Layer = maxLayer;
                return;
            }

            // If less than 0, set to 0
            if (targetLayer < 0)
            {
                changedImage.Layer = 0;
                return;
            }

            // Check if any other image uses the same Layer
            var conflictImage = ImageResources.FirstOrDefault(img => img != changedImage && img.Layer == targetLayer);
            
            if (conflictImage != null)
            {
                // Found conflicting image, need to swap Layer
                // Temporarily disable event listening to avoid recursion
                conflictImage.PropertyChanged -= OnImageResourcePropertyChanged;
                
                // Swap Layer values
                var oldLayer = conflictImage.Layer;
                conflictImage.Layer = GetOriginalLayer(changedImage);
                
                // Resume event listening
                conflictImage.PropertyChanged += OnImageResourcePropertyChanged;
            }
        }

        private int GetOriginalLayer(ImageResource changedImage)
        {
            // Find an unused Layer value from all images
            var usedLayers = ImageResources.Where(img => img != changedImage).Select(img => img.Layer).ToHashSet();
            
            for (int i = 0; i < ImageResources.Count; i++)
            {
                if (!usedLayers.Contains(i))
                {
                    return i;
                }
            }
            
            // If all Layers are used, return maximum value
            return ImageResources.Count - 1;
        }

        private string CalculateCurrentStateHash()
        {
            var state = new
            {
                PreviewWidth = _previewWidth,
                PreviewHeight = _previewHeight,
                ZoomLevel = _zoomLevel,
                ImageResources = _imageResources.Select(img => new
                {
                    img.Name,
                    img.FilePath,
                    img.X,
                    img.Y,
                    img.Scale,
                    img.Rotation,
                    img.Layer,
                    img.IsVisible,
                    img.Opacity
                }).OrderBy(img => img.Layer).ToList()
            };

            var jsonString = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonString));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private void CheckForUnsavedChanges()
        {
            if (_originalStateHash == null) return;

            var currentHash = CalculateCurrentStateHash();
            HasUnsavedChanges = currentHash != _originalStateHash;
        }

        public void SaveOriginalState()
        {
            _originalStateHash = CalculateCurrentStateHash();
            HasUnsavedChanges = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}