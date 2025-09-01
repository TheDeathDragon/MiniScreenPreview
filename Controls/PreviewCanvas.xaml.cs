using MiniScreenPreview.Models;
using MiniScreenPreview.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniScreenPreview.Controls
{
    public partial class PreviewCanvas : UserControl
    {
        private MainViewModel? _viewModel;
        private bool _isDragging;
        private Point _lastPosition;
        private Image? _draggedImage;
        private readonly Dictionary<ImageResource, PropertyChangedEventHandler> _propertyChangedHandlers = new();

        public PreviewCanvas()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            MouseWheel += OnPreviewMouseWheel;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            if (_viewModel != null)
            {
                _viewModel.ImageResources.CollectionChanged += OnImageResourcesChanged;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                
                // Subscribe to all existing images' property changes
                foreach (var imageResource in _viewModel.ImageResources)
                {
                    imageResource.PropertyChanged += OnAnyImageResourcePropertyChanged;
                }
                
                UpdateCanvas();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedImageResource) ||
                e.PropertyName == nameof(MainViewModel.ShowSelectionBorder) ||
                e.PropertyName == nameof(MainViewModel.SelectionBorderColor))
            {
                UpdateSelectionBorder();
            }
        }

        private void OnImageResourcesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle new items
            if (e.NewItems != null)
            {
                foreach (ImageResource item in e.NewItems)
                {
                    item.PropertyChanged += OnAnyImageResourcePropertyChanged;
                }
            }

            // Handle removed items
            if (e.OldItems != null)
            {
                foreach (ImageResource item in e.OldItems)
                {
                    item.PropertyChanged -= OnAnyImageResourcePropertyChanged;
                }
            }

            UpdateCanvas();
        }

        private void OnAnyImageResourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Handle visibility changes for all images (visible and invisible)
            if (e.PropertyName == nameof(ImageResource.IsVisible))
            {
                UpdateCanvas();
            }
        }

        private void UpdateCanvas()
        {
            if (_viewModel == null) return;

            // Clean up existing event handlers to prevent memory leaks
            CleanupExistingHandlers();
            MainCanvas.Children.Clear();

            var sortedImages = _viewModel.ImageResources
                .Where(img => img.IsVisible && img.ImageSource != null)
                .OrderBy(img => img.Layer)
                .ToList();

            foreach (var imageResource in sortedImages)
            {
                var image = new Image
                {
                    Source = imageResource.ImageSource,
                    Width = imageResource.ImageSource?.PixelWidth * imageResource.Scale ?? 100,
                    Height = imageResource.ImageSource?.PixelHeight * imageResource.Scale ?? 100,
                    Opacity = imageResource.Opacity,
                    Tag = imageResource,
                    Cursor = imageResource.IsLocked ? Cursors.No : Cursors.Hand,
                    IsHitTestVisible = !imageResource.IsLocked,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new RotateTransform(imageResource.Rotation));
                image.RenderTransform = transformGroup;

                // Create container for selection overlay
                var grid = new Grid
                {
                    Tag = imageResource
                };
                
                // Add the image
                grid.Children.Add(image);
                
                // Add border overlay that doesn't affect layout
                var selectionBorder = new Border
                {
                    BorderThickness = new Thickness(0),
                    IsHitTestVisible = false, // 不参与鼠标事件
                    Name = "SelectionBorder"
                };
                grid.Children.Add(selectionBorder);

                Canvas.SetLeft(grid, imageResource.X);
                Canvas.SetTop(grid, imageResource.Y);
                Panel.SetZIndex(grid, imageResource.Layer);

                image.MouseLeftButtonDown += OnImageMouseLeftButtonDown;
                image.MouseMove += OnImageMouseMove;
                image.MouseLeftButtonUp += OnImageMouseLeftButtonUp;

                MainCanvas.Children.Add(grid);

                // Store and subscribe to property change handler
                var handler = CreatePropertyChangedHandler(grid, image);
                _propertyChangedHandlers[imageResource] = handler;
                imageResource.PropertyChanged += handler;
            }

            UpdateSelectionBorder();
        }

        private void CleanupExistingHandlers()
        {
            // Clean up UI event handlers
            foreach (UIElement element in MainCanvas.Children)
            {
                if (element is Grid grid)
                {
                    foreach (UIElement child in grid.Children)
                    {
                        if (child is Image image)
                        {
                            // Remove event handlers
                            image.MouseLeftButtonDown -= OnImageMouseLeftButtonDown;
                            image.MouseMove -= OnImageMouseMove;
                            image.MouseLeftButtonUp -= OnImageMouseLeftButtonUp;
                        }
                    }
                }
            }

            // Clean up PropertyChanged event handlers
            foreach (var kvp in _propertyChangedHandlers)
            {
                kvp.Key.PropertyChanged -= kvp.Value;
            }
            _propertyChangedHandlers.Clear();
        }

        private PropertyChangedEventHandler CreatePropertyChangedHandler(Grid grid, Image image)
        {
            return (s, args) =>
            {
                if (s is not ImageResource imageResource) return;

                if (args.PropertyName == nameof(ImageResource.X))
                {
                    Canvas.SetLeft(grid, imageResource.X);
                }
                else if (args.PropertyName == nameof(ImageResource.Y))
                {
                    Canvas.SetTop(grid, imageResource.Y);
                }
                else if (args.PropertyName == nameof(ImageResource.Scale))
                {
                    if (imageResource.ImageSource != null)
                    {
                        image.Width = imageResource.ImageSource.PixelWidth * imageResource.Scale;
                        image.Height = imageResource.ImageSource.PixelHeight * imageResource.Scale;
                    }
                }
                else if (args.PropertyName == nameof(ImageResource.Opacity))
                {
                    image.Opacity = imageResource.Opacity;
                }
                else if (args.PropertyName == nameof(ImageResource.Rotation))
                {
                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new RotateTransform(imageResource.Rotation));
                    image.RenderTransform = transformGroup;
                }
                else if (args.PropertyName == nameof(ImageResource.Layer))
                {
                    Panel.SetZIndex(grid, imageResource.Layer);
                }
                else if (args.PropertyName == nameof(ImageResource.IsLocked))
                {
                    image.Cursor = imageResource.IsLocked ? Cursors.No : Cursors.Hand;
                    image.IsHitTestVisible = !imageResource.IsLocked;
                }
            };
        }

        private void UpdateSelectionBorder()
        {
            if (_viewModel == null) return;

            foreach (UIElement element in MainCanvas.Children)
            {
                if (element is Grid grid && grid.Tag is ImageResource imageResource)
                {
                    // Find the selection border within the grid
                    var selectionBorder = grid.Children.OfType<Border>()
                        .FirstOrDefault(b => b.Name == "SelectionBorder");
                    
                    if (selectionBorder != null)
                    {
                        if (_viewModel.ShowSelectionBorder && imageResource == _viewModel.SelectedImageResource)
                        {
                            selectionBorder.BorderThickness = new Thickness(2);
                            selectionBorder.BorderBrush = GetBrushFromColorName(_viewModel.SelectionBorderColor);
                        }
                        else
                        {
                            selectionBorder.BorderThickness = new Thickness(0);
                        }
                    }
                }
            }
        }

        private Brush GetBrushFromColorName(string colorName)
        {
            return colorName switch
            {
                "Red" => Brushes.Red,
                "Green" => Brushes.Green,
                "Blue" => Brushes.Blue,
                _ => Brushes.Red
            };
        }

        private void OnImageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Tag is ImageResource imageResource)
            {
                _viewModel!.SelectedImageResource = imageResource;
                _isDragging = true;
                _lastPosition = e.GetPosition(MainCanvas);
                _draggedImage = image;
                image.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedImage?.Tag is ImageResource imageResource)
            {
                var currentPosition = e.GetPosition(MainCanvas);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                var newX = imageResource.X + deltaX;
                var newY = imageResource.Y + deltaY;

                imageResource.X = newX;
                imageResource.Y = newY;

                _lastPosition = currentPosition;
                e.Handled = true;
            }
        }

        private void OnImageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedImage != null)
            {
                _isDragging = false;
                _draggedImage.ReleaseMouseCapture();
                _draggedImage = null;
                e.Handled = true;
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_viewModel == null) return;

            // 如果按住Ctrl键，则调整缩放级别
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                var zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
                _viewModel.ZoomLevel = Math.Max(0.1, Math.Min(5.0, _viewModel.ZoomLevel + zoomDelta));
                e.Handled = true;
            }
            else
            {
                // 否则让ScrollViewer处理滚动
                // 不设置e.Handled = true，让事件继续冒泡到ScrollViewer
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_viewModel?.SelectedImageResource != null)
            {
                const double moveStep = 1.0;

                switch (e.Key)
                {
                    case Key.Left:
                        _viewModel.SelectedImageResource.X -= moveStep;
                        break;
                    case Key.Right:
                        _viewModel.SelectedImageResource.X += moveStep;
                        break;
                    case Key.Up:
                        _viewModel.SelectedImageResource.Y -= moveStep;
                        break;
                    case Key.Down:
                        _viewModel.SelectedImageResource.Y += moveStep;
                        break;
                    case Key.Delete:
                        if (_viewModel.RemoveImageCommand.CanExecute(null))
                        {
                            _viewModel.RemoveImageCommand.Execute(null);
                        }
                        break;
                }
            }
        }
    }
}