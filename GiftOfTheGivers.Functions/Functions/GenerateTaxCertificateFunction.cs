using System.Globalization;
using System.Text.Json;
using GiftOfTheGivers.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GiftOfTheGivers.Functions;

/// <summary>
/// HTTP-triggered function that stands in for a serverless "issue tax certificate" step.
/// The web app's DonateController calls this after it saves a donation, instead of building
/// the certificate reference number itself (see Section A.1 of the POE Part 2 report).
/// </summary>
public class GenerateTaxCertificateFunction
{
    private readonly ILogger<GenerateTaxCertificateFunction> _logger;

    public GenerateTaxCertificateFunction(ILogger<GenerateTaxCertificateFunction> logger)
    {
        _logger = logger;
    }

    public record GenerateCertificateRequest(
        int DonationId,
        string DonorName,
        decimal Amount,
        string Currency,
        string? ProjectName);

    public record GenerateCertificateResponse(
        string ReferenceNo,
        DateTime IssuedAtUtc,
        string CertificateText);

    [Function("GenerateTaxCertificate")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "GenerateTaxCertificate")] HttpRequest req)
    {
        GenerateCertificateRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<GenerateCertificateRequest>(
                req.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "GenerateTaxCertificate received a malformed request body.");
            return new BadRequestObjectResult(new { error = "Request body must be valid JSON." });
        }

        if (request is null || request.DonationId <= 0 || string.IsNullOrWhiteSpace(request.DonorName))
        {
            return new BadRequestObjectResult(new { error = "donationId and donorName are required." });
        }

        // A certificate is a financial document: a zero or negative amount is never a valid
        // Section 18A deduction, so it is rejected here rather than printed.
        if (request.Amount <= 0m)
        {
            return new BadRequestObjectResult(new { error = "amount must be greater than zero." });
        }

        var issuedAtUtc = DateTime.UtcNow;
        var referenceNo = TaxCertificateHelper.FormatReferenceNumber(request.DonationId, issuedAtUtc);

        // Amount and date are formatted invariantly for the same reason as the reference number:
        // the certificate must read identically no matter which region the Function App runs in.
        // DonationTotalsHelper.FormatAmount is reused so the web app's dashboard totals and the
        // certificate never drift apart in how they present money.
        var certificateText =
            "Gift of the Givers Foundation - Section 18A Tax Certificate (Placeholder)" + Environment.NewLine +
            $"Reference: {referenceNo}" + Environment.NewLine +
            $"Donor: {request.DonorName}" + Environment.NewLine +
            $"Amount: {DonationTotalsHelper.FormatAmount(request.Amount, request.Currency ?? "ZAR")}" + Environment.NewLine +
            $"Project: {request.ProjectName ?? "General fund"}" + Environment.NewLine +
            $"Issued: {issuedAtUtc.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)}";

        _logger.LogInformation(
            "Generated tax certificate {ReferenceNo} for donation {DonationId}.",
            referenceNo, request.DonationId);

        return new OkObjectResult(new GenerateCertificateResponse(referenceNo, issuedAtUtc, certificateText));
    }
}
