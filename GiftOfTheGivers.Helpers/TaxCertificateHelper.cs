using System.Globalization;

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
    /// <remarks>
    /// The reference is built with <see cref="CultureInfo.InvariantCulture"/> on purpose. String
    /// interpolation uses the calling thread's culture by default, and two parts of this format
    /// are culture-sensitive: "yyyy" is resolved against the culture's own calendar (a Thai or
    /// Saudi locale would render 2026 as 2569 or 1447), and "D6" is rendered with the culture's
    /// digits (Arabic-Indic digits under ar-SA). Either would produce a different reference for
    /// the same donation depending on the server's regional settings, which for a value carrying
    /// a UNIQUE constraint — and printed on a SARS Section 18A certificate — is a correctness
    /// problem rather than a cosmetic one.
    /// </remarks>
    public static string FormatReferenceNumber(int donationId, DateTime issuedAtUtc)
    {
        if (donationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(donationId), "donationId must be a positive, saved donation id.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"GOTG-TAX-{issuedAtUtc:yyyy}-{donationId:D6}");
    }
}
