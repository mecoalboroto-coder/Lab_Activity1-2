using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace EquipmentBorrowing.Desktop.Converters;

public sealed class BoolToStatusBrushConverter : IValueConverter
{
    public static readonly BoolToStatusBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isError = value is bool b && b;
        return isError ? Brushes.IndianRed : Brushes.SeaGreen;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
