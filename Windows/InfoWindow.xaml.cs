using MiniScreenPreview.ViewModels;
using System.Text;
using System.Windows;

namespace MiniScreenPreview.Windows
{
    public partial class InfoWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public InfoWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            GenerateInfoText();
        }

        private void GenerateInfoText()
        {
            var sb = new StringBuilder();

            // Project Information
            sb.AppendLine("=== PROJECT INFORMATION ===");
            sb.AppendLine($"Preview Size: {_viewModel.PreviewWidth} × {_viewModel.PreviewHeight}");
            sb.AppendLine($"Zoom Level: {_viewModel.ZoomLevel:F2}×");
            sb.AppendLine($"Total Images: {_viewModel.ImageResources.Count}");
            sb.AppendLine();

            // Images Information (only visible images)
            sb.AppendLine("=== IMAGES INFORMATION ===");
            var visibleImages = _viewModel.ImageResources
                .Where(img => img.IsVisible)
                .OrderBy(img => img.Layer)
                .ToList();

            if (visibleImages.Count == 0)
            {
                sb.AppendLine("No visible images.");
            }
            else
            {
                for (int i = 0; i < visibleImages.Count; i++)
                {
                    var img = visibleImages[i];
                    sb.AppendLine($"Image {i + 1}:");
                    sb.AppendLine($"  Name: {img.Name}");
                    sb.AppendLine($"  File: {img.FilePath}");

                    if (img.ImageSource != null)
                    {
                        sb.AppendLine($"  Original Size: {img.ImageSource.PixelWidth} × {img.ImageSource.PixelHeight} px");
                    }

                    sb.AppendLine($"  Position: X={img.X:F0}, Y={img.Y:F0}");
                    sb.AppendLine($"  Scale: {img.Scale:F2}×");
                    sb.AppendLine($"  Rotation: {img.Rotation:F0}°");
                    sb.AppendLine($"  Opacity: {img.Opacity:P0}");
                    sb.AppendLine($"  Layer: {img.Layer}");

                    if (i < visibleImages.Count - 1)
                        sb.AppendLine();
                }
            }

            InfoTextBox.Text = sb.ToString();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            var text = InfoTextBox.Text;
            try
            {
                Clipboard.SetDataObject(text);
                MessageBox.Show("Information copied to clipboard successfully!", "Copy Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            catch { }

            MessageBox.Show("Failed to copy to clipboard after multiple attempts.", "Copy Error",
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}