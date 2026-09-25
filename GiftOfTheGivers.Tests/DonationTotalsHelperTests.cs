using GiftOfTheGivers.Helpers;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class DonationTotalsHelperTests
{
    [Fact]
    public void CalculateTotal_SumsAllAmounts()
    {
        var total = DonationTotalsHelper.CalculateTotal(new[] { 100m, 250.50m, 49.50m });

        Assert.Equal(400m, total);
    }

    [Fact]
    public void CalculateTotal_ReturnsZeroForNull()
    {
        Assert.Equal(0m, DonationTotalsHelper.CalculateTotal(null));
    }

    [Fact]
    public void CalculateTotalsByCurrency_GroupsCaseInsensitively()
    {
        var donations = new[]
        {
            ("ZAR", 100m),
            ("zar", 50m),
            ("USD", 20m)
        };

        var totals = DonationTotalsHelper.CalculateTotalsByCurrency(donations);

        Assert.Equal(150m, totals["ZAR"]);
        Assert.Equal(20m, totals["USD"]);
    }

    [Fact]
    public void FormatAmount_UsesTwoDecimalPlacesAndUppercaseCurrency()
    {
        var formatted = DonationTotalsHelper.FormatAmount(1250, "zar");

        Assert.Equal("1,250.00 ZAR", formatted);
    }
}
