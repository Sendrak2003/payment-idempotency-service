using System.Globalization;
using System.Text.RegularExpressions;

namespace WalletApi.Api.Contracts;

public static partial class AmountValidator
{
    [GeneratedRegex(@"^\d+(\.\d{1,2})?$")]
    private static partial Regex Pattern();

    public static bool TryParse(string? raw, out decimal amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(raw) || !Pattern().IsMatch(raw))
        {
            return false;
        }

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount))
        {
            return false;
        }

        return amount > 0;
    }
}
