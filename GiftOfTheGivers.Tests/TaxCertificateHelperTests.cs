using System.Globalization;
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

    /// <summary>
    /// Regression test for the reference number being built with the calling thread's culture.
    /// th-TH uses the Buddhist calendar, so an uncorrected implementation renders the year of
    /// a 2026 donation as 2569 and produces a completely different reference for the same
    /// donation depending on where the code happens to run.
    /// </summary>
    [Theory]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("de-DE")]
    [InlineData("en-ZA")]
    public void FormatReferenceNumber_IsIdenticalInEveryCulture(string cultureName)
    {
        var issuedAt = new DateTime(2026, 9, 26, 0, 0, 0, DateTimeKind.Utc);
        var original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);

            var reference = TaxCertificateHelper.FormatReferenceNumber(42, issuedAt);

            Assert.Equal("GOTG-TAX-2026-000042", reference);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
