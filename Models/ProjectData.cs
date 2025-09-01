namespace MiniScreenPreview.Models
{
    public class ProjectData
    {
        public double PreviewWidth { get; set; }
        public double PreviewHeight { get; set; }
        public double ZoomLevel { get; set; }
        public List<ImageResourceData> ImageResources { get; set; }

        public ProjectData()
        {
            ImageResources = new List<ImageResourceData>();
            PreviewWidth = 360;
            PreviewHeight = 360;
            ZoomLevel = 1.0;
        }
    }

    public class ImageResourceData
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Scale { get; set; } = 1.0;
        public double Rotation { get; set; }
        public int Layer { get; set; }
        public bool IsVisible { get; set; } = true;
        public double Opacity { get; set; } = 1.0;
        public bool IsLocked { get; set; } = false;
    }
}