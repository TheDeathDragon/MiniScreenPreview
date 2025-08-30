using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace MiniScreenPreview.Models
{
    public class ImageResource : INotifyPropertyChanged
    {
        private string _name;
        private string _filePath;
        private double _x;
        private double _y;
        private double _scale;
        private int _layer;
        private bool _isVisible;
        private double _opacity;
        private double _rotation;
        private BitmapImage? _imageSource;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                LoadImage();
                OnPropertyChanged();
            }
        }

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public double Scale
        {
            get => _scale;
            set
            {
                _scale = Math.Max(0.1, Math.Min(5.0, value));
                OnPropertyChanged();
            }
        }

        public int Layer
        {
            get => _layer;
            set
            {
                _layer = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }

        public double Opacity
        {
            get => _opacity;
            set
            {
                _opacity = Math.Max(0.0, Math.Min(1.0, value));
                OnPropertyChanged();
            }
        }

        public double Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value % 360;
                if (_rotation < 0) _rotation += 360;
                OnPropertyChanged();
            }
        }

        public BitmapImage? ImageSource
        {
            get => _imageSource;
            private set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public ImageResource()
        {
            _name = string.Empty;
            _filePath = string.Empty;
            _x = 0;
            _y = 0;
            _scale = 1.0;
            _layer = 0;
            _isVisible = true;
            _opacity = 1.0;
            _rotation = 0.0;
        }

        private void LoadImage()
        {
            try
            {
                if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ImageSource = bitmap;
                }
                else
                {
                    ImageSource = null;
                }
            }
            catch
            {
                ImageSource = null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}