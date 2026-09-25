using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models;

public class ReliefProject
{
    [Key]
    public int ProjectId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Location { get; set; }

    public string Status { get; set; } = "Active"; // Active / Closed

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public List<ProjectUpdate> Updates { get; set; } = new();
}
