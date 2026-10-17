using System.Diagnostics;
using System.IO;
using System.Text;

namespace FortniteTweakTool.Services;

public static class TweakService
{
    private sealed record RegValue(string KeyPath, string Name, string Type, string Value);

    // Alle Registry-Tweaks gelten nur für das aktuelle Benutzerkonto (HKCU) und werden mit Backup angewendet.
    private static readonly Dictionary<string, RegValue[]> RegistryTweaks = new()
    {
        ["gamemode"] = new[]
        {
            new RegValue(@"HKCU:\Software\Microsoft\GameBar", "AutoGameModeEnabled", "DWord", "1"),
            new RegValue(@"HKCU:\Software\Microsoft\GameBar", "AllowAutoGameMode", "DWord", "1")
        },
        ["gamedvr"] = new[]
        {
            new RegValue(@"HKCU:\System\GameConfigStore", "GameDVR_Enabled", "DWord", "0"),
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", "DWord", "0")
        },
        ["transparency"] = new[]
        {
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", "DWord", "0")
        },
        ["bgapps"] = new[]
        {
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", "DWord", "1")
        },
        ["mouse"] = new[]
        {
            new RegValue(@"HKCU:\Control Panel\Mouse", "MouseSpeed", "String", "0"),
            new RegValue(@"HKCU:\Control Panel\Mouse", "MouseThreshold1", "String", "0"),
            new RegValue(@"HKCU:\Control Panel\Mouse", "MouseThreshold2", "String", "0")
        },
        ["animations"] = new[]
        {
            new RegValue(@"HKCU:\Control Panel\Desktop\WindowMetrics", "MinAnimate", "String", "0"),
            new RegValue(@"HKCU:\Control Panel\Desktop", "MenuShowDelay", "String", "0"),
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", "DWord", "0")
        },
        ["notifications"] = new[]
        {
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", "DWord", "0")
        },
        ["gamebarpanel"] = new[]
        {
            new RegValue(@"HKCU:\Software\Microsoft\GameBar", "ShowStartupPanel", "DWord", "0"),
            new RegValue(@"HKCU:\Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", "DWord", "0")
        },
        ["tips"] = new[]
        {
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", "DWord", "0"),
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", "DWord", "0"),
            new RegValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", "DWord", "0")
        },
        ["fse"] = new[]
        {
            new RegValue(@"HKCU:\System\GameConfigStore", "GameDVR_FSEBehaviorMode", "DWord", "2"),
            new RegValue(@"HKCU:\System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", "DWord", "1"),
            new RegValue(@"HKCU:\System\GameConfigStore", "GameDVR_FSEBehavior", "DWord", "2"),
            new RegValue(@"HKCU:\System\GameConfigStore", "GameDVR_DXGIHonorFSEWindowsCompatible", "DWord", "1")
        },
        ["mousespeed"] = new[]
        {
            new RegValue(@"HKCU:\Control Panel\Mouse", "MouseSensitivity", "String", "10")
        },
        ["keyboard"] = new[]
        {
            new RegValue(@"HKCU:\Control Panel\Keyboard", "KeyboardDelay", "String", "0"),
            new RegValue(@"HKCU:\Control Panel\Keyboard", "KeyboardSpeed", "String", "31")
        },
        ["stickykeys"] = new[]
        {
            new RegValue(@"HKCU:\Control Panel\Accessibility\StickyKeys", "Flags", "String", "506"),
            new RegValue(@"HKCU:\Control Panel\Accessibility\Keyboard Response", "Flags", "String", "122"),
            new RegValue(@"HKCU:\Control Panel\Accessibility\ToggleKeys", "Flags", "String", "58")
        }
    };

    private static readonly Dictionary<string, string> SuccessNotes = new()
    {
        ["mouse"] = "Wirkt nach der nächsten Windows-Anmeldung.",
        ["stickykeys"] = "Wirkt ggf. erst nach der nächsten Anmeldung.",
        ["gamedvr"] = "Wirkt teilweise erst nach einem Neustart.",
        ["bgapps"] = "Wirkt teilweise erst nach einem Neustart.",
        ["animations"] = "Wirkt nach Neuanmeldung oder Explorer-Neustart.",
        ["gamebarpanel"] = "Wirkt teilweise erst nach einem Neustart.",
        ["fse"] = "Wirkt beim nächsten Spielstart.",
        ["mousespeed"] = "Wirkt nach der nächsten Windows-Anmeldung.",
        ["keyboard"] = "Wirkt nach der nächsten Windows-Anmeldung.",
        ["tempclean"] = "Temporäre Dateien gelöscht (belegte Dateien wurden übersprungen).",
        ["shadercache"] = "Cache geleert – beim nächsten Spielstart baut sich der Cache neu auf."
    };

