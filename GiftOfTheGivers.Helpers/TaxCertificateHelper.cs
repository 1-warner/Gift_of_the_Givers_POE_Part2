namespace GiftOfTheGivers.Helpers;

/// <summary>
/// Formatting utilities for donation tax-certificate reference numbers.
/// Extracted from GiftOfTheGiversApp.Controllers.DonateController so the same
/// formatting rule can be shared by the web app and the Azure Function project
/// (see GiftOfTheGivers.Functions.GenerateTaxCertificateFunction).
/// </summary>
public static class TaxCertificateHelper
{
    /// <summary>
    /// Builds a certificate reference number in the form GOTG-TAX-{year}-{donationId:000000}.
    /// Using the donation id (instead of a random number) keeps the reference deterministic
    /// and unique per donation, which matches the UNIQUE constraint on
    /// TaxCertificates.ReferenceNo in the Part 1 schema.
    /// </summary>
    public static string FormatReferenceNumber(int donationId, DateTime issuedAtUtc)
    {
        if (donationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(donationId), "donationId must be a positive, saved donation id.");
        }

        return $"GOTG-TAX-{issuedAtUtc:yyyy}-{donationId:D6}";
    }
}
