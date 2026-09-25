using GiftOfTheGivers.Helpers;
using Xunit;

namespace GiftOfTheGivers.Tests;

public class TaxCertificateHelperTests
{
    [Fact]
    public void FormatReferenceNumber_PadsDonationIdToSixDigits()
    {
        var issuedAt = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc);

        var reference = TaxCertificateHelper.FormatReferenceNumber(42, issuedAt);

        Assert.Equal("GOTG-TAX-2026-000042", reference);
    }

    [Fact]
    public void FormatReferenceNumber_UsesTheIssuedYear()
    {
        var issuedAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var reference = TaxCertificateHelper.FormatReferenceNumber(7, issuedAt);

        Assert.StartsWith("GOTG-TAX-2027-", reference);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void FormatReferenceNumber_RejectsNonPositiveDonationId(int donationId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TaxCertificateHelper.FormatReferenceNumber(donationId, DateTime.UtcNow));
    }
}
