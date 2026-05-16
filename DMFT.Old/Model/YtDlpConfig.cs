using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Maui.Storage;

namespace DMFT.Model
{
    public class YtDlpConfig
    {
        public string ExecutablePath => DetermineExecutablePath();

        private static string DetermineExecutablePath()
        {
            string exeName = GetPlatformExecutableName();
            try
            {
                // Try app data folder first (packaged binaries can be placed here)
                string appDataFolder = Path.Combine(FileSystem.AppDataDirectory, "yt-dlp");
                string candidateAppData = Path.Combine(appDataFolder, exeName);
                if (File.Exists(candidateAppData))
                {
                    return candidateAppData;
                }

                // Then try beside the app binaries (content dir at runtime)
                string baseDir = AppContext.BaseDirectory;
                string candidateBase = Path.Combine(baseDir, "yt-dlp", exeName);
                if (File.Exists(candidateBase))
                {
                    return candidateBase;
                }

                // Fallback to PATH lookup
                return exeName;
            }
            catch
            {
                return exeName;
            }
        }

        private static string GetPlatformExecutableName()
        {
#if WINDOWS
            return "yt-dlp.exe";
#elif MACCATALYST
            return "yt-dlp_macos";
#elif IOS
            return "yt-dlp_macos";
#else
            // Linux / other platforms
            return "yt-dlp";
#endif
        }
    }
}
