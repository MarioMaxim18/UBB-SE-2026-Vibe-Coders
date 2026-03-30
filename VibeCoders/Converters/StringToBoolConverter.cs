using System;
using Microsoft.UI.Xaml.Data;

namespace VibeCoders.Converters;

/// <summary>
/// Returns true when the string is non-null and non-empty.
/// Used to show the ErrorMessage InfoBar only when there is an error.
/// </summary>
public sealed class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is string s && !string.IsNullOrEmpty(s);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}