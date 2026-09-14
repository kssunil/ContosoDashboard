using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public interface IDocumentShareService
{
    Task<bool> CanAccessAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<DocumentShare>> GetSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
}

public sealed class DocumentShareService : IDocumentShareService
{
    private readonly ApplicationDbContext _context;

    public DocumentShareService(ApplicationDbContext context) => _context = context;

    public async Task<bool> CanAccessAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.UserId == requestingUserId, cancellationToken);
    }

    public async Task<List<DocumentShare>> GetSharesAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || document.UploaderId != requestingUserId) return new List<DocumentShare>();
        return await _context.DocumentShares.AsNoTracking().Include(s => s.User).Where(s => s.DocumentId == documentId).ToListAsync(cancellationToken);
    }
}
