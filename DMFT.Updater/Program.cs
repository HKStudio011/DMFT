using System.Diagnostics;
using System.IO.Compression;

if (args is not ["--zip", _, "--pid", _, "--app-dir", _])
{
    Console.Error.WriteLine("Usage: DMFT.Updater --zip <zip-path> --pid <parent-pid> --app-dir <app-directory>");
    goto error;
}

var zipPath = args[1];
var pid = int.Parse(args[3]);
var appDir = args[5];

if (!File.Exists(zipPath))
{
    Console.Error.WriteLine($"Zip not found: {zipPath}");
    goto error;
}

try
{
    var parent = Process.GetProcessById(pid);
    if (!parent.WaitForExit(30_000))
    {
        Console.Error.WriteLine("Timed out waiting for parent process to exit");
        goto error;
    }
}
catch (ArgumentException) { }

try
{
    var exePath = Path.Combine(appDir, "DMFT.exe");
    var backupPath = exePath + ".bak";
    try { File.Copy(exePath, backupPath, overwrite: true); } catch { }

    var selfPath = Path.Combine(appDir, "DMFT.Updater.exe");
    var selfBackup = selfPath + ".old";
    try { File.Move(selfPath, selfBackup, overwrite: true); } catch { }

    ZipFile.ExtractToDirectory(zipPath, appDir, overwriteFiles: true);

    Process.Start(new ProcessStartInfo
    {
        FileName = exePath,
        UseShellExecute = true,
        WorkingDirectory = appDir
    });

    try { File.Delete(backupPath); } catch { }
    try { File.Delete(selfBackup); } catch { }
    try { File.Delete(zipPath); } catch { }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Update failed: {ex.Message}");
    goto error;
}

error:
    Console.Error.WriteLine("Update failed.");
    Console.WriteLine("Press Enter to exit...");
    Console.ReadLine();
    return 1;
