using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FortniteTweakTool.Models;
using FortniteTweakTool.Services;

namespace FortniteTweakTool;

public partial class MainWindow
{
    private const int HotkeyId = 9001;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private Button? _overlayToggleButton;
    private TextBlock? _overlayHotkeyInfo;
    private bool _overlayUiReady;
    private static OverlaySettings O => OverlayManager.Current;

    private void InitOverlay()
    {
        OverlayManager.Load();
        BuildOverlayPage();

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            bool ok = RegisterHotKey(hwnd, HotkeyId, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x4F); // Strg + Alt + O
            HwndSource.FromHwnd(hwnd)?.AddHook(HotkeyHook);
            if (_overlayHotkeyInfo != null)
                _overlayHotkeyInfo.Text = ok
                    ? "Tastenkürzel: Strg + Alt + O blendet das Overlay ein/aus (auch im Spiel)."
                    : "Das Tastenkürzel Strg + Alt + O ist bereits von einem anderen Programm belegt. Nutze den Button.";
        };
        Closed += (_, _) =>
        {
            try { UnregisterHotKey(new WindowInteropHelper(this).Handle, HotkeyId); } catch { }
            OverlayManager.Shutdown();
        };
        Loaded += (_, _) =>
        {
            if (O.Enabled) OverlayManager.Show();
            UpdateOverlayButton();
        };
    }

    private IntPtr HotkeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            OverlayManager.Toggle();
            UpdateOverlayButton();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void Overlay_Click(object s, RoutedEventArgs e) =>
        ShowPage("Overlay", "Eigenes Ingame-Overlay mit echten Systemwerten.", "", overlay: true);

    private void UpdateOverlayButton()
    {
        if (_overlayToggleButton != null)
            _overlayToggleButton.Content = OverlayManager.IsVisible ? "⏹  Overlay ausschalten" : "▶  Overlay einschalten";
    }

    // ====================== UI der Overlay-Seite (im Code gebaut) ======================

    private Border Card(string title, params UIElement[] children)
    {
        var panel = new StackPanel();
        var t = new TextBlock { Text = title };
        t.Style = (Style)FindResource("SectionTitle");
        panel.Children.Add(t);
        foreach (var c in children) panel.Children.Add(c);
        var b = new Border { Child = panel };
        b.Style = (Style)FindResource("CardStyle");
        return b;
    }

    private TextBlock Muted(string text, double size = 12, Thickness? margin = null)
    {
        var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = size, Margin = margin ?? new Thickness(0, 6, 0, 0) };
        t.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        return t;
    }

    private CheckBox Check(string text, bool value, Action<bool> set)
    {
        var c = new CheckBox { Content = text, IsChecked = value, Margin = new Thickness(0, 4, 18, 4) };
        c.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        void Handler(object? _, RoutedEventArgs __)
        {
            if (!_overlayUiReady) return;
            set(c.IsChecked == true);
            OverlayManager.Refresh(true);
        }
        c.Checked += Handler;
        c.Unchecked += Handler;
        return c;
    }

    private Grid SliderRow(string label, double min, double max, double value, string unit, Action<double> set)
    {
        var g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        var l = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
        var sl = new Slider { Minimum = min, Maximum = max, Value = value };
        Grid.SetColumn(sl, 1);
        var v = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right, Text = $"{value:0} {unit}" };
        v.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        Grid.SetColumn(v, 2);
        sl.ValueChanged += (_, e) =>
        {
            v.Text = $"{e.NewValue:0} {unit}";
            if (!_overlayUiReady) return;
            set(e.NewValue);
            OverlayManager.Refresh(true);
        };
        g.Children.Add(l); g.Children.Add(sl); g.Children.Add(v);
        return g;
    }

    private RadioButton Chip(string text, string group, bool value, Action onChecked)
    {
        var r = new RadioButton { Content = text, GroupName = group, IsChecked = value };
        r.Style = (Style)FindResource("ChipStyle");
        r.Checked += (_, _) => { if (!_overlayUiReady) return; onChecked(); OverlayManager.Refresh(true); };
        return r;
    }

    private void BuildOverlayPage()
    {
        OverlayActions.Children.Clear();

        _overlayToggleButton = new Button { Content = "▶  Overlay einschalten", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(18, 9, 18, 9) };
        _overlayToggleButton.Click += (_, _) => { OverlayManager.Toggle(); UpdateOverlayButton(); };
        _overlayHotkeyInfo = Muted("Tastenkürzel: Strg + Alt + O blendet das Overlay ein/aus (auch im Spiel).", 12, new Thickness(0, 10, 0, 0));

        OverlayActions.Children.Add(Card("OVERLAY",
            _overlayToggleButton,
            _overlayHotkeyInfo,
            Muted("Das Overlay ist ein normales, durchsichtiges Fenster über dem Desktop. Es greift nicht in Fortnite ein (kein Hook, keine Injection) und zeigt nur echte Systemwerte. Damit es über dem Spiel sichtbar ist, stelle Fortnite auf 'Fenster' oder 'Vollbild (Fenster)'. Im exklusiven Vollbild kann Windows es verdecken."),
            Muted("Der Ping ist ein ICMP-Test zum eingestellten Ziel, kein Fortnite-Server-Ping. FPS werden bewusst nicht angezeigt: Ohne Eingriff in das Spiel gibt es dafür keinen verlässlichen Wert. Der Ein/Aus-Zustand wird gespeichert; das Overlay startet dann automatisch mit der App.")));

        var show = new WrapPanel();
        show.Children.Add(Check("Titel", O.ShowTitle, v => O.ShowTitle = v));
        show.Children.Add(Check("Uhrzeit", O.ShowClock, v => O.ShowClock = v));
        show.Children.Add(Check("Ping", O.ShowPing, v => O.ShowPing = v));
        show.Children.Add(Check("CPU-Auslastung", O.ShowCpu, v => O.ShowCpu = v));
        show.Children.Add(Check("RAM", O.ShowRam, v => O.ShowRam = v));
        show.Children.Add(Check("Fortnite-Status", O.ShowGame, v => O.ShowGame = v));
        var layout = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
        layout.Children.Add(Check("Nebeneinander statt untereinander", O.Horizontal, v => O.Horizontal = v));
        layout.Children.Add(Check("Maus klickt durch das Overlay hindurch", O.ClickThrough, v => O.ClickThrough = v));
        OverlayActions.Children.Add(Card("ANZEIGE", show, layout,
            Muted("Ist 'Maus klickt durch' aus, kannst du das Overlay mit der Maus verschieben (Position wird dann 'Frei').")));

        var pos = new WrapPanel();
        pos.Children.Add(Chip("Oben links", "OvPos", O.Corner == OverlayCorner.TopLeft, () => O.Corner = OverlayCorner.TopLeft));
        pos.Children.Add(Chip("Oben rechts", "OvPos", O.Corner == OverlayCorner.TopRight, () => O.Corner = OverlayCorner.TopRight));
        pos.Children.Add(Chip("Unten links", "OvPos", O.Corner == OverlayCorner.BottomLeft, () => O.Corner = OverlayCorner.BottomLeft));
        pos.Children.Add(Chip("Unten rechts", "OvPos", O.Corner == OverlayCorner.BottomRight, () => O.Corner = OverlayCorner.BottomRight));
        pos.Children.Add(Chip("Frei verschiebbar", "OvPos", O.Corner == OverlayCorner.Custom, () => O.Corner = OverlayCorner.Custom));
        OverlayActions.Children.Add(Card("POSITION", pos));

        var host = new TextBox { Text = O.PingHost, FontFamily = new System.Windows.Media.FontFamily("Consolas"), Padding = new Thickness(6, 4, 6, 4), Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
        host.SetResourceReference(Control.BackgroundProperty, "InsetBrush");
        host.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        host.SetResourceReference(Control.BorderBrushProperty, "PanelBorderBrush");
        host.SetResourceReference(TextBoxBase.CaretBrushProperty, "TextBrush");
        void CommitHost() { O.PingHost = string.IsNullOrWhiteSpace(host.Text) ? "1.1.1.1" : host.Text.Trim(); host.Text = O.PingHost; OverlayManager.Refresh(true); }
        host.LostFocus += (_, _) => { if (_overlayUiReady) CommitHost(); };
        host.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter && _overlayUiReady) CommitHost(); };

        OverlayActions.Children.Add(Card("AUSSEHEN",
            SliderRow("Größe", 70, 200, O.Scale * 100, "%", v => O.Scale = v / 100),
            SliderRow("Gesamt-Deckkraft", 30, 100, O.Opacity * 100, "%", v => O.Opacity = v / 100),
            SliderRow("Hintergrund-Deckkraft", 0, 100, O.BackgroundOpacity * 100, "%", v => O.BackgroundOpacity = v / 100),
            Muted("Farben, Schrift und Eckenradius übernimmt das Overlay automatisch aus deinem Design.", 12, new Thickness(0, 8, 0, 0)),
            Muted("Ping-Ziel (IP oder Hostname):", 12, new Thickness(0, 12, 0, 0)),
            host));

        _overlayUiReady = true;
        UpdateOverlayButton();
    }
}
