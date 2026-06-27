using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Web.Services;

public class YtDlpConfigProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; private set; } = "--restrict-filenames --no-warnings";
    public string OutputTemplate { get; private set; } = "";
    public string FormatString { get; private set; } = "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";

    public YtDlpConfigProvider(IStoragePathProvider storage)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
            ExecutablePath = "yt-dlp";
    }

    public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        var (extraArgs, outputTemplate, formatString) = await AppSettingsReader.ReadYtDlpConfigAsync(dbFactory);
        if (!string.IsNullOrWhiteSpace(extraArgs))
            ExtraArguments = extraArgs;
        if (!string.IsNullOrWhiteSpace(outputTemplate))
            OutputTemplate = outputTemplate;
        if (!string.IsNullOrWhiteSpace(formatString))
            FormatString = formatString;
    }
}