    public static string BackupDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteTweakTool", "Backups");
    public static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteTweakTool", "app.log");

    private static string BackupFile(string key) => Path.Combine(BackupDirectory, key + ".bak");
    public static bool HasBackup(string key) => File.Exists(BackupFile(key));

    public static void Log(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private static (bool ok, string message) Result(int exitCode) => exitCode switch
    {
        0 => (true, "Änderung erfolgreich ausgeführt."),
        3 => (false, "Kein Backup vorhanden – nichts zurückzusetzen."),
        _ => (false, $"PowerShell meldete Exit-Code {exitCode}. Details stehen im Log.")
    };

    // Mit Administratorrechten (UAC-Abfrage). Nur für Dinge nötig, die nicht im Benutzerkonto liegen.
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
            return Result(p.ExitCode);
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

    // Ohne UAC im aktuellen Benutzerkonto (für HKCU-Einstellungen).
    public static async Task<(bool ok, string message)> RunUserPowerShellAsync(string script)
    {
        try
        {
            Directory.CreateDirectory(BackupDirectory);
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) return (false, "PowerShell konnte nicht gestartet werden.");
            var stdoutTask = p.StandardOutput.ReadToEndAsync();
            var stderrTask = p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            var stderr = (await stderrTask).Trim();
            await stdoutTask;
            Log($"PowerShell(user) exit={p.ExitCode}; stderr={stderr.Replace(Environment.NewLine, " ")}; script={script.Replace(Environment.NewLine, " ")}");
            return Result(p.ExitCode);
        }
        catch (Exception ex)
        {
            Log("Fehler: " + ex);
            return (false, ex.Message);
        }
    }

    public static async Task<(bool ok, string message)> ApplyAsync(string key)
    {
        (bool ok, string message) result;
        if (key == "power") result = await RunElevatedPowerShellAsync(ApplyPowerScript());
        else if (key == "dnsflush") result = await RunElevatedPowerShellAsync(DnsFlushScript());
        else if (key == "tempclean") result = await RunUserPowerShellAsync(CleanFolderScript("$env:TEMP"));
        else if (key == "shadercache") result = await RunUserPowerShellAsync(CleanFolderScript("(Join-Path $env:LOCALAPPDATA 'D3DSCache')"));
        else if (RegistryTweaks.ContainsKey(key)) result = await RunUserPowerShellAsync(ApplyRegistryScript(key));
        else return (false, "Unbekannter Tweak.");

        if (!result.ok) return result;
        return (true, SuccessNotes.TryGetValue(key, out var note) ? note : "");
    }

    public static Task<(bool ok, string message)> UndoAsync(string key)
    {
        if (key == "power") return RunElevatedPowerShellAsync(UndoPowerScript());
        if (RegistryTweaks.ContainsKey(key)) return RunUserPowerShellAsync(UndoRegistryScript(key));
        return Task.FromResult<(bool ok, string message)>((false, "Dieser Tweak kann nicht rückgängig gemacht werden."));
    }

    private static string BuildItems(RegValue[] values)
    {
        var parts = values.Select(v =>
        {
            var literal = v.Type == "DWord" ? v.Value : "'" + v.Value.Replace("'", "''") + "'";
            return $"@{{ Path='{v.KeyPath}'; Name='{v.Name}'; Type='{v.Type}'; Value={literal} }}";
        });
        return "@(" + string.Join(",\n", parts) + ")";
    }

    private static string EscapeSingleQuotes(string s) => s.Replace("'", "''");

