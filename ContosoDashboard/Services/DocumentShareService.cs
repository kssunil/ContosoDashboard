using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed record DocumentShareResult(bool Succeeded, DocumentShare? Share, IReadOnlyList<string> Errors)
{
    public static DocumentShareResult Failure(params string[] errors) => new(false, null, errors);
}

public interface IDocumentShareService
{
    Task<bool> CanAccessAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<DocumentShare>> GetSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentShareResult> ShareAsync(int documentId, int recipientUserId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(int documentShareId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default);
}

public sealed class DocumentShareService : IDocumentShareService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IDocumentActivityService _activityService;

    public DocumentShareService(ApplicationDbContext context, INotificationService notificationService, IDocumentActivityService activityService)
    {
        _context = context;
        _notificationService = notificationService;
        _activityService = activityService;
    }

    public async Task<bool> CanAccessAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.UserId == requestingUserId, cancellationToken);
    }

    public async Task<List<DocumentShare>> GetSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await IsOwnerOrAdministratorAsync(document, requestingUserId, cancellationToken)) return new List<DocumentShare>();
        return await _context.DocumentShares.AsNoTracking().Include(s => s.User).Where(s => s.DocumentId == documentId).ToListAsync(cancellationToken);
    }

    public async Task<DocumentShareResult> ShareAsync(int documentId, int recipientUserId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null) return DocumentShareResult.Failure("The document was not found.");
        if (!await IsOwnerOrAdministratorAsync(document, requestingUserId, cancellationToken)) return DocumentShareResult.Failure("You are not authorized to share this document.");

        if (recipientUserId == document.UploaderId) return DocumentShareResult.Failure("The document owner already has access.");

        var recipient = await _context.Users.FindAsync(new object?[] { recipientUserId }, cancellationToken);
        if (recipient == null) return DocumentShareResult.Failure("The selected user was not found.");

        var alreadyShared = await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.UserId == recipientUserId, cancellationToken);
        if (alreadyShared) return DocumentShareResult.Failure("This document is already shared with the selected user.");

        var share = new DocumentShare
        {
            DocumentId = documentId,
            UserId = recipientUserId,
            SharedByUserId = requestingUserId,
            SharedDate = DateTime.UtcNow
        };
        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync(cancellationToken);

        await _activityService.AddAsync(documentId, requestingUserId, DocumentActivityActions.Share, $"Shared with {recipient.DisplayName}.", cancellationToken);

        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = recipientUserId,
            Title = "Document Shared With You",
            Message = $"{document.Title} was shared with you.",
            Type = NotificationType.DocumentShared,
            Priority = NotificationPriority.Informational,
            RelatedDocumentId = documentId
        });

        return new DocumentShareResult(true, share, Array.Empty<string>());
    }

    public async Task<bool> RevokeAsync(int documentShareId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var share = await _context.DocumentShares.Include(s => s.Document).FirstOrDefaultAsync(s => s.DocumentShareId == documentShareId, cancellationToken);
        if (share == null) return false;
        if (!await IsOwnerOrAdministratorAsync(share.Document, requestingUserId, cancellationToken)) return false;

        _context.DocumentShares.Remove(share);
        await _context.SaveChangesAsync(cancellationToken);
        await _activityService.AddAsync(share.DocumentId, requestingUserId, DocumentActivityActions.ShareRevoked, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<List<Document>> GetSharedWithMeAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.AsNoTracking()
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Include(d => d.Tags)
            .Where(d => !d.IsDeleted && d.Shares.Any(s => s.UserId == requestingUserId))
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> IsOwnerOrAdministratorAsync(Document document, int requestingUserId, CancellationToken cancellationToken)
    {
        if (document.UploaderId == requestingUserId) return true;
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == requestingUserId, cancellationToken);
        return user?.Role == UserRole.Administrator;
    }
}
