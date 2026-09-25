using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models;

public class ProjectUpdate
{
    [Key]
    public int UpdateId { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public ReliefProject? Project { get; set; }

    public string? AuthorName { get; set; }

    [Required, Display(Name = "Update")]
    public string Body { get; set; } = string.Empty;

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
}
