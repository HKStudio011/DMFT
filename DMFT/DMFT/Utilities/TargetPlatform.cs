using System;
using System.Collections.Generic;
using System.Text;

namespace DMFT.Utilities
{
    public static class TargetPlatform
    {
        [Flags]
        public enum Platform
        {
            Web = 1,
            Maui = 2,
            Windows = 4,
            MacOS = 8,
            Linux = 16,
            Android = 32,
            iOS = 64,
            WebAssembly = 128,
        }

        public static Platform CurrentPlatform { get; private set; }

        public static void SetCurrentPlatform(Platform platform)
        {
            CurrentPlatform = platform;
        }

        public static string GetFormFactor()
        {
            var platform = CurrentPlatform;

            if (platform.HasFlag(Platform.WebAssembly))
                return "WebAssembly";
            if (platform.HasFlag(Platform.Web))
                return "Web";
            if (platform.HasFlag(Platform.Windows))
                return "Windows";
            if (platform.HasFlag(Platform.Maui))
                return "MAUI";

            return platform.ToString();
        }

    }
}
