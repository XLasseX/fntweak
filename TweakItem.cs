namespace FortniteTweakTool.Models;
public sealed class TweakItem
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Risk { get; set; } = "Niedrig";
    public string Key { get; set; } = "";
    public bool Selected { get; set; }
    public string Status { get; set; } = "Nicht angewendet";
}
