using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models;

public class Volunteer
{
    public int VolunteerId { get; set; }

    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Skills (e.g. medical, logistics, cooking)")]
    public string? Skills { get; set; }

    [Display(Name = "Availability (e.g. weekends, full-time)")]
    public string? Availability { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
