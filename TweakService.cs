using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace FortniteTweakTool.Services;

public static class TweakService
{
    public static string BackupDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteTweakTool", "Backups");
    public static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteTweakTool", "app.log");

    public static void Log(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }

    public static async Task<(bool ok, string message)> RunElevatedPowerShellAsync(string script)
    {
        try
        {
            Directory.CreateDirectory(BackupDirectory);
            // Use an encoded command to safely preserve PowerShell quoting.
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}")
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var p = Process.Start(psi);
            if (p == null) return (false, "PowerShell konnte nicht gestartet werden.");
            await p.WaitForExitAsync();
            Log($"PowerShell exit={p.ExitCode}; script={script.Replace(Environment.NewLine, " ")}");
            return p.ExitCode == 0 ? (true, "Änderung erfolgreich ausgeführt.") : (false, $"PowerShell meldete Exit-Code {p.ExitCode}.");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Log("UAC-Abfrage vom Benutzer abgebrochen.");
            return (false, "Abgebrochen: Administratorfreigabe wurde nicht erteilt.");
        }
        catch (Exception ex)
        {
            Log("Fehler: " + ex);
            return (false, ex.Message);
        }
    }

    public static string RestorePointScript()
    {
        return """
        $ErrorActionPreference = 'Stop'
        try {
          Enable-ComputerRestore -Drive "$env:SystemDrive\" -ErrorAction SilentlyContinue
          Checkpoint-Computer -Description "Fortnite Tweak Tool - vor Optimierung" -RestorePointType "MODIFY_SETTINGS"
          exit 0
        } catch {
          Write-Error $_
          exit 2
        }
        """;
    }

    public static string ApplyGameModeScript()
    {
        // Back up the prior registry value to a timestamped .reg file before changing it.
        var backup = Path.Combine(BackupDirectory, "GameMode-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".reg");
        var escaped = backup.Replace("'", "''");
        const string template = """
        $ErrorActionPreference = 'Stop'
        $key = 'HKCU\Software\Microsoft\GameBar'
        $backup = '__BACKUP__'
        reg.exe export $key $backup /y 2>$null | Out-Null
        if (-not (Test-Path 'HKCU:\Software\Microsoft\GameBar')) { New-Item 'HKCU:\Software\Microsoft\GameBar' -Force | Out-Null }
        New-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'AutoGameModeEnabled' -PropertyType DWord -Value 1 -Force | Out-Null
        New-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'AllowAutoGameMode' -PropertyType DWord -Value 1 -Force | Out-Null
        exit 0
        """;
        return template.Replace("__BACKUP__", escaped);
    }

    public static string RestoreGameModeScript()
    {
        return """
        $ErrorActionPreference = 'Stop'
        $files = Get-ChildItem "$env:LOCALAPPDATA\FortniteTweakTool\Backups\GameMode-*.reg" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
        if ($files) { reg.exe import $files[0].FullName; exit $LASTEXITCODE }
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'AutoGameModeEnabled' -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\GameBar' -Name 'AllowAutoGameMode' -ErrorAction SilentlyContinue
        exit 0
        """;
    }

    public static string HighPerformancePlanScript() => """
        $ErrorActionPreference = 'Stop'
        # This changes the active power plan only; it does not alter voltages or clocks.
        powercfg /setactive SCHEME_MIN
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        exit 0
        """;
}
