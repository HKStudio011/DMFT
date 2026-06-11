using DMFT.Core.Services;
using DMFT.Shared.Services;

namespace DMFT.Services;

public class YtDlpConfigProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; } = string.Empty;
    public string OutputTemplate { get; } = string.Empty;
    public string FormatString { get; } = string.Empty;

    public YtDlpConfigProvider(IStoragePathProvider storage)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
            ExecutablePath = Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe");
    }
}
