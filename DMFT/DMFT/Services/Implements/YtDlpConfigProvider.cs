namespace DMFT.Services;

public class YtDlpConfigProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments =>_settings.Get("ytdlp_extra_args") ?? "--restrict-filenames --no-warnings";
    public string OutputTemplate => _settings.Get("ytdlp_output_template") ?? "";
    public string FormatString => _settings.Get("ytdlp_format") ?? "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";

    private readonly IAppSettingsService _settings;

    public YtDlpConfigProvider(IStoragePathProvider storage, IAppSettingsService settings)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
        {
            var baseDirPath = Path.Combine(storage.GetAppLocalPath(), "yt-dlp", "yt-dlp.exe");
            ExecutablePath = File.Exists(baseDirPath) ? baseDirPath : "yt-dlp";
        }

        _settings = settings;
    }
}
