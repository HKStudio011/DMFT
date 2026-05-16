using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DMFT.Utilities
{
    public static class FolderOpener
    {
        public static string GetParentFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            var parent = Directory.GetParent(path)?.FullName;
            return string.IsNullOrWhiteSpace(parent) ? path : parent;
        }

        public static void OpenParentFolder(string saveLocation)
        {
            if (string.IsNullOrWhiteSpace(saveLocation))
                throw new ArgumentException("Location is null or empty");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Explorer
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{saveLocation}\"",
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: Open Finder
                Process.Start("open", saveLocation);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Use xdg-open if available
                Process.Start("xdg-open", saveLocation);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS for folder opening");
            }
        }
    }
}
