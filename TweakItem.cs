using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FortniteTweakTool.Models;

public sealed class TweakItem : INotifyPropertyChanged
{
    private string _status = "Nicht angewendet";

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Risk { get; set; } = "Niedrig";
    public string Key { get; set; } = "";
    public string Category { get; set; } = "fps";
    public bool CanUndo { get; set; }
    public bool Selected { get; set; }

    public Visibility UndoVisibility => CanUndo ? Visibility.Visible : Visibility.Collapsed;

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
