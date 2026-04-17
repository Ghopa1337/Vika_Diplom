using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace CargoTransport.Desktop.ViewModels;

public sealed class BooleanToVisibilityConverter : MarkupExtension, IValueConverter
{
    private static BooleanToVisibilityConverter? _instance;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _instance ??= new BooleanToVisibilityConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility visibility && visibility == Visibility.Visible;
}

public sealed class InverseBooleanToVisibilityConverter : MarkupExtension, IValueConverter
{
    private static InverseBooleanToVisibilityConverter? _instance;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _instance ??= new InverseBooleanToVisibilityConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility visibility && visibility != Visibility.Visible;
}

public sealed class NullToVisibilityConverter : MarkupExtension, IValueConverter
{
    private static NullToVisibilityConverter? _instance;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _instance ??= new NullToVisibilityConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class StepToColorConverter : MarkupExtension, IValueConverter
{
    private static readonly Brush ActiveBrush = new SolidColorBrush(Color.FromRgb(0x0F, 0x5C, 0x69));
    private static readonly Brush InactiveBrush = Brushes.White;
    private static StepToColorConverter? _instance;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _instance ??= new StepToColorConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryParseInt(value, out int currentStep) || !TryParseInt(parameter, out int targetStep))
        {
            return InactiveBrush;
        }

        return currentStep >= targetStep ? ActiveBrush : InactiveBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static bool TryParseInt(object? value, out int result)
    {
        result = 0;
        return value switch
        {
            int intValue => (result = intValue) >= 0,
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => (result = parsed) >= 0,
            _ => false
        };
    }
}

public sealed class StepToVisibilityConverter : MarkupExtension, IValueConverter
{
    private static StepToVisibilityConverter? _instance;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        _instance ??= new StepToVisibilityConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!TryParseInt(value, out int currentStep))
        {
            return Visibility.Collapsed;
        }

        if (parameter is string text && text.Equals("NotLast", StringComparison.OrdinalIgnoreCase))
        {
            return currentStep < 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        return TryParseInt(parameter, out int targetStep) && currentStep == targetStep
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static bool TryParseInt(object? value, out int result)
    {
        result = 0;
        return value switch
        {
            int intValue => (result = intValue) >= 0,
            string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => (result = parsed) >= 0,
            _ => false
        };
    }
}
