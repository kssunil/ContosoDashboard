using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentVersion
{
    [Key]
    public int DocumentVersionId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int VersionNumber { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required, MaxLength(255)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    public int UploadedByUserId { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;
}
