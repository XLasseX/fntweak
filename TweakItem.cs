using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FortniteTweakTool.Models;

public sealed class TweakItem : INotifyPropertyChanged
{
    private string _status = "Nicht angewendet";

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Risk { get; set; } = "Niedrig";
    public string Key { get; set; } = "";
    public bool Selected { get; set; }

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
