using System.Globalization;

namespace MeuDinDin.Components.Shared;

internal static class MoneyMask
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public static string FormatTyping(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        var sanitized = Sanitize(rawValue);
        var hasComma = sanitized.Contains(',');
        var parts = sanitized.Split(',', 2);
        var integerDigits = DigitsOnly(parts[0]);
        var decimalDigits = hasComma && parts.Length > 1
            ? DigitsOnly(parts[1]).Substring(0, Math.Min(2, DigitsOnly(parts[1]).Length))
            : string.Empty;

        var formattedInteger = FormatInteger(integerDigits);

        if (!hasComma)
        {
            return formattedInteger;
        }

        return $"{formattedInteger},{decimalDigits}";
    }

    public static string FormatCompleted(string? rawValue)
    {
        if (!TryParse(rawValue, out var value))
        {
            return "0,00";
        }

        return FormatValue(value);
    }

    public static string FormatNullableCompleted(string? rawValue)
    {
        return TryParseNullable(rawValue, out var value) && value.HasValue
            ? FormatValue(value.Value)
            : string.Empty;
    }

    public static string FormatValue(decimal value) => value.ToString("N2", Culture);

    public static bool TryParse(string? rawValue, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        return decimal.TryParse(NormalizeForParsing(rawValue), NumberStyles.Number, Culture, out value);
    }

    public static bool TryParseNullable(string? rawValue, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            return true;
        }

        if (TryParse(rawValue, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }

    private static string NormalizeForParsing(string rawValue)
    {
        var masked = FormatTyping(rawValue);

        if (string.IsNullOrWhiteSpace(masked))
        {
            return "0,00";
        }

        if (!masked.Contains(','))
        {
            return $"{masked},00";
        }

        var parts = masked.Split(',', 2);
        var decimalDigits = parts.Length > 1 ? parts[1] : string.Empty;

        if (decimalDigits.Length == 0)
        {
            decimalDigits = "00";
        }
        else if (decimalDigits.Length == 1)
        {
            decimalDigits += "0";
        }

        return $"{parts[0]},{decimalDigits}";
    }

    private static string Sanitize(string rawValue)
    {
        var cleaned = rawValue
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty);

        var chars = cleaned
            .Where(ch => char.IsDigit(ch) || ch == ',')
            .ToArray();

        var sanitized = new string(chars);
        var firstComma = sanitized.IndexOf(',');

        if (firstComma < 0)
        {
            return sanitized;
        }

        var beforeComma = sanitized[..firstComma];
        var afterComma = sanitized[(firstComma + 1)..].Replace(",", string.Empty, StringComparison.Ordinal);
        return $"{beforeComma},{afterComma}";
    }

    private static string DigitsOnly(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string FormatInteger(string digits)
    {
        var normalized = string.IsNullOrWhiteSpace(digits) ? "0" : digits;
        var value = decimal.Parse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture);
        return value.ToString("#,0", Culture);
    }
}
