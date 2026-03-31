using System;
using Microsoft.UI.Xaml.Data;

namespace VibeCoders.Converters;

/// <summary>
/// Inverts a boolean value. Used to disable inputs when a set is completed.
/// </summary>
public sealed class BoolNegateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : value;
}