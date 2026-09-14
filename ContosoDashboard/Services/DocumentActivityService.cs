using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public interface IDocumentActivityService
{
    Task<DocumentActivity> AddAsync(int documentId, int actorUserId, string action, string? details = null, CancellationToken cancellationToken = default);
    Task<List<DocumentActivity>> GetAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
}

public sealed class DocumentActivityService : IDocumentActivityService
{
    private readonly ApplicationDbContext _context;

    public DocumentActivityService(ApplicationDbContext context) => _context = context;

    public async Task<DocumentActivity> AddAsync(int documentId, int actorUserId, string action, string? details = null, CancellationToken cancellationToken = default)
    {
        var activity = new DocumentActivity
        {
            DocumentId = documentId,
            ActorUserId = actorUserId,
            Action = action,
            Details = details,
            OccurredDate = DateTime.UtcNow
        };
        _context.DocumentActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async Task<List<DocumentActivity>> GetAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null) return new List<DocumentActivity>();

        var isOwner = document.UploaderId == requestingUserId;
        var isAdministrator = await _context.Users.AsNoTracking().AnyAsync(u => u.UserId == requestingUserId && u.Role == UserRole.Administrator, cancellationToken);
        if (!isOwner && !isAdministrator) return new List<DocumentActivity>();

        return await _context.DocumentActivities.AsNoTracking()
            .Include(a => a.ActorUser)
            .Where(a => a.DocumentId == documentId)
            .OrderByDescending(a => a.OccurredDate)
            .ToListAsync(cancellationToken);
    }
}
