using DMFT.Model;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace DMFT.Services
{
    public class DownloadProgress
    {
        public string Status { get; set; } = string.Empty;
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Speed { get; set; }
        public int EtaSeconds { get; set; }
    }

    public interface IMediaDownloader
    {
        Task DownloadAsync(string videoUrl, string outputPath, bool noWatermark);
        Task DownloadAudioAsync(string videoUrl, string outputPath);
        Task CancelAsync();
        Action<DownloadProgress>? OnProgress { get; set; }
    }

    public class MediaDownloader : IMediaDownloader
    {
        private readonly YtDlpConfig _config;
        private System.Diagnostics.Process? _currentProcess;

        public Action<DownloadProgress>? OnProgress { get; set; }

        public MediaDownloader(YtDlpConfig config)
        {
            _config = config ?? new YtDlpConfig();
        }

        public Task DownloadAsync(string videoUrl, string outputPath, bool noWatermark)
        {
            string args = $"--newline --progress-template \"%(progress)j\" -o \"{outputPath}\" -f \"bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best\" --merge-output-format mp4 \"{videoUrl}\"";
            if (noWatermark)
            {
                args = $"--newline --progress-template \"%(progress)j\" -o \"{outputPath}\" -f \"bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best\" --merge-output-format mp4 \"{videoUrl}\"";
            }
            return RunYtDlpAsync(args);
        }

        public Task DownloadAudioAsync(string videoUrl, string outputPath)
        {
            string args = $"--newline --progress-template \"%(progress)j\" -o \"{outputPath}\" -x --audio-format mp3 --audio-quality 0 \"{videoUrl}\"";
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

            proc.OutputDataReceived += (_, e) => HandleProgressLine(e.Data, "stdout");
            proc.ErrorDataReceived += (_, e) => HandleProgressLine(e.Data, "stderr");

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync();

            _currentProcess = null;

            if (proc.ExitCode != 0)
            {
                var err = proc.StandardError.ReadToEnd();
                throw new Exception($"yt-dlp failed with exit code {proc.ExitCode}: {err}");
            }
        }

        private void HandleProgressLine(string? line, string source)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var progress = new DownloadProgress
                {
                    Status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                    DownloadedBytes = root.TryGetProperty("downloaded_bytes", out var db) && db.ValueKind == JsonValueKind.Number ? db.GetInt64() : 0,
                    TotalBytes = root.TryGetProperty("total_bytes", out var tb) && tb.ValueKind == JsonValueKind.Number ? tb.GetInt64() : 0,
                    Speed = root.TryGetProperty("speed", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetDouble() : 0,
                    EtaSeconds = root.TryGetProperty("eta", out var et) && et.ValueKind == JsonValueKind.Number ? et.GetInt32() : -1
                };

                OnProgress?.Invoke(progress);
            }
            catch
            {
                Console.WriteLine($"[yt-dlp:{source}] {line}");
            }
        }
    }
}