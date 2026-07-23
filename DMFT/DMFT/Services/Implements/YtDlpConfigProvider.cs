namespace DMFT.Services;

public class YtDlpConfigProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; }
    public string OutputTemplate { get; }
    public string FormatString { get; }

    public YtDlpConfigProvider(IStoragePathProvider storage, IAppSettingsService settings)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
        {
            var baseDirPath = Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe");
            ExecutablePath = File.Exists(baseDirPath) ? baseDirPath : "yt-dlp";
        }

        ExtraArguments = settings.Get("ytdlp_extra_args") ?? "--restrict-filenames --no-warnings";
        OutputTemplate = settings.Get("ytdlp_output_template") ?? "";
        FormatString = settings.Get("ytdlp_format") ?? "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";
    }
}
