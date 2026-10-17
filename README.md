# Fortnite Tweak Tool (Windows)

Eine native WPF-App für Windows 10/11 mit Dashboard, Systemanalyse, Ping-Diagnose, ausgewählten reversiblen Tweaks, Restore Center und lokalen Logs.

## Voraussetzungen
- Windows 10 oder Windows 11
- .NET 8 SDK (Windows)
- Für Änderungen: UAC-/Administratorfreigabe
- GPU-Abfrage nutzt `System.Management`

## Starten
1. Installiere das .NET 8 SDK für Windows.
2. Öffne PowerShell im Projektordner.
3. Führe aus:

```powershell
dotnet restore
dotnet run
```

## Als EXE veröffentlichen
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
Die veröffentlichte EXE liegt im Unterordner `bin\Release\net8.0-windows\win-x64\publish\`.

## Enthalten
- System-/Windows-Informationen, GPU-Abfrage und Erkennung des Fortnite-Prozesses
- ICMP-Ping-Test zu 1.1.1.1 (kein Fortnite-Server-Ping)
- One-click-Ablauf mit vorherigem Versuch, einen Windows-Wiederherstellungspunkt anzulegen
- Tweaks (jeweils mit Backup des Originalzustands und einzelnem "Rückgängig"):
  - FPS: Windows Game Mode, Hochleistungs-Energieplan, Game-DVR-Hintergrundaufnahme aus, Transparenzeffekte aus, Hintergrund-Apps aus
  - Input: Mausbeschleunigung aus, Tastenkombi-Popups (Einrast-/Filter-/Umschalttasten) aus
  - Ping: DNS-Cache leeren (einmalige Aktion, nichts zurückzusetzen)
- Benutzer-Tweaks (HKCU) laufen ohne UAC; Energieplan, DNS-Reset und Wiederherstellungspunkt fragen per UAC
- **Design & Effekte** (neue Seite, alles live und automatisch gespeichert):
  - 8 Design-Vorlagen (Standard Violett, Neon Cyber, Ozean, Sonnenuntergang, Matrix, Blutrot, Eis hell, Graphit)
  - Hintergrund: einfarbig, animierter Farbverlauf, Aurora oder **eigenes Bild / Video** (PNG, JPG, BMP, TIFF, MP4, WMV, AVI, MOV, MKV; Videos laufen stumm in Dauerschleife)
  - Alle 6 Farben frei wählbar (Farbwähler mit RGB-Reglern, Hex-Feld, Palette und Live-Vorschau)
  - Partikel-Effekt (aufsteigend, fallend, schwebend; Anzahl einstellbar), Animationsgeschwindigkeit, Effektstärke
  - Panel-Deckkraft (Glas-Effekt), Eckenradius, Neon-Glow, Seitenwechsel-Animation, Schriftart
  - Titelleiste passt sich an (Windows 11: Farbe, Windows 10: dunkel/hell)
  - Design exportieren / importieren / zurücksetzen
- Restore Center: "Alle Tweaks rückgängig machen", Backup- und Log-Ordner
- UAC-Abfrage über Windows; Fehler und Abbrüche werden protokolliert

## Wichtige Einschränkungen
- Ein Wiederherstellungspunkt kann durch Windows-Systemschutz, Richtlinien oder bereits erstellte Punkte blockiert sein. Die App weist darauf hin und fragt vor dem Fortfahren erneut.
- `Checkpoint-Computer` kann je nach Systemzustand scheitern. Ein Erfolg wird nicht vorgetäuscht.
- Der Registry-Export kann fehlschlagen, wenn der Schlüssel vorher nicht existiert; der Undo entfernt in diesem Fall die beiden gesetzten Werte, statt andere Einstellungen zu verändern.
- Der Energieplanwechsel ist eine Windows-Standardfunktion und verändert keine CPU-/GPU-Spannungen.
- Ping hängt primär von Route, ISP, WLAN, Router-Auslastung und Spielserver ab. Die App verspricht keine Verbesserung.
- FPS, Frametime und echte Input-Latenz werden nicht künstlich angezeigt. Ein verlässlicher Fortnite-Benchmark benötigt eine kontrollierte Spielszene und passende Telemetrie; diese Version zeigt deshalb keine erfundenen Werte.
- Änderungen an HPET, TCP-Registry, Maus-Timer, Diensten oder Treibern werden bewusst nicht automatisiert.
- Teste zuerst auf einem nicht kritischen Windows-Benutzerkonto. Ein Wiederherstellungspunkt ersetzt kein vollständiges Backup.

## Datenschutz
Alle Logs, Backups und die Design-Einstellungen (`theme.json`) liegen lokal in `%LOCALAPPDATA%\\FortniteTweakTool`. Es werden keine Daten übertragen.
