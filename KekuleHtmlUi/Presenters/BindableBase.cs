// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Tim
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KekuleHtmlUi.Presenters;

/// <summary>
/// Base class for presenters whose properties are bound in XAML.
/// </summary>
public abstract class BindableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Assigns <paramref name="value"/> to <paramref name="storage"/> and raises <see cref="PropertyChanged"/>, but only if the value actually changes.
    /// </summary>
    /// <returns>Whether the value has changed.</returns>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        RaisePropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for the given property.
    /// </summary>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
