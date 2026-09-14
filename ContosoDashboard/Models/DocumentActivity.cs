using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Details { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;
}

public static class DocumentActivityActions
{
    public const string Upload = "Upload";
    public const string ProjectDocumentAdded = "ProjectDocumentAdded";
    public const string Download = "Download";
    public const string Preview = "Preview";
    public const string MetadataEdit = "MetadataEdit";
    public const string Replacement = "Replacement";
    public const string Share = "Share";
    public const string ShareRevoked = "ShareRevoked";
    public const string Delete = "Delete";
    public const string ScanRejected = "ScanRejected";
}
