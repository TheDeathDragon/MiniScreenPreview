namespace MiniScreenPreview.Models
{
    public class SizePreset
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public SizePreset(string name, double width, double height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}