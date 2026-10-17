using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using FortniteTweakTool.Models;
using FortniteTweakTool.Services;

namespace FortniteTweakTool;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TweakItem> _tweaks = new();

    public MainWindow()
    {
        InitializeComponent();
        TweaksList.ItemsSource = _tweaks;
        _tweaks.Add(new TweakItem { Name = "Windows Game Mode", Description = "Aktiviert die Windows-Game-Mode-Registry-Flags für das aktuelle Benutzerkonto. Die Wirkung hängt von Windows-Version und Spiel ab.", Risk = "Niedrig; Registryänderung", Key = "gamemode", Selected = true });
        _tweaks.Add(new TweakItem { Name = "Hochleistungs-Energieplan", Description = "Wechselt zum Windows-Energieplan 'High performance'. Kann Stromverbrauch, Lüftergeräusch und Temperatur erhöhen; nicht auf jedem PC verfügbar.", Risk = "Mittel; Strom/Hitze", Key = "power" });
        Loaded += (_, _) => RefreshSystem();
    }

    private void RefreshSystem()
    {
        SystemReport.Text = SystemService.GetSystemReport();
        DashWindows.Text = Environment.OSVersion.VersionString;
        DashFortnite.Text = Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length > 0 ? "Läuft" : "Nicht erkannt";
        HeaderStatus.Text = SystemService.IsAdmin() ? "Administrator-Kontext" : "Standardrechte • UAC bei Änderungen";
        BackupInfo.Text = $"Backup-Ordner: {TweakService.BackupDirectory}{Environment.NewLine}Log: {TweakService.LogPath}";
        TweakService.Log("Systemanalyse aktualisiert.");
    }

    private void ShowPage(string title, string subtitle, string intro, bool dashboard = false, bool restore = false, bool settings = false)
    {
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        DashboardPanel.Visibility = dashboard ? Visibility.Visible : Visibility.Collapsed;
        ContentPanel.Visibility = dashboard ? Visibility.Collapsed : Visibility.Visible;
        RestoreActions.Visibility = restore ? Visibility.Visible : Visibility.Collapsed;
        SettingsActions.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        TweaksList.Visibility = (!dashboard && !restore && !settings) ? Visibility.Visible : Visibility.Collapsed;
        SectionIntro.Text = intro;
        if (restore || settings) TweaksList.Visibility = Visibility.Collapsed;
    }

    private void Dashboard_Click(object s, RoutedEventArgs e) => ShowPage("Dashboard", "Echte Systemdaten und nachvollziehbare Aktionen.", "", dashboard: true);
    private void Fps_Click(object s, RoutedEventArgs e)
    {
        ShowPage("FPS Boost", "Windows-Leistungsoptionen mit nachvollziehbarem Nutzen und Risiko.", "Es werden keine FPS-Werte erfunden. Für einen belastbaren Vorher-Nachher-Vergleich nutze eine reproduzierbare Fortnite-Route und dasselbe Grafikprofil.");
        TweaksList.Visibility = Visibility.Visible;
    }
    private void Input_Click(object s, RoutedEventArgs e)
    {
        ShowPage("Input Delay", "Eingabelatenz ohne riskante Maus-/Timer-Hacks.", "Die App verändert keine USB-Polling-Raten, HPET-, Timer- oder Treiberwerte automatisch. Aktiviere in Fortnite NVIDIA Reflex (falls verfügbar) und verwende aktuelle GPU-Treiber; die tatsächliche Latenz ist hardware- und spielabhängig.");
        TweaksList.Visibility = Visibility.Visible;
    }
    private void Ping_Click(object s, RoutedEventArgs e)
    {
        ShowPage("Ping Optimizer", "Diagnose statt unrealistischer Ping-Versprechen.", "Ein ICMP-Test zu 1.1.1.1 ist kein Fortnite-Server-Ping. Verwende Ethernet, pausiere große Downloads/Uploads und prüfe Router-Auslastung. Die App nimmt keine DNS-/TCP-Registry-Hacks vor.");
        TweaksList.Visibility = Visibility.Visible;
    }
    private void Restore_Click(object s, RoutedEventArgs e) => ShowPage("Restore Center", "Backups, Wiederherstellungspunkt und Undo.", "", restore: true);
    private void Settings_Click(object s, RoutedEventArgs e) => ShowPage("Settings & Logs", "Lokale Protokolle und App-Einstellungen.", "", settings: true);

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshSystem();
        ActionStatus.Text = "Systemanalyse aktualisiert.";
        await Task.CompletedTask;
    }

    private async void PingTest_Click(object sender, RoutedEventArgs e)
    {
        DashPing.Text = "Teste…";
        var result = await SystemService.PingAsync();
        DashPing.Text = result.ms.HasValue ? $"{result.ms.Value} ms" : "Fehler";
        ActionStatus.Text = result.status;
        TweakService.Log("Ping-Test: " + result.status);
    }

    private async void OneClick_Click(object sender, RoutedEventArgs e)
    {
        var selected = new List<string>();
        if (OneClickGameMode.IsChecked == true) selected.Add("Game Mode");
        if (OneClickPower.IsChecked == true) selected.Add("Hochleistungs-Energieplan");
        if (selected.Count == 0) { ActionStatus.Text = "Wähle mindestens eine Optimierung aus."; return; }

        var answer = MessageBox.Show(
            "Vor der Optimierung wird versucht, einen Windows-Wiederherstellungspunkt anzulegen. Danach werden die ausgewählten Einstellungen geändert; Windows kann eine UAC-Abfrage anzeigen. Fortfahren?\n\n" +
            string.Join("\n", selected) + "\n\nEin Wiederherstellungspunkt ist nicht garantiert verfügbar.",
            "Optimierung bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        ActionStatus.Text = "Erstelle Wiederherstellungspunkt…";
        var rp = await TweakService.RunElevatedPowerShellAsync(TweakService.RestorePointScript());
        TweakService.Log("Wiederherstellungspunkt: " + rp.message);
        if (!rp.ok)
        {
            var proceed = MessageBox.Show("Der Wiederherstellungspunkt konnte nicht bestätigt werden:\n" + rp.message + "\n\nTrotzdem mit den reversiblen Tweaks fortfahren?", "Wiederherstellungspunkt fehlgeschlagen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (proceed != MessageBoxResult.Yes) { ActionStatus.Text = "Abgebrochen, keine Tweaks angewendet."; return; }
        }

        var messages = new List<string>();
        if (OneClickGameMode.IsChecked == true)
        {
            ActionStatus.Text = "Aktiviere Game Mode…";
            var r = await TweakService.RunElevatedPowerShellAsync(TweakService.ApplyGameModeScript());
            messages.Add("Game Mode: " + r.message);
            SetStatus("gamemode", r.ok ? "Angewendet" : "Fehlgeschlagen");
        }
        if (OneClickPower.IsChecked == true)
        {
            ActionStatus.Text = "Aktiviere Energieplan…";
            var r = await TweakService.RunElevatedPowerShellAsync(TweakService.HighPerformancePlanScript());
            messages.Add("Energieplan: " + r.message);
            SetStatus("power", r.ok ? "Angewendet" : "Fehlgeschlagen");
        }
        ActionStatus.Text = string.Join("\n", messages);
        RefreshSystem();
    }

    private async void ApplyTweak_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string key) return;
        var item = _tweaks.FirstOrDefault(x => x.Key == key);
        if (item == null) return;
        var result = MessageBox.Show($"'{item.Name}' anwenden?\n\n{item.Description}\n\nRisiko: {item.Risk}\n\nVorher wird versucht, einen Wiederherstellungspunkt zu erstellen.", "Tweak bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        var rp = await TweakService.RunElevatedPowerShellAsync(TweakService.RestorePointScript());
        if (!rp.ok && MessageBox.Show("Wiederherstellungspunkt nicht bestätigt:\n" + rp.message + "\nTrotzdem fortfahren?", "Hinweis", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var script = key == "gamemode" ? TweakService.ApplyGameModeScript() : TweakService.HighPerformancePlanScript();
        var applied = await TweakService.RunElevatedPowerShellAsync(script);
        item.Status = applied.ok ? "Angewendet" : "Fehlgeschlagen";
        ActionStatus.Text = item.Name + ": " + applied.message;
        TweakService.Log(item.Name + ": " + applied.message);
        TweaksList.Items.Refresh();
        RefreshSystem();
    }

    private async void RestorePoint_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Windows-Wiederherstellungspunkt jetzt erstellen? Dies erfordert Administratorrechte und kann durch Windows-Richtlinien blockiert sein.", "Bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var result = await TweakService.RunElevatedPowerShellAsync(TweakService.RestorePointScript());
        ActionStatus.Text = result.message;
        MessageBox.Show(result.message, "Restore Center", MessageBoxButton.OK, result.ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        TweakService.Log("Restore point: " + result.message);
    }

    private async void UndoGameMode_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Game-Mode-Registrywerte anhand des letzten verfügbaren Backups wiederherstellen? Falls kein Backup existiert, werden die beiden Werte entfernt.", "Änderung rückgängig machen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var result = await TweakService.RunElevatedPowerShellAsync(TweakService.RestoreGameModeScript());
        ActionStatus.Text = result.message;
        SetStatus("gamemode", result.ok ? "Zurückgesetzt" : "Undo fehlgeschlagen");
        TweakService.Log("Undo Game Mode: " + result.message);
        MessageBox.Show(result.message, "Restore Center", MessageBoxButton.OK, result.ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void SetStatus(string key, string status)
    {
        var item = _tweaks.FirstOrDefault(x => x.Key == key);
        if (item != null) item.Status = status;
        TweaksList.Items.Refresh();
    }

    private void OpenBackup_Click(object sender, RoutedEventArgs e) => OpenFolder(TweakService.BackupDirectory);
    private void OpenLog_Click(object sender, RoutedEventArgs e) => OpenFolder(Path.GetDirectoryName(TweakService.LogPath)!);
    private void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }
    private void ShowLog_Click(object sender, RoutedEventArgs e)
    {
        LogOutput.Text = File.Exists(TweakService.LogPath) ? File.ReadAllText(TweakService.LogPath) : "Noch keine Logeinträge.";
    }
    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Lokales Protokoll wirklich leeren?", "Bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        if (File.Exists(TweakService.LogPath)) File.WriteAllText(TweakService.LogPath, "");
        LogOutput.Text = "Log geleert.";
    }
}
