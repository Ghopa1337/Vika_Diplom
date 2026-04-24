using System.Text.RegularExpressions;

namespace CargoTransport.Desktop;

internal static partial class InputValidationHelper
{
    private const decimal DefaultCostPerKilometer = 75m;

    public static string KeepDigitsOnly(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    public static string NormalizeEmailInput(string? value) =>
        value?.Trim() ?? string.Empty;

    public static string? NormalizeOptionalEmail(string? value)
    {
        string normalized = NormalizeEmailInput(value);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string? NormalizeOptionalPhone(string? value)
    {
        string digits = KeepDigitsOnly(value);
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    public static bool IsValidAsciiEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && EmailRegex().IsMatch(value.Trim());

    public static decimal CalculateDeliveryCost(decimal distanceKm) =>
        Math.Round(distanceKm * DefaultCostPerKilometer, 2, MidpointRounding.AwayFromZero);

    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
