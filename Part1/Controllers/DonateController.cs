using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiftOfTheGiversApp.Data;
using GiftOfTheGiversApp.Models;
using GiftOfTheGivers.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GiftOfTheGiversApp.Controllers;

public class DonateController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DonateController> _logger;

    public DonateController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IHttpClientFactory httpClientFactory,
        ILogger<DonateController> logger)
    {
        _db = db;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Projects = await _db.ReliefProjects.Where(p => p.Status == "Active").ToListAsync();
        return View(new Donation { Amount = 100 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Donation model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Projects = await _db.ReliefProjects.Where(p => p.Status == "Active").ToListAsync();
            return View(model);
        }

        // Link to the signed-in donor if there is one; otherwise it is an anonymous guest donation.
        if (User.Identity?.IsAuthenticated == true)
        {
            model.DonorUserId = _userManager.GetUserId(User);
            model.DonorName = User.Identity.Name;
        }
        else
        {
            model.DonorName = string.IsNullOrWhiteSpace(model.DonorName) ? "Anonymous Guest" : model.DonorName;
        }

        model.DonatedAt = DateTime.UtcNow;

        // Save first so EF Core assigns the identity DonationId - the reference number format
        // (Section D) is built from that id, so it has to come after the first save.
        _db.Donations.Add(model);
        await _db.SaveChangesAsync();

        // D.2 evidence: the reference number is produced by the GiftOfTheGivers.Helpers NuGet
        // package (published from Azure Artifacts in Section D.1), not by logic inlined here.
        model.ReferenceNo = TaxCertificateHelper.FormatReferenceNumber(model.DonationId, model.DonatedAt);
        await _db.SaveChangesAsync();

        // A.1 evidence: the same donation is also posted to the GenerateTaxCertificate Azure
        // Function so a serverless step "generates" the certificate independently of the web
        // app process. This is best-effort - if the Function isn't running/deployed, the
        // donation still succeeds using the reference number computed above.
        await TryNotifyGenerateTaxCertificateFunctionAsync(model);

        return RedirectToAction(nameof(Certificate), new { id = model.DonationId });
    }

    private async Task TryNotifyGenerateTaxCertificateFunctionAsync(Donation model)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("FunctionsClient");

            var payload = JsonSerializer.Serialize(new
            {
                donationId = model.DonationId,
                donorName = model.DonorName,
                amount = model.Amount,
                currency = model.Currency,
                projectName = model.Project?.Name
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("GenerateTaxCertificate", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "GenerateTaxCertificate function returned {StatusCode} for donation {DonationId}.",
                    response.StatusCode, model.DonationId);
            }
        }
        catch (Exception ex)
        {
            // The Function is a companion/serverless step, not on the critical path for the
            // donation itself, so a failure here is logged and swallowed rather than shown to
            // the donor.
            _logger.LogWarning(ex, "Could not reach the GenerateTaxCertificate function for donation {DonationId}.", model.DonationId);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Certificate(int id)
    {
        var donation = await _db.Donations.Include(d => d.Project).FirstOrDefaultAsync(d => d.DonationId == id);
        if (donation == null) return NotFound();
        return View(donation);
    }

    [HttpGet]
    public async Task<IActionResult> CertificatePdf(int id)
    {
        var donation = await _db.Donations.Include(d => d.Project).FirstOrDefaultAsync(d => d.DonationId == id);
        if (donation == null) return NotFound();

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(12).FontColor(Colors.Grey.Darken3));

                page.Header().Column(col =>
                {
                    col.Item().Text("Gift of the Givers Foundation").FontSize(20).Bold().FontColor("#C0392B");
                    col.Item().Text("Section 18A Tax Certificate (Placeholder — prototype)").FontSize(11).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#C0392B");
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("This certifies receipt of the following donation:").Bold();
                    col.Item().Text($"Certificate reference: {donation.ReferenceNo}");
                    col.Item().Text($"Donor: {donation.DonorName}");
                    col.Item().Text($"Amount: {donation.Amount:0.00} {donation.Currency}");
                    col.Item().Text($"Type: {donation.Frequency}");
                    col.Item().Text($"Project: {(donation.Project != null ? donation.Project.Name : "General fund")}");
                    col.Item().Text($"Date: {donation.DonatedAt:dd MMMM yyyy}");
                    col.Item().PaddingTop(20).Text("Thank you for supporting disaster relief across South Africa and beyond.")
                        .Italic().FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Gift of the Givers — Portfolio of Evidence prototype. ");
                    t.Span("Values are symbolic and not valid for SARS purposes.").FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"TaxCertificate-{donation.ReferenceNo}.pdf");
    }
}
