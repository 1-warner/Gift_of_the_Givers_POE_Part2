using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models;

public class Donation
{
    public int DonationId { get; set; }

    // Null when the donation is made by an anonymous guest
    public string? DonorUserId { get; set; }
    public string? DonorName { get; set; }

    public int? ProjectId { get; set; }
    public ReliefProject? Project { get; set; }

    [Range(1, 10000000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "ZAR";   // ZAR / USD / EUR

    [Required]
    public string Frequency { get; set; } = "OneTime"; // OneTime / Recurring

    public DateTime DonatedAt { get; set; } = DateTime.UtcNow;

    // Placeholder tax-certificate reference generated on donation
    public string ReferenceNo { get; set; } = string.Empty;
}
