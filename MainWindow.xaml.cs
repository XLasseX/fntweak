using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FortniteTweakTool.Models;
using FortniteTweakTool.Services;

namespace FortniteTweakTool;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TweakItem> _tweaks = new();
    private readonly ICollectionView _view;
    private string _category = "fps";
    private bool _restorePointHandled;

    public MainWindow()
    {
        ThemeManager.Load();
        ThemeManager.ApplyResources();
        InitializeComponent();
        InitDesign();
        _view = CollectionViewSource.GetDefaultView(_tweaks);
        _view.Filter = o => o is TweakItem t && t.Category == _category;
        TweaksList.ItemsSource = _view;

        // FPS
        _tweaks.Add(new TweakItem { Category = "fps", Key = "gamemode", CanUndo = true, Name = "Windows Game Mode", Description = "Aktiviert die Windows-Game-Mode-Registry-Flags für das aktuelle Benutzerkonto. Die Wirkung hängt von Windows-Version und Spiel ab.", Risk = "Niedrig; Registryänderung, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "power", CanUndo = true, Name = "Hochleistungs-Energieplan", Description = "Wechselt zum Windows-Energieplan 'High performance'. Der bisherige Plan wird gemerkt und beim Rückgängigmachen wiederhergestellt. Kann Stromverbrauch, Lüftergeräusch und Temperatur erhöhen; nicht auf jedem PC verfügbar.", Risk = "Mittel; Strom/Hitze, fragt per UAC" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "gamedvr", CanUndo = true, Name = "Xbox Game Bar Hintergrundaufnahme aus", Description = "Schaltet die Hintergrundaufnahme (Game DVR) der Xbox Game Bar für dein Benutzerkonto aus. Aufnahmen per Game Bar funktionieren danach nicht mehr. Der Effekt auf die FPS ist je nach System klein.", Risk = "Niedrig; Registryänderung, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "transparency", CanUndo = true, Name = "Transparenzeffekte aus", Description = "Deaktiviert die Fenstertransparenz von Windows. Spart etwas GPU-Last auf dem Desktop; im Spiel selbst ist der Effekt meist gering.", Risk = "Niedrig; nur Optik, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "bgapps", CanUndo = true, Name = "Hintergrund-Apps aus", Description = "Verhindert, dass Apps im Hintergrund laufen, wenn du sie nicht nutzt. Kann CPU- und Netzwerklast senken; manche Apps melden sich dann nicht mehr im Hintergrund. Die Wirkung hängt von der Windows-Version ab.", Risk = "Niedrig; umkehrbar" });

        // Input
        _tweaks.Add(new TweakItem { Category = "input", Key = "mouse", CanUndo = true, Name = "Mausbeschleunigung aus", Description = "Deaktiviert 'Zeigerbeschleunigung verbessern' in Windows. Das wirkt vor allem auf dem Desktop; Fortnite nutzt meist Rohdaten der Maus, im Spiel ändert sich daher oft wenig.", Risk = "Niedrig; Registryänderung, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "input", Key = "stickykeys", CanUndo = true, Name = "Tastenkombi-Popups aus", Description = "Schaltet die Windows-Kürzel für Einrastfunktion, Anschlagverzögerung und Umschalttasten ab, damit mehrfaches Drücken von Shift im Spiel keinen Dialog öffnet.", Risk = "Niedrig; Barrierefreiheits-Kürzel entfallen, umkehrbar" });

        // Ping
        _tweaks.Add(new TweakItem { Category = "ping", Key = "dnsflush", CanUndo = false, Name = "DNS-Cache leeren", Description = "Leert einmalig den DNS-Cache von Windows. Kann bei Verbindungsproblemen helfen, senkt aber den Spielping nicht direkt. Es muss nichts zurückgesetzt werden.", Risk = "Niedrig; fragt per UAC" });

        Loaded += (_, _) => { RefreshSystem(); PlayPageAnimation(); };
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

    private void ShowPage(string title, string subtitle, string intro, bool dashboard = false, bool restore = false, bool settings = false, bool tweaks = false, bool design = false)
    {
        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
        SectionIntro.Text = intro;
        DashboardPanel.Visibility = dashboard ? Visibility.Visible : Visibility.Collapsed;
        ContentPanel.Visibility = dashboard ? Visibility.Collapsed : Visibility.Visible;
        RestoreActions.Visibility = restore ? Visibility.Visible : Visibility.Collapsed;
        SettingsActions.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        TweaksList.Visibility = tweaks ? Visibility.Visible : Visibility.Collapsed;
        DesignActions.Visibility = design ? Visibility.Visible : Visibility.Collapsed;
        PlayPageAnimation();
    }

    private void ShowTweakPage(string title, string subtitle, string intro, string category)
    {
        _category = category;
        _view.Refresh();
        ShowPage(title, subtitle, intro, tweaks: true);
    }

    private void Dashboard_Click(object s, RoutedEventArgs e) => ShowPage("Dashboard", "Echte Systemdaten und nachvollziehbare Aktionen.", "", dashboard: true);

    private void Fps_Click(object s, RoutedEventArgs e) =>
        ShowTweakPage("FPS Boost", "Windows-Leistungsoptionen mit nachvollziehbarem Nutzen und Risiko.",
            "Es werden keine FPS-Werte erfunden. Für einen belastbaren Vorher-Nachher-Vergleich nutze eine reproduzierbare Fortnite-Route und dasselbe Grafikprofil. Jeder Tweak lässt sich einzeln rückgängig machen.", "fps");

    private void Input_Click(object s, RoutedEventArgs e) =>
        ShowTweakPage("Input Delay", "Eingabelatenz ohne riskante Maus-/Timer-Hacks.",
            "Die App verändert keine USB-Polling-Raten, HPET-, Timer- oder Treiberwerte automatisch. Die folgenden Windows-Einstellungen für Maus und Tastatur sind umkehrbar. Aktiviere in Fortnite zusätzlich NVIDIA Reflex (falls verfügbar) und verwende aktuelle GPU-Treiber; die tatsächliche Latenz ist hardware- und spielabhängig.", "input");

    private void Ping_Click(object s, RoutedEventArgs e) =>
        ShowTweakPage("Ping Optimizer", "Diagnose statt unrealistischer Ping-Versprechen.",
            "Ein ICMP-Test zu 1.1.1.1 ist kein Fortnite-Server-Ping. Verwende Ethernet, pausiere große Downloads/Uploads und prüfe Router-Auslastung. Die App nimmt keine DNS-/TCP-Registry-Hacks vor; hier gibt es nur das einmalige Leeren des DNS-Caches.", "ping");

    private void Restore_Click(object s, RoutedEventArgs e) => ShowPage("Restore Center", "Backups, Wiederherstellungspunkt und Undo.", "", restore: true);
    private void Settings_Click(object s, RoutedEventArgs e) => ShowPage("Settings & Logs", "Lokale Protokolle und App-Einstellungen.", "", settings: true);

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshSystem();
        ActionStatus.Text = "Systemanalyse aktualisiert.";
    }

    private async void PingTest_Click(object sender, RoutedEventArgs e)
    {
        DashPing.Text = "Teste…";
        var result = await SystemService.PingAsync();
        DashPing.Text = result.ms.HasValue ? $"{result.ms.Value} ms" : "Fehler";
        ActionStatus.Text = result.status;
        TweakService.Log("Ping-Test: " + result.status);
    }

    // Legt höchstens einmal pro Programmstart einen Wiederherstellungspunkt an (oder fragt, ob trotzdem fortgefahren werden soll).
    private async Task<bool> EnsureRestorePointAsync()
    {
        if (_restorePointHandled) return true;
        ActionStatus.Text = "Erstelle Wiederherstellungspunkt…";
        var rp = await TweakService.RunElevatedPowerShellAsync(TweakService.RestorePointScript());
        TweakService.Log("Wiederherstellungspunkt: " + rp.message);
        if (rp.ok) { _restorePointHandled = true; return true; }

        var proceed = MessageBox.Show(
            "Der Wiederherstellungspunkt konnte nicht bestätigt werden:\n" + rp.message + "\n\nTrotzdem mit den reversiblen Tweaks fortfahren?",
            "Wiederherstellungspunkt fehlgeschlagen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (proceed != MessageBoxResult.Yes) return false;
        _restorePointHandled = true;
        return true;
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

        if (!await EnsureRestorePointAsync()) { ActionStatus.Text = "Abgebrochen, keine Tweaks angewendet."; return; }

        var messages = new List<string>();
        if (OneClickGameMode.IsChecked == true)
        {
            ActionStatus.Text = "Aktiviere Game Mode…";
            var r = await TweakService.ApplyAsync("gamemode");
            messages.Add("Game Mode: " + (r.ok ? "angewendet" : r.message));
            SetStatus("gamemode", r.ok ? "Angewendet" : "Fehlgeschlagen");
        }
        if (OneClickPower.IsChecked == true)
        {
            ActionStatus.Text = "Aktiviere Energieplan…";
            var r = await TweakService.ApplyAsync("power");
            messages.Add("Energieplan: " + (r.ok ? "angewendet" : r.message));
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

        var confirm = MessageBox.Show($"'{item.Name}' anwenden?\n\n{item.Description}\n\nRisiko: {item.Risk}\n\nVorher wird (einmal pro Programmstart) versucht, einen Wiederherstellungspunkt zu erstellen.", "Tweak bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        if (!await EnsureRestorePointAsync()) return;

        item.Status = "Wird angewendet…";
        var applied = await TweakService.ApplyAsync(key);
        item.Status = applied.ok
            ? "Angewendet" + (string.IsNullOrEmpty(applied.message) ? "" : " – " + applied.message)
            : "Fehlgeschlagen – " + applied.message;
        TweakService.Log(item.Name + ": " + item.Status);
        RefreshSystem();
    }

    private async void UndoTweak_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not string key) return;
        var item = _tweaks.FirstOrDefault(x => x.Key == key);
        if (item == null) return;

        var confirm = MessageBox.Show($"'{item.Name}' rückgängig machen?\n\nDer gespeicherte Originalzustand wird wiederhergestellt.", "Änderung rückgängig machen", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        item.Status = "Wird zurückgesetzt…";
        var result = await TweakService.UndoAsync(key);
        item.Status = result.ok ? "Zurückgesetzt" : "Rückgängig fehlgeschlagen – " + result.message;
        TweakService.Log("Undo " + item.Name + ": " + (result.ok ? "ok" : result.message));
    }

    private async void RestorePoint_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Windows-Wiederherstellungspunkt jetzt erstellen? Dies erfordert Administratorrechte und kann durch Windows-Richtlinien blockiert sein.", "Bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var result = await TweakService.RunElevatedPowerShellAsync(TweakService.RestorePointScript());
        ActionStatus.Text = result.message;
        MessageBox.Show(result.message, "Restore Center", MessageBoxButton.OK, result.ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
        TweakService.Log("Restore point: " + result.message);
    }

    private async void UndoAll_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Alle Tweaks mit gespeichertem Backup auf den Originalzustand zurücksetzen? Für den Energieplan kann Windows eine UAC-Abfrage anzeigen.", "Alle Tweaks rückgängig machen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        var lines = new List<string>();
        foreach (var item in _tweaks.Where(t => t.CanUndo && TweakService.HasBackup(t.Key)).ToList())
        {
            var r = await TweakService.UndoAsync(item.Key);
            item.Status = r.ok ? "Zurückgesetzt" : "Rückgängig fehlgeschlagen – " + r.message;
            lines.Add(item.Name + ": " + (r.ok ? "zurückgesetzt" : r.message));
            TweakService.Log("Undo " + item.Name + ": " + (r.ok ? "ok" : r.message));
        }

        var text = lines.Count == 0 ? "Keine Backups gefunden – es gibt nichts zurückzusetzen." : string.Join("\n", lines);
        ActionStatus.Text = text;
        MessageBox.Show(text, "Restore Center", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SetStatus(string key, string status)
    {
        var item = _tweaks.FirstOrDefault(x => x.Key == key);
        if (item != null) item.Status = status;
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
