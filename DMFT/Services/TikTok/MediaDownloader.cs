using DMFT.Model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DMFT.Services
{
    public interface IMediaDownloader
    {
        Task DownloadAsync(string videoUrl, string outputPath, bool noWatermark);
        Task DownloadAudioAsync(string videoUrl, string outputPath);
        Task CancelAsync();
    }

    public class MediaDownloader : IMediaDownloader
    {
        private readonly YtDlpConfig _config;
        private System.Diagnostics.Process? _currentProcess;

        public MediaDownloader(YtDlpConfig config)
        {
            _config = config ?? new YtDlpConfig();
        }

        public Task DownloadAsync(string videoUrl, string outputPath, bool noWatermark)
        {
            // Build yt-dlp command to download best MP4 + audio merged
            string args = $"-o \"{outputPath}\" -f \"bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best\" --merge-output-format mp4 \"{videoUrl}\"";
            if (noWatermark)
            {
                // Placeholder for non-watermarked download; yt-dlp may not guarantee this
                args = $"-o \"{outputPath}\" -f \"bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best\" --merge-output-format mp4 \"{videoUrl}\"";
            }
            return RunYtDlpAsync(args);
        }

        public Task DownloadAudioAsync(string videoUrl, string outputPath)
        {
            string args = $"-o \"{outputPath}\" -x --audio-format mp3 --audio-quality 0 \"{videoUrl}\"";
            return RunYtDlpAsync(args);
        }

        public Task CancelAsync()
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                try { _currentProcess.Kill(true); } catch { /* ignore */ }
            }
            _currentProcess = null;
            return Task.CompletedTask;
        }

        private async Task RunYtDlpAsync(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _config.ExecutablePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            _currentProcess = proc;
            if (proc == null) throw new Exception("yt-dlp process failed to start");

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask);
            await proc.WaitForExitAsync();

            _currentProcess = null;

            if (proc.ExitCode != 0)
            {
                var err = await stderrTask;
                throw new Exception($"yt-dlp failed with exit code {proc.ExitCode}: {err}");
            }
        }
    }
}