    private static string ApplyRegistryScript(string key)
    {
        const string template = """
        $ErrorActionPreference = 'Stop'
        $items = __ITEMS__
        $backupFile = '__BACKUP__'
        # Den ursprünglichen Zustand nur einmal sichern, damit "Rückgängig" immer auf den Originalwert zurückführt.
        if (-not (Test-Path -LiteralPath $backupFile)) {
          $saved = @()
          foreach ($i in $items) {
            $existed = $false
            $old = $null
            if (Test-Path -LiteralPath $i.Path) {
              $p = Get-ItemProperty -LiteralPath $i.Path -Name $i.Name -ErrorAction SilentlyContinue
              if ($null -ne $p -and $null -ne $p.($i.Name)) { $existed = $true; $old = $p.($i.Name) }
            }
            $saved += [pscustomobject]@{ Path = $i.Path; Name = $i.Name; Type = $i.Type; Existed = $existed; Old = $old }
          }
          New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupFile) | Out-Null
          ConvertTo-Json -InputObject @($saved) | Set-Content -LiteralPath $backupFile -Encoding UTF8
        }
        foreach ($i in $items) {
          if (-not (Test-Path -LiteralPath $i.Path)) { New-Item -Path $i.Path -Force | Out-Null }
          New-ItemProperty -LiteralPath $i.Path -Name $i.Name -PropertyType $i.Type -Value $i.Value -Force | Out-Null
        }
        exit 0
        """;
        return template
            .Replace("__ITEMS__", BuildItems(RegistryTweaks[key]))
            .Replace("__BACKUP__", EscapeSingleQuotes(BackupFile(key)));
    }

    private static string UndoRegistryScript(string key)
    {
        const string template = """
        $ErrorActionPreference = 'Stop'
        $backupFile = '__BACKUP__'
        if (-not (Test-Path -LiteralPath $backupFile)) { exit 3 }
        $saved = @(Get-Content -LiteralPath $backupFile -Raw | ConvertFrom-Json)
        foreach ($s in $saved) {
          if ($s.Existed) {
            if (-not (Test-Path -LiteralPath $s.Path)) { New-Item -Path $s.Path -Force | Out-Null }
            New-ItemProperty -LiteralPath $s.Path -Name $s.Name -PropertyType $s.Type -Value $s.Old -Force | Out-Null
          } else {
            Remove-ItemProperty -LiteralPath $s.Path -Name $s.Name -ErrorAction SilentlyContinue
          }
        }
        Remove-Item -LiteralPath $backupFile -Force
        exit 0
        """;
        return template.Replace("__BACKUP__", EscapeSingleQuotes(BackupFile(key)));
    }

    public static string RestorePointScript()
    {
        return """
        $ErrorActionPreference = 'Stop'
        try {
          Enable-ComputerRestore -Drive "$env:SystemDrive\" -ErrorAction SilentlyContinue
          Checkpoint-Computer -Description "Rage Tweaks - vor Optimierung" -RestorePointType "MODIFY_SETTINGS"
          exit 0
        } catch {
          Write-Error $_
          exit 2
        }
        """;
    }

    private static string ApplyPowerScript()
    {
        const string template = """
        $ErrorActionPreference = 'Stop'
        # This changes the active power plan only; it does not alter voltages or clocks.
        $backupFile = '__BACKUP__'
        if (-not (Test-Path -LiteralPath $backupFile)) {
          $line = (powercfg /getactivescheme) | Out-String
          if ($line -match '([0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12})') {
            Set-Content -LiteralPath $backupFile -Value $Matches[1]
          }
        }
        powercfg /setactive SCHEME_MIN
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        exit 0
        """;
        return template.Replace("__BACKUP__", EscapeSingleQuotes(BackupFile("power")));
    }

    private static string UndoPowerScript()
    {
        const string template = """
        $ErrorActionPreference = 'Stop'
        $backupFile = '__BACKUP__'
        if (-not (Test-Path -LiteralPath $backupFile)) { exit 3 }
        $guid = (Get-Content -LiteralPath $backupFile -Raw).Trim()
        powercfg /setactive $guid
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        Remove-Item -LiteralPath $backupFile -Force
        exit 0
        """;
        return template.Replace("__BACKUP__", EscapeSingleQuotes(BackupFile("power")));
    }

    // Löscht nur den Inhalt eines Ordners im Benutzerkonto; Dateien, die gerade benutzt werden, werden übersprungen.
    private static string CleanFolderScript(string folderExpression) => """
        $ErrorActionPreference = 'SilentlyContinue'
        $dir = __DIR__
        if (-not (Test-Path -LiteralPath $dir)) { exit 0 }
        Get-ChildItem -LiteralPath $dir -Force | ForEach-Object {
          try { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction Stop } catch { }
        }
        exit 0
        """.Replace("__DIR__", folderExpression);

    private static string DnsFlushScript() => """
        $ErrorActionPreference = 'Stop'
        ipconfig /flushdns | Out-Null
        exit $LASTEXITCODE
        """;
}
