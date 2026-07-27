using System.Diagnostics;
using System.IO.Compression;

if (args is not ["--zip", _, "--pid", _, "--app-dir", _])
{
    Console.Error.WriteLine("Usage: DMFT.Updater --zip <zip-path> --pid <parent-pid> --app-dir <app-directory>");
    return 1;
}

var zipPath = args[1];
var pid = int.Parse(args[3]);
var appDir = args[5];

if (!File.Exists(zipPath))
{
    Console.Error.WriteLine($"Zip not found: {zipPath}");
    return 1;
}

try
{
    var parent = Process.GetProcessById(pid);
    if (!parent.WaitForExit(30_000))
    {
        Console.Error.WriteLine("Timed out waiting for parent process to exit");
        return 1;
    }
}
catch (ArgumentException) { }

try
{
    var exePath = Path.Combine(appDir, "DMFT.exe");
    var backupPath = exePath + ".bak";
    try { File.Copy(exePath, backupPath, overwrite: true); } catch { }

    ZipFile.ExtractToDirectory(zipPath, appDir, overwriteFiles: true);

    Process.Start(new ProcessStartInfo
    {
        FileName = exePath,
        UseShellExecute = true,
        WorkingDirectory = appDir
    });

    try { File.Delete(backupPath); } catch { }
    try { File.Delete(zipPath); } catch { }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Update failed: {ex.Message}");
    return 1;
}
