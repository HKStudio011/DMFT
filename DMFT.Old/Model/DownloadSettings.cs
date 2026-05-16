using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace DMFT.Model
{
    public static class DownloadSettings
    {
        private static readonly string SettingsFileName = Path.Combine(FileSystem.AppDataDirectory, "download_settings.json");
        public static string DefaultPath { get; private set; } = string.Empty;

        public static async Task LoadAsync()
        {
            try
            {
                if (File.Exists(SettingsFileName))
                {
                    DefaultPath = await File.ReadAllTextAsync(SettingsFileName);
                }
            }
            catch { /* ignore for MVP */ }
        }

        public static async Task SaveAsync()
        {
            try
            {
                await File.WriteAllTextAsync(SettingsFileName, DefaultPath ?? "");
            }
            catch { /* ignore for MVP */ }
        }

        public static void SetPath(string path)
        {
            DefaultPath = path ?? string.Empty;
            _ = SaveAsync();
        }
    }
}
