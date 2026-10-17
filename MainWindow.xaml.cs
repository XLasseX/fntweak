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
        InitOverlay();
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

        // FPS (weitere)
        _tweaks.Add(new TweakItem { Category = "fps", Key = "animations", CanUndo = true, Name = "Windows-Animationen aus", Description = "Schaltet Fensteranimationen, Menü-Verzögerung und Taskleisten-Animationen ab. Das Windows fühlt sich direkter an; im Spiel selbst ist der FPS-Effekt gering.", Risk = "Niedrig; nur Optik, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "notifications", CanUndo = true, Name = "Windows-Benachrichtigungen aus", Description = "Schaltet Toast-Benachrichtigungen ab, damit während des Spielens keine Pop-ups aufpoppen oder das Spiel kurz in den Hintergrund fällt.", Risk = "Niedrig; du siehst keine Windows-Pop-ups mehr, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "gamebarpanel", CanUndo = true, Name = "Game-Bar-Startfenster & Controller-Popup aus", Description = "Verhindert, dass die Xbox Game Bar beim Spielstart oder per Controller-Taste dazwischenfunkt.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "tips", CanUndo = true, Name = "Tipps & Vorschläge aus", Description = "Deaktiviert Windows-Tipps und Vorschläge im Startmenü und in den Einstellungen. Weniger Hintergrund-Einblendungen.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "fse", CanUndo = true, Name = "Vollbild-Optimierungen global aus", Description = "Deaktiviert die Windows-Vollbild-Optimierungen für Spiele. Die Wirkung hängt stark von Spiel, Treiber und Grafikkarte ab und kann auch nichts bringen. Bei Problemen einfach rückgängig machen.", Risk = "Niedrig bis mittel; Wirkung nicht garantiert, umkehrbar" });

        // Input (weitere)
        _tweaks.Add(new TweakItem { Category = "input", Key = "mousespeed", CanUndo = true, Name = "Zeigergeschwindigkeit auf 6/11", Description = "Setzt die Windows-Mausgeschwindigkeit auf die Standardstufe 6 von 11 (1:1-Übersetzung). Wichtig für gleichbleibendes Mausgefühl auf dem Desktop; Fortnite nutzt meist eigene DPI/Sensitivity.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "input", Key = "keyboard", CanUndo = true, Name = "Tastatur: schnellste Wiederholrate", Description = "Setzt Wiederholverzögerung auf kurz und Wiederholrate auf schnell. Betrifft vor allem Eingaben in Windows (z. B. Chat), nicht die Spielsteuerung.", Risk = "Niedrig; umkehrbar" });

        // Cleaner
        _tweaks.Add(new TweakItem { Category = "clean", Key = "tempclean", CanUndo = false, Name = "Temporäre Dateien löschen", Description = "Löscht den Inhalt deines Temp-Ordners (%TEMP%). Dateien, die gerade benutzt werden, bleiben erhalten. Gibt Speicherplatz frei; FPS verbessert sich dadurch nicht.", Risk = "Niedrig; Dateien werden endgültig gelöscht, nicht umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "shadercache", CanUndo = false, Name = "DirectX-Shader-Cache leeren", Description = "Löscht den DirectX-Shader-Cache. Hilft manchmal bei Grafikfehlern oder Rucklern nach Treiberupdates. Beim nächsten Spielstart wird der Cache neu aufgebaut, was kurz zu Rucklern führen kann.", Risk = "Niedrig; baut sich selbst neu auf, nicht umkehrbar" });

        // Ping
        _tweaks.Add(new TweakItem { Category = "ping", Key = "dnsflush", CanUndo = false, Name = "DNS-Cache leeren", Description = "Leert einmalig den DNS-Cache von Windows. Kann bei Verbindungsproblemen helfen, senkt aber den Spielping nicht direkt. Es muss nichts zurückgesetzt werden.", Risk = "Niedrig; fragt per UAC" });

        // Weitere Tweaks
        _tweaks.Add(new TweakItem { Category = "fps", Key = "visualfx", CanUndo = true, Name = "Visuelle Effekte: beste Leistung", Description = "Stellt Windows auf 'Für optimale Leistung anpassen'. Weniger Desktop-Effekte, etwas weniger GPU-Last auf dem Desktop; im Spiel selbst meist kaum messbar.", Risk = "Niedrig; nur Optik, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "dragfull", CanUndo = true, Name = "Fensterinhalt beim Ziehen aus", Description = "Beim Verschieben von Fenstern wird nur der Umriss gezeigt statt des ganzen Fensters. Spart Rechenleistung auf dem Desktop.", Risk = "Niedrig; nur Optik, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "aeropeek", CanUndo = true, Name = "Aero Peek (Taskleisten-Vorschau) aus", Description = "Schaltet die Fenstervorschau beim Überfahren der Taskleisten-Ecke ab. Weniger Compositing-Aufwand.", Risk = "Niedrig; nur Optik, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "windowedopt", CanUndo = true, Name = "Optimierungen für Fenstermodus-Spiele an", Description = "Aktiviert die Windows-Option 'Optimierungen für Spiele im Fenstermodus' (bessere Präsentation im Fenster-/Rahmenlos-Modus). Wirkung hängt von Spiel und Treiber ab.", Risk = "Niedrig; Wirkung nicht garantiert, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "startupdelay", CanUndo = true, Name = "Autostart-Verzögerung aus", Description = "Entfernt die künstliche Wartezeit, mit der Windows Autostart-Programme nach der Anmeldung startet. Der Desktop ist schneller bedienbar, dafür starten Autostart-Apps früher.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "fps", Key = "appsuggest", CanUndo = true, Name = "Vorschlags-Apps automatisch installieren: aus", Description = "Verhindert, dass Windows ungefragt Vorschlags- und Partner-Apps nachinstalliert. Weniger Hintergrund-Downloads und Autostart-Müll.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "input", Key = "mousehover", CanUndo = true, Name = "Maus-Hover-Zeit verkürzen", Description = "Verkürzt die Zeit, bis Tooltips und Hover-Effekte erscheinen. Macht das Desktop-Gefühl direkter.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "input", Key = "mousetrails", CanUndo = true, Name = "Mauszeiger-Spur aus", Description = "Stellt sicher, dass keine Zeigerspur angezeigt wird.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "input", Key = "touchfeedback", CanUndo = true, Name = "Touch- und Gesten-Feedback aus", Description = "Schaltet die visuellen Punkte/Wellen bei Touch-Eingaben ab. Nur relevant bei Touchscreen oder Touchpad-Gesten.", Risk = "Niedrig; nur Optik, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "input", Key = "nowinkey", CanUndo = true, Name = "Windows-Taste: Tastenkürzel aus", Description = "Deaktiviert alle Win-Tastenkombinationen (Win+D, Win+E …). So landest du im Spiel nicht aus Versehen im Startmenü. Das Startmenü öffnest du dann nur noch per Maus oder Strg+Esc.", Risk = "Mittel; Windows-Kürzel entfallen, umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "adid", CanUndo = true, Name = "Werbe-ID aus", Description = "Deaktiviert die Werbe-ID deines Benutzerkontos. Apps können dich damit nicht mehr über Apps hinweg für Werbung verfolgen.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "tailored", CanUndo = true, Name = "Maßgeschneiderte Erlebnisse aus", Description = "Windows nutzt Diagnosedaten nicht mehr für personalisierte Tipps und Werbung.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "feedback", CanUndo = true, Name = "Feedback-Abfragen aus", Description = "Windows fragt nicht mehr nach Feedback zur Nutzung.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "lockscreenads", CanUndo = true, Name = "Sperrbildschirm-Tipps aus", Description = "Schaltet Tipps, Fakten und Werbung auf dem Sperrbildschirm ab.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "startads", CanUndo = true, Name = "Startmenü-Werbung aus", Description = "Entfernt vorgeschlagene Inhalte und Werbung im Startmenü und in den Einstellungen.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "bingsearch", CanUndo = true, Name = "Web-Suche im Startmenü aus", Description = "Die Windows-Suche durchsucht nur noch deinen PC und schickt Eingaben nicht an Bing.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "searchhighlights", CanUndo = true, Name = "Suchhighlights aus", Description = "Blendet die täglich wechselnden Bilder und Themen im Suchfeld aus.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "inkdata", CanUndo = true, Name = "Tipp- und Handschrift-Datensammlung aus", Description = "Windows sammelt keine Tipp- und Handschriftmuster mehr zur Personalisierung.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "autoplay", CanUndo = true, Name = "Autoplay für Laufwerke aus", Description = "Beim Einstecken von USB-Sticks oder Laufwerken öffnet sich kein Autoplay-Dialog mehr.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "fileext", CanUndo = true, Name = "Dateiendungen anzeigen", Description = "Zeigt im Explorer immer die Dateiendung (.exe, .txt …). Hilft, gefährliche Dateien zu erkennen.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "sys", Key = "taskchat", CanUndo = true, Name = "Chat-Symbol in der Taskleiste aus", Description = "Entfernt das Chat/Teams-Symbol aus der Taskleiste.", Risk = "Niedrig; umkehrbar" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "recyclebin", CanUndo = false, Name = "Papierkorb leeren", Description = "Leert den Papierkorb endgültig. Gibt Speicherplatz frei.", Risk = "Mittel; Dateien im Papierkorb sind danach endgültig weg" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "thumbcache", CanUndo = false, Name = "Vorschaubild-Cache löschen", Description = "Löscht den Explorer-Cache der Vorschaubilder. Wird beim Öffnen von Ordnern neu aufgebaut. Gesperrte Dateien bleiben erhalten.", Risk = "Niedrig; baut sich selbst neu auf" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "recentfiles", CanUndo = false, Name = "Zuletzt verwendete Dateien löschen", Description = "Leert die Liste 'Zuletzt verwendet' (nur die Verknüpfungen, nicht die Dateien selbst).", Risk = "Niedrig; nur die Verlaufsliste wird gelöscht" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "nvcache", CanUndo = false, Name = "NVIDIA-Shader-Cache leeren", Description = "Löscht den NVIDIA-DirectX- und OpenGL-Shader-Cache. Hilft manchmal bei Grafikfehlern nach Treiberupdates; beim nächsten Spielstart baut sich der Cache neu auf (kurze Ruckler möglich).", Risk = "Niedrig; baut sich selbst neu auf" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "amdcache", CanUndo = false, Name = "AMD-Shader-Cache leeren", Description = "Löscht den AMD-Shader-Cache. Beim nächsten Spielstart wird er neu aufgebaut (kurze Ruckler möglich).", Risk = "Niedrig; baut sich selbst neu auf" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "fnlogs", CanUndo = false, Name = "Fortnite-Logs & Crash-Berichte löschen", Description = "Löscht die lokalen Fortnite-Logdateien und Crash-Berichte. Keine Spieldaten, keine Einstellungen.", Risk = "Niedrig; nur Protokolldateien" });
        _tweaks.Add(new TweakItem { Category = "clean", Key = "wer", CanUndo = false, Name = "Windows-Fehlerberichte löschen", Description = "Löscht gesammelte Windows-Fehlerberichte (WER) deines Benutzerkontos.", Risk = "Niedrig; nur Berichte" });
        _tweaks.Add(new TweakItem { Category = "ping", Key = "arpflush", CanUndo = false, Name = "ARP-Cache leeren", Description = "Leert die Zuordnung von IP- zu MAC-Adressen im lokalen Netzwerk. Kann bei Verbindungsproblemen im Heimnetz helfen, senkt den Ping aber nicht direkt.", Risk = "Niedrig; fragt per UAC, nichts zurückzusetzen" });

        foreach (var once in _tweaks.Where(x => !x.CanUndo)) once.Status = "Einmalige Aktion";
        Loaded += (_, _) => { RefreshSystem(); PlayPageAnimation(); _ = RefreshTweakStatesAsync(); };
    }

    // Liest beim Start den echten Windows-Zustand und zeigt "Aktiv" für bereits angewendete Tweaks.
    private async Task RefreshTweakStatesAsync()
    {
        foreach (var item in _tweaks.Where(t => t.CanUndo).ToList())
        {
            var active = await TweakService.IsAppliedAsync(item.Key);
            item.Status = active ? "Aktiv" : (TweakService.HasBackup(item.Key) ? "Nicht aktiv (Original)" : "Nicht angewendet");
        }
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

    private void ShowPage(string title, string subtitle, string intro, bool dashboard = false, bool restore = false, bool settings = false, bool tweaks = false, bool design = false, bool overlay = false)
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
        OverlayActions.Visibility = overlay ? Visibility.Visible : Visibility.Collapsed;
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

    private void Sys_Click(object s, RoutedEventArgs e) =>
        ShowTweakPage("Privacy & Ruhe", "Weniger Werbung, Tracking und Störungen im Windows-Alltag.",
            "Alle Einstellungen gelten nur für dein Benutzerkonto, sind einzeln umkehrbar und verändern keine Systemdienste. Sie bringen keine FPS, sorgen aber für weniger Pop-ups, Hintergrundaktivität und Datensammlung.", "sys");

    private void Clean_Click(object s, RoutedEventArgs e) =>
        ShowTweakPage("Cleaner", "Aufräumen im eigenen Benutzerkonto.",
            "Diese Aktionen löschen nur Dateien in deinem Benutzerkonto (Temp-Ordner, Shader-Cache). Gelöschte Dateien lassen sich nicht wiederherstellen. Ein Wiederherstellungspunkt wird dafür nicht angelegt.", "clean");

    private void Restore_Click(object s, RoutedEventArgs e) => ShowPage("Restore Center", "Backups, Wiederherstellungspunkt und Undo.", "", restore: true);
    private void Settings_Click(object s, RoutedEventArgs e) => ShowPage("Settings & Logs", "Lokale Protokolle und App-Einstellungen.", "", settings: true);

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshSystem(); _ = RefreshTweakStatesAsync();
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

        var confirm = MessageBox.Show($"'{item.Name}' anwenden?\n\n{item.Description}\n\nRisiko: {item.Risk}" + (item.Category == "clean" ? "" : "\n\nVorher wird (einmal pro Programmstart) versucht, einen Wiederherstellungspunkt zu erstellen."), "Tweak bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        if (item.Category != "clean" && !await EnsureRestorePointAsync()) return;

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

        await RefreshTweakStatesAsync();
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
