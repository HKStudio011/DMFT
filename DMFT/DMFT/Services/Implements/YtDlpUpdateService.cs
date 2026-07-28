using System.Diagnostics;

namespace DMFT.Services;

public interface IYtDlpUpdateService
{
    Task<string?> GetCurrentVersionAsync();
    Task<string?> UpdateAsync();
}

public class YtDlpUpdateService : IYtDlpUpdateService
{
    private readonly IYtDlpConfigProvider _config;

    public YtDlpUpdateService(IYtDlpConfigProvider config)
    {
        _config = config;
    }

    public async Task<string?> GetCurrentVersionAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _config.ExecutablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return output?.Trim();
        }
        catch { return null; }
    }

    public async Task<string?> UpdateAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _config.ExecutablePath,
                Arguments = "-U",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return await GetCurrentVersionAsync();
        }
        catch { return null; }
    }
}
