namespace GiftOfTheGivers.Helpers;

/// <summary>
/// Utility methods for summarising donation amounts, used by the Employee dashboard
/// and available for reuse by any future reporting feature (Section E5 in Part 1).
/// </summary>
public static class DonationTotalsHelper
{
    /// <summary>
    /// Sums a set of donation amounts. Returns 0 for an empty/null sequence rather than throwing,
    /// so it is safe to call directly from a view or dashboard aggregate with no defensive checks.
    /// </summary>
    public static decimal CalculateTotal(IEnumerable<decimal>? amounts)
        => amounts is null ? 0m : amounts.Sum();

    /// <summary>
    /// Groups and sums donation amounts by currency code (ZAR / USD / EUR), which is what the
    /// management dashboard (Epic E5) needs since amounts in different currencies cannot be
    /// summed together.
    /// </summary>
    public static IReadOnlyDictionary<string, decimal> CalculateTotalsByCurrency(
        IEnumerable<(string Currency, decimal Amount)> donations)
    {
        return donations
            .GroupBy(d => d.Currency, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key.ToUpperInvariant(), g => g.Sum(d => d.Amount));
    }

    /// <summary>
    /// Formats an amount with its currency code, e.g. "1 250.00 ZAR", using invariant
    /// number formatting so the output is consistent regardless of the server's locale.
    /// </summary>
    public static string FormatAmount(decimal amount, string currencyCode)
        => $"{amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} {currencyCode.ToUpperInvariant()}";
}
