using MiniScreenPreview.Models;
using MiniScreenPreview.ViewModels;
using System.IO;
using System.Text.Json;

namespace MiniScreenPreview.Services
{
    public class DataService
    {
        private const string SETTINGS_FILE_NAME = "app_settings.json";
        private readonly string _settingsFilePath;

        public DataService()
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _settingsFilePath = Path.Combine(appDirectory, SETTINGS_FILE_NAME);
        }

        public async Task SaveProjectDataAsync(MainViewModel viewModel, string filePath)
        {
            try
            {
                var projectData = new ProjectData
                {
                    PreviewWidth = viewModel.PreviewWidth,
                    PreviewHeight = viewModel.PreviewHeight,
                    ZoomLevel = viewModel.ZoomLevel,
                    ImageResources = viewModel.ImageResources.Select(img => new ImageResourceData
                    {
                        Name = img.Name,
                        FilePath = img.FilePath,
                        X = img.X,
                        Y = img.Y,
                        Scale = img.Scale,
                        Rotation = img.Rotation,
                        Layer = img.Layer,
                        IsVisible = img.IsVisible,
                        Opacity = img.Opacity
                    }).ToList()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var jsonString = JsonSerializer.Serialize(projectData, options);
                await File.WriteAllTextAsync(filePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save project data: {ex.Message}");
            }
        }

        public async Task<ProjectData?> LoadProjectDataAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(filePath);
                var projectData = JsonSerializer.Deserialize<ProjectData>(jsonString);
                return projectData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load project data: {ex.Message}");
                return null;
            }
        }

        public void ApplyProjectData(ProjectData projectData, MainViewModel viewModel)
        {
            if (projectData == null) return;

            viewModel.PreviewWidth = projectData.PreviewWidth;
            viewModel.PreviewHeight = projectData.PreviewHeight;
            viewModel.ZoomLevel = projectData.ZoomLevel;

            viewModel.ImageResources.Clear();

            foreach (var imageData in projectData.ImageResources)
            {
                if (File.Exists(imageData.FilePath))
                {
                    var imageResource = new ImageResource
                    {
                        Name = imageData.Name,
                        FilePath = imageData.FilePath,
                        X = imageData.X,
                        Y = imageData.Y,
                        Scale = imageData.Scale,
                        Rotation = imageData.Rotation,
                        Layer = imageData.Layer,
                        IsVisible = imageData.IsVisible,
                        Opacity = imageData.Opacity
                    };
                    viewModel.ImageResources.Add(imageResource);
                }
            }
        }

        public async Task SaveLastOpenedFilePathAsync(string filePath)
        {
            try
            {
                var settings = new { LastOpenedFilePath = filePath };
                var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsFilePath, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save last opened file path: {ex.Message}");
            }
        }

        public async Task<string?> LoadLastOpenedFilePathAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

                if (settings != null && settings.TryGetValue("LastOpenedFilePath", out var pathObj))
                {
                    var filePath = pathObj.ToString();
                    return File.Exists(filePath) ? filePath : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load last opened file path: {ex.Message}");
                return null;
            }
        }
    }
}