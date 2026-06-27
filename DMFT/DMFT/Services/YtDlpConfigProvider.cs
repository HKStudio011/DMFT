using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Services;

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
            ExecutablePath = Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe");
    }

    public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var extraArgs = (await db.AppSettings.FindAsync("ytdlp_extra_args"))?.Value;
            if (!string.IsNullOrWhiteSpace(extraArgs))
                ExtraArguments = extraArgs;

            var outputTemplate = (await db.AppSettings.FindAsync("ytdlp_output_template"))?.Value;
            if (!string.IsNullOrWhiteSpace(outputTemplate))
                OutputTemplate = outputTemplate;

            var formatString = (await db.AppSettings.FindAsync("ytdlp_format"))?.Value;
            if (!string.IsNullOrWhiteSpace(formatString))
                FormatString = formatString;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YtDlpConfigProvider] Failed to load config from DB: {ex.Message}");
        }
    }
}
