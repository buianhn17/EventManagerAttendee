using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EventManagerAttendee.ViewModels;

/// <summary>
/// Base class for objects the UI binds to (MVVM pattern).
/// When a property changes, PropertyChanged tells the bound controls to redraw.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Notifies the UI that a property has a new value.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Sets the field and notifies the UI, but only if the value really changed.
    /// Returns true when it changed.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
