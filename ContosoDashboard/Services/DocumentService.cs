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

public interface IDocumentService
{
    Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default);
    Task<List<Document>> GetAccessibleDocumentsAsync(int requestingUserId, int? projectId = null, CancellationToken cancellationToken = default);
    Task<List<Document>> GetRecentUploadsAsync(int requestingUserId, int count = 5, CancellationToken cancellationToken = default);
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
        if (!safety.IsSafe) return DocumentUploadResult.Failure(safety.Errors.ToArray());

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
        var query = _context.Documents
            .AsNoTracking()
            .Include(d => d.Uploader)
            .Include(d => d.Project)
            .Include(d => d.Tags)
            .Where(d => !d.IsDeleted && (d.UploaderId == requestingUserId ||
                (d.ProjectId.HasValue && (d.Project!.ProjectManagerId == requestingUserId || d.Project.ProjectMembers.Any(m => m.UserId == requestingUserId)))));

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
}
