using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class ProjectActivity
{
    [Key]
    public int ProjectActivityId { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Details { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;
}

public static class ProjectActivityActions
{
    public const string ProjectCreated = "ProjectCreated";
}
