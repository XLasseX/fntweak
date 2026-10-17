using System.Text.Json;
using System.Text.Json.Serialization;

namespace FortniteTweakTool.Models;

public enum OverlayCorner { TopLeft, TopRight, BottomLeft, BottomRight, Custom }

/// <summary>Einstellungen des Ingame-Overlays. Wird als overlay.json gespeichert.</summary>
public sealed class OverlaySettings
{
    public bool Enabled { get; set; } = false;            // beim Start automatisch zeigen
    public bool ClickThrough { get; set; } = true;        // Mausklicks gehen durch das Overlay hindurch
    public OverlayCorner Corner { get; set; } = OverlayCorner.TopLeft;
    public double Left { get; set; } = 40;                // nur bei Corner = Custom
    public double Top { get; set; } = 40;

    public bool ShowTitle { get; set; } = true;
    public bool ShowClock { get; set; } = true;
    public bool ShowPing { get; set; } = true;
    public bool ShowCpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;
    public bool ShowGame { get; set; } = true;
    public bool Horizontal { get; set; } = false;

    public double Scale { get; set; } = 1.0;              // 0.7..2
    public double Opacity { get; set; } = 1.0;            // 0.3..1 (alles)
    public double BackgroundOpacity { get; set; } = 0.65; // 0..1 (Panel-Hintergrund)
    public string PingHost { get; set; } = "1.1.1.1";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
