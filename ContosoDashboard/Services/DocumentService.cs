using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentUploadRequest
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Category { get; init; }
    public int? ProjectId { get; init; }
    public int? TaskId { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record DocumentUploadResult(bool Succeeded, Document? Document, IReadOnlyList<string> Errors)
{
    public static DocumentUploadResult Failure(params string[] errors) => new(false, null, errors);
}

public enum DocumentSortField
{
    Title,
    UploadedDate,
    Category,
    FileSize
}

public sealed class DocumentSearchRequest
{
    public string? SearchText { get; init; }
    public string? Category { get; init; }
    public int? ProjectId { get; init; }
    public DateTime? UploadedFrom { get; init; }
    public DateTime? UploadedTo { get; init; }
    public long? MinFileSize { get; init; }
    public long? MaxFileSize { get; init; }
    public DocumentSortField SortBy { get; init; } = DocumentSortField.UploadedDate;
    public bool SortDescending { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record DocumentSearchResult(IReadOnlyList<Document> Items, int TotalCount, int Page, int PageSize);

public sealed record DocumentMetadataUpdate(string Title, string? Description, string Category, IReadOnlyCollection<string> Tags);

public sealed record DocumentAuditReport(
    int TotalDocuments,
    int TotalActiveUploaders,
    IReadOnlyDictionary<string, int> DocumentsByCategory,
    IReadOnlyDictionary<string, int> ActivityCountsByAction,
    IReadOnlyList<DocumentActivity> RecentActivity);

public interface IDocumentService
{
    Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, CancellationToken cancellationToken = default);
    Task<List<Document>> GetRecentUploadsAsync(int requestingUserId, int count = 5, CancellationToken cancellationToken = default);
    Task<DocumentSearchResult> SearchAsync(DocumentSearchRequest request, int requestingUserId, CancellationToken cancellationToken = default);
    Task<Document?> GetDocumentForAccessAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<Stream?> OpenDocumentContentAsync(int documentId, int requestingUserId, string activityAction, CancellationToken cancellationToken = default);
    Task<DocumentUploadResult> EditMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentUploadResult> ReplaceFileAsync(int documentId, Stream content, string fileName, string contentType, long length, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentUploadResult> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentAuditReport?> GetAuditReportAsync(int requestingUserId, CancellationToken cancellationToken = default);
}

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileSafetyScanner _scanner;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IFileSafetyScanner scanner,
        INotificationService notificationService,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _scanner = scanner;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var safety = await _scanner.ValidateAsync(request.FileName, request.ContentType, request.Length, request.Title, request.Category, cancellationToken);
        if (!safety.IsSafe)
        {
            _logger.LogWarning("Upload rejected for user {UserId}, file {FileName}: {Errors}", requestingUserId, request.FileName, string.Join("; ", safety.Errors));
            return DocumentUploadResult.Failure(safety.Errors.ToArray());
        }

        Project? project = null;
        TaskItem? task = null;
        if (request.ProjectId.HasValue)
        {
            project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId.Value, cancellationToken);
            if (project == null) return DocumentUploadResult.Failure("The selected project was not found.");

            var isProjectMember = project.ProjectManagerId == requestingUserId || project.ProjectMembers.Any(m => m.UserId == requestingUserId);
            if (!isProjectMember) return DocumentUploadResult.Failure("You are not a member of the selected project.");
        }

        if (request.TaskId.HasValue)
        {
            task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == request.TaskId.Value, cancellationToken);
            if (task == null) return DocumentUploadResult.Failure("The selected task was not found.");
            if (request.ProjectId != task.ProjectId) return DocumentUploadResult.Failure("A task document must use the task's project.");
        }

        var relativePath = _fileStorage.CreateRelativePath(requestingUserId, request.ProjectId, Path.GetExtension(request.FileName));
        try
        {
            await _fileStorage.SaveAsync(relativePath, request.Content, cancellationToken);

            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Category = request.Category,
                FilePath = relativePath,
                OriginalFileName = Path.GetFileName(request.FileName),
                FileSize = request.Length,
                MimeType = request.ContentType,
                UploaderId = requestingUserId,
                ProjectId = request.ProjectId,
                TaskId = request.TaskId,
                UploadedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            foreach (var tag in request.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
                document.Tags.Add(new DocumentTag { Tag = tag });

            document.Activities.Add(new DocumentActivity
            {
                ActorUserId = requestingUserId,
                Action = DocumentActivityActions.Upload,
                Details = "Document uploaded."
            });

            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);

            if (project != null)
            {
                var recipientIds = project.ProjectMembers.Select(m => m.UserId)
                    .Append(project.ProjectManagerId)
                    .Where(id => id != requestingUserId)
                    .Distinct();
                foreach (var recipientId in recipientIds)
                {
                    await _notificationService.CreateNotificationAsync(new Notification
                    {
                        UserId = recipientId,
                        Title = "New Project Document",
                        Message = $"{request.Title} was added to {project.Name}.",
                        Type = NotificationType.DocumentAdded,
                        Priority = NotificationPriority.Informational
                    });
                }
            }

            return new DocumentUploadResult(true, document, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document upload failed for user {UserId}.", requestingUserId);
            try
            {
                await _fileStorage.DeleteAsync(relativePath, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Document cleanup failed for {RelativePath}.", relativePath);
            }

            return DocumentUploadResult.Failure("The document could not be stored. No document record was created.");
        }
    }

    public async Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, CancellationToken cancellationToken = default)
    {
        var query = BuildAccessibleQuery(requestingUserId);
        if (projectId.HasValue) query = query.Where(d => d.ProjectId == projectId.Value);
        return await query.OrderByDescending(d => d.UploadedDate).ToListAsync(cancellationToken);
    }

    public async Task<List<Document>> GetRecentUploadsAsync(int requestingUserId, int count = 5, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.AsNoTracking()
            .Include(d => d.Project)
            .Where(d => !d.IsDeleted && d.UploaderId == requestingUserId)
            .OrderByDescending(d => d.UploadedDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentSearchResult> SearchAsync(DocumentSearchRequest request, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var query = BuildAccessibleQuery(requestingUserId);

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var text = request.SearchText.Trim();
            query = query.Where(d =>
                d.Title.Contains(text) ||
                (d.Description != null && d.Description.Contains(text)) ||
                d.Tags.Any(t => t.Tag.Contains(text)) ||
                d.Uploader.DisplayName.Contains(text) ||
                (d.Project != null && d.Project.Name.Contains(text)));
        }

        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(d => d.Category == request.Category);
        if (request.ProjectId.HasValue) query = query.Where(d => d.ProjectId == request.ProjectId.Value);
        if (request.UploadedFrom.HasValue) query = query.Where(d => d.UploadedDate >= request.UploadedFrom.Value);
        if (request.UploadedTo.HasValue) query = query.Where(d => d.UploadedDate <= request.UploadedTo.Value);
        if (request.MinFileSize.HasValue) query = query.Where(d => d.FileSize >= request.MinFileSize.Value);
        if (request.MaxFileSize.HasValue) query = query.Where(d => d.FileSize <= request.MaxFileSize.Value);

        query = (request.SortBy, request.SortDescending) switch
        {
            (DocumentSortField.Title, false) => query.OrderBy(d => d.Title),
            (DocumentSortField.Title, true) => query.OrderByDescending(d => d.Title),
            (DocumentSortField.Category, false) => query.OrderBy(d => d.Category),
            (DocumentSortField.Category, true) => query.OrderByDescending(d => d.Category),
            (DocumentSortField.FileSize, false) => query.OrderBy(d => d.FileSize),
            (DocumentSortField.FileSize, true) => query.OrderByDescending(d => d.FileSize),
            (DocumentSortField.UploadedDate, false) => query.OrderBy(d => d.UploadedDate),
            _ => query.OrderByDescending(d => d.UploadedDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new DocumentSearchResult(items, totalCount, page, pageSize);
    }

    public async Task<Document?> GetDocumentForAccessAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .Include(d => d.Uploader)
            .Include(d => d.Project).ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.Tags)
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null) return null;
        if (await CanAccessAsync(document, requestingUserId, cancellationToken)) return document;
        _logger.LogWarning("Unauthorized access attempt on document {DocumentId} by user {UserId}.", documentId, requestingUserId);
        return null;
    }

    public async Task<Stream?> OpenDocumentContentAsync(int documentId, int requestingUserId, string activityAction, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentForAccessAsync(documentId, requestingUserId, cancellationToken);
        if (document == null) return null;

        var stream = await _fileStorage.OpenReadAsync(document.FilePath, cancellationToken);
        if (stream == null) return null;

        document.Activities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = requestingUserId,
            Action = activityAction
        });
        await _context.SaveChangesAsync(cancellationToken);
        return stream;
    }

    public async Task<DocumentUploadResult> EditMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.Tags).FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null) return DocumentUploadResult.Failure("The document was not found.");
        if (!await CanManageAsync(document, requestingUserId, cancellationToken))
        {
            _logger.LogWarning("Unauthorized metadata edit attempt on document {DocumentId} by user {UserId}.", documentId, requestingUserId);
            return DocumentUploadResult.Failure("You are not authorized to edit this document.");
        }

        if (string.IsNullOrWhiteSpace(update.Title) || update.Title.Length > 255)
            return DocumentUploadResult.Failure("A document title is required and must be 255 characters or fewer.");
        if (!DocumentCategories.All.Contains(update.Category))
            return DocumentUploadResult.Failure("Select a valid document category.");

        document.Title = update.Title.Trim();
        document.Description = string.IsNullOrWhiteSpace(update.Description) ? null : update.Description.Trim();
        document.Category = update.Category;
        document.UpdatedDate = DateTime.UtcNow;

        document.Tags.Clear();
        foreach (var tag in update.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            document.Tags.Add(new DocumentTag { Tag = tag });

        document.Activities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = requestingUserId,
            Action = DocumentActivityActions.MetadataEdit
        });

        await _context.SaveChangesAsync(cancellationToken);
        return new DocumentUploadResult(true, document, Array.Empty<string>());
    }

    public async Task<DocumentUploadResult> ReplaceFileAsync(int documentId, Stream content, string fileName, string contentType, long length, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.Include(d => d.Versions).FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
        if (document == null) return DocumentUploadResult.Failure("The document was not found.");
        if (!await CanManageAsync(document, requestingUserId, cancellationToken))
        {
            _logger.LogWarning("Unauthorized file replacement attempt on document {DocumentId} by user {UserId}.", documentId, requestingUserId);
            return DocumentUploadResult.Failure("You are not authorized to replace this document's file.");
        }

        var safety = await _scanner.ValidateAsync(fileName, contentType, length, document.Title, document.Category, cancellationToken);
        if (!safety.IsSafe) return DocumentUploadResult.Failure(safety.Errors.ToArray());

        var relativePath = _fileStorage.CreateRelativePath(document.UploaderId, document.ProjectId, Path.GetExtension(fileName));
        try
        {
            await _fileStorage.SaveAsync(relativePath, content, cancellationToken);

            var nextVersionNumber = (document.Versions.Count == 0 ? 0 : document.Versions.Max(v => v.VersionNumber)) + 1;
            document.Versions.Add(new DocumentVersion
            {
                DocumentId = document.DocumentId,
                VersionNumber = nextVersionNumber,
                FilePath = document.FilePath,
                OriginalFileName = document.OriginalFileName,
                FileSize = document.FileSize,
                MimeType = document.MimeType,
                UploadedByUserId = document.UploaderId,
                UploadedDate = document.UpdatedDate
            });

            document.FilePath = relativePath;
            document.OriginalFileName = Path.GetFileName(fileName);
            document.FileSize = length;
            document.MimeType = contentType;
            document.UpdatedDate = DateTime.UtcNow;

            document.Activities.Add(new DocumentActivity
            {
                DocumentId = document.DocumentId,
                ActorUserId = requestingUserId,
                Action = DocumentActivityActions.Replacement
            });

            await _context.SaveChangesAsync(cancellationToken);
            return new DocumentUploadResult(true, document, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document replacement failed for document {DocumentId}.", documentId);
            try
            {
                await _fileStorage.DeleteAsync(relativePath, cancellationToken);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Replacement cleanup failed for {RelativePath}.", relativePath);
            }
            return DocumentUploadResult.Failure("The replacement file could not be stored. The document was not changed.");
        }
    }

    public async Task<DocumentUploadResult> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents
            .Include(d => d.Project).ThenInclude(p => p!.ProjectMembers)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || document.IsDeleted) return DocumentUploadResult.Failure("The document was not found.");
        if (!await CanDeleteAsync(document, requestingUserId, cancellationToken))
        {
            _logger.LogWarning("Unauthorized delete attempt on document {DocumentId} by user {UserId}.", documentId, requestingUserId);
            return DocumentUploadResult.Failure("You are not authorized to delete this document.");
        }

        try
        {
            await _fileStorage.DeleteAsync(document.FilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File removal failed while deleting document {DocumentId}.", documentId);
            return DocumentUploadResult.Failure("The file could not be removed from storage. The document was not deleted.");
        }

        document.IsDeleted = true;
        document.UpdatedDate = DateTime.UtcNow;
        document.Activities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = requestingUserId,
            Action = DocumentActivityActions.Delete
        });

        await _context.SaveChangesAsync(cancellationToken);
        return new DocumentUploadResult(true, document, Array.Empty<string>());
    }

    public async Task<DocumentAuditReport?> GetAuditReportAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        var requestingUser = await _context.Users.FindAsync(new object?[] { requestingUserId }, cancellationToken);
        if (requestingUser?.Role != UserRole.Administrator)
        {
            _logger.LogWarning("Unauthorized audit report access attempt by user {UserId}.", requestingUserId);
            return null;
        }

        var totalDocuments = await _context.Documents.CountAsync(d => !d.IsDeleted, cancellationToken);
        var totalActiveUploaders = await _context.Documents.Where(d => !d.IsDeleted).Select(d => d.UploaderId).Distinct().CountAsync(cancellationToken);
        var byCategory = await _context.Documents.Where(d => !d.IsDeleted).GroupBy(d => d.Category)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var byAction = await _context.DocumentActivities.GroupBy(a => a.Action)
            .Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken);
        var recentActivity = await _context.DocumentActivities.AsNoTracking()
            .Include(a => a.ActorUser)
            .Include(a => a.Document)
            .OrderByDescending(a => a.OccurredDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        return new DocumentAuditReport(
            totalDocuments,
            totalActiveUploaders,
            byCategory.ToDictionary(x => x.Key, x => x.Count),
            byAction.ToDictionary(x => x.Key, x => x.Count),
            recentActivity);
    }

    private IQueryable<Document> BuildAccessibleQuery(int requestingUserId)
    {
        return _context.Documents
            .AsNoTracking()
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Include(d => d.Tags)
            .Where(d => !d.IsDeleted && (
                d.UploaderId == requestingUserId ||
                (d.ProjectId.HasValue && (d.Project!.ProjectManagerId == requestingUserId || d.Project.ProjectMembers.Any(m => m.UserId == requestingUserId))) ||
                d.Shares.Any(s => s.UserId == requestingUserId)));
    }

    private async Task<bool> CanAccessAsync(Document document, int requestingUserId, CancellationToken cancellationToken)
    {
        if (document.UploaderId == requestingUserId) return true;
        if (await IsAdministratorAsync(requestingUserId, cancellationToken)) return true;

        if (document.ProjectId.HasValue)
        {
            var project = document.Project ?? await _context.Projects.Include(p => p.ProjectMembers).FirstOrDefaultAsync(p => p.ProjectId == document.ProjectId.Value, cancellationToken);
            if (project != null && (project.ProjectManagerId == requestingUserId || project.ProjectMembers.Any(m => m.UserId == requestingUserId)))
                return true;
        }

        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && s.UserId == requestingUserId, cancellationToken);
    }

    private async Task<bool> CanManageAsync(Document document, int requestingUserId, CancellationToken cancellationToken)
    {
        if (document.UploaderId == requestingUserId) return true;
        return await IsAdministratorAsync(requestingUserId, cancellationToken);
    }

    private async Task<bool> CanDeleteAsync(Document document, int requestingUserId, CancellationToken cancellationToken)
    {
        if (document.UploaderId == requestingUserId) return true;
        if (await IsAdministratorAsync(requestingUserId, cancellationToken)) return true;

        if (document.ProjectId.HasValue)
        {
            var project = document.Project ?? await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == document.ProjectId.Value, cancellationToken);
            if (project != null && project.ProjectManagerId == requestingUserId) return true;
        }

        return false;
    }

    private async Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        return user?.Role == UserRole.Administrator;
    }
}
