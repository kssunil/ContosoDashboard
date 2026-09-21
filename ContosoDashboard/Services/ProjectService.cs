using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class CreateProjectTaskRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public TaskPriority? Priority { get; init; }
    public Models.TaskStatus? Status { get; init; }
    public DateTime? DueDate { get; init; }
    public int? AssignedUserId { get; init; }
}

public sealed class CreateProjectDocumentRequest
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
}

public sealed class CreateProjectRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ProjectStatus Status { get; init; }
    public int? Progress { get; init; }
    public required int ProjectManagerId { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public List<CreateProjectTaskRequest> Tasks { get; init; } = new();
    public List<CreateProjectDocumentRequest> Documents { get; init; } = new();
    public List<int> TeamMemberUserIds { get; init; } = new();
}

public sealed record CreateProjectResult(bool Succeeded, Project? Project, string? ErrorMessage)
{
    public static CreateProjectResult Failure(string errorMessage) => new(false, null, errorMessage);
}

public interface IProjectService
{
    Task<List<Project>> GetUserProjectsAsync(int userId);
    Task<Project?> GetProjectByIdAsync(int projectId, int requestingUserId);
    Task<CreateProjectResult> CreateProjectAsync(CreateProjectRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateProjectAsync(Project project, int requestingUserId);
    Task<bool> AddProjectMemberAsync(int projectId, int userId, string role, int requestingUserId);
    Task<List<ProjectMember>> GetProjectMembersAsync(int projectId, int requestingUserId);
}

public class ProjectService : IProjectService
{
    private const int MaxStartDateMonthsInPast = 12;

    private readonly ApplicationDbContext _context;
    private readonly IProjectActivityService _projectActivityService;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileSafetyScanner _fileSafetyScanner;

    public ProjectService(
        ApplicationDbContext context,
        IProjectActivityService projectActivityService,
        IFileStorageService fileStorage,
        IFileSafetyScanner fileSafetyScanner)
    {
        _context = context;
        _projectActivityService = projectActivityService;
        _fileStorage = fileStorage;
        _fileSafetyScanner = fileSafetyScanner;
    }

    public async Task<List<Project>> GetUserProjectsAsync(int userId)
    {
        // Get projects where user is manager or a member
        var managedProjects = _context.Projects
            .Where(p => p.ProjectManagerId == userId);

        var memberProjects = _context.Projects
            .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId));

        var projects = await managedProjects
            .Union(memberProjects)
            .Include(p => p.ProjectManager)
            .Include(p => p.Tasks)
            .Include(p => p.ProjectMembers)
            .ThenInclude(pm => pm.User)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

        return projects;
    }

    public async Task<Project?> GetProjectByIdAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectManager)
            .Include(p => p.Tasks)
            .ThenInclude(t => t.AssignedUser)
            .Include(p => p.ProjectMembers)
            .ThenInclude(pm => pm.User)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null) return null;

        // Authorization: User must be project manager or a project member
        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isProjectManager && !isProjectMember)
        {
            return null; // User not authorized to view this project
        }

        return project;
    }

    public async Task<CreateProjectResult> CreateProjectAsync(CreateProjectRequest request, int actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == actorUserId, cancellationToken);
        if (actor == null || (actor.Role != UserRole.ProjectManager && actor.Role != UserRole.Administrator))
        {
            return CreateProjectResult.Failure("You do not have permission to create a project.");
        }

        var title = request.Title.Trim();
        if (title.Length == 0)
        {
            return CreateProjectResult.Failure("Title is required.");
        }

        var titleInUse = await _context.Projects.AsNoTracking()
            .AnyAsync(p => p.Name.ToLower() == title.ToLower(), cancellationToken);
        if (titleInUse)
        {
            return CreateProjectResult.Failure("A project with this title already exists.");
        }

        var projectManager = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.ProjectManagerId, cancellationToken);
        if (projectManager == null || projectManager.Role != UserRole.ProjectManager)
        {
            return CreateProjectResult.Failure("Select a valid project manager.");
        }

        var description = request.Description.Trim();
        if (description.Length == 0)
        {
            return CreateProjectResult.Failure("Description is required.");
        }

        if (request.Status != ProjectStatus.Active && request.Status != ProjectStatus.Inactive)
        {
            return CreateProjectResult.Failure("Status must be Active or Inactive.");
        }

        var progress = request.Progress ?? 0;
        if (progress < 0 || progress > 100)
        {
            return CreateProjectResult.Failure("Progress must be between 0 and 100.");
        }

        var earliestStartDate = DateTime.UtcNow.AddMonths(-MaxStartDateMonthsInPast);
        if (request.StartDate < earliestStartDate)
        {
            return CreateProjectResult.Failure($"Start Date cannot be more than {MaxStartDateMonthsInPast} months in the past.");
        }

        if (request.EndDate < request.StartDate)
        {
            return CreateProjectResult.Failure("End Date cannot be earlier than Start Date.");
        }

        foreach (var taskRequest in request.Tasks)
        {
            if (string.IsNullOrWhiteSpace(taskRequest.Title))
            {
                return CreateProjectResult.Failure("Every task must have a title.");
            }
        }

        if (request.TeamMemberUserIds.Count > 0)
        {
            var validMemberCount = await _context.Users.AsNoTracking()
                .CountAsync(u => request.TeamMemberUserIds.Contains(u.UserId), cancellationToken);
            if (validMemberCount != request.TeamMemberUserIds.Distinct().Count())
            {
                return CreateProjectResult.Failure("One or more selected team members are invalid.");
            }
        }

        var project = new Project
        {
            Name = title,
            Description = description,
            Status = request.Status,
            Progress = progress,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = request.StartDate,
            TargetCompletionDate = request.EndDate,
            CreatedByUserId = actorUserId,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        foreach (var taskRequest in request.Tasks)
        {
            project.Tasks.Add(new TaskItem
            {
                Title = taskRequest.Title.Trim(),
                Description = taskRequest.Description,
                Priority = taskRequest.Priority ?? TaskPriority.Medium,
                Status = taskRequest.Status ?? Models.TaskStatus.NotStarted,
                DueDate = taskRequest.DueDate,
                AssignedUserId = taskRequest.AssignedUserId ?? actorUserId,
                CreatedByUserId = actorUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        foreach (var userId in request.TeamMemberUserIds.Distinct())
        {
            project.ProjectMembers.Add(new ProjectMember
            {
                UserId = userId,
                Role = "Team Member",
                AssignedDate = DateTime.UtcNow
            });
        }

        var writtenFilePaths = new List<string>();
        try
        {
            foreach (var documentRequest in request.Documents)
            {
                var fileTitle = Path.GetFileNameWithoutExtension(documentRequest.FileName);
                var safety = await _fileSafetyScanner.ValidateAsync(
                    documentRequest.FileName, documentRequest.ContentType, documentRequest.Length,
                    fileTitle, DocumentCategories.ProjectDocuments, cancellationToken);
                if (!safety.IsSafe)
                {
                    return CreateProjectResult.Failure(string.Join(" ", safety.Errors));
                }

                var relativePath = _fileStorage.CreateRelativePath(actorUserId, null, Path.GetExtension(documentRequest.FileName));
                await _fileStorage.SaveAsync(relativePath, documentRequest.Content, cancellationToken);
                writtenFilePaths.Add(relativePath);

                project.Documents.Add(new Document
                {
                    Title = fileTitle,
                    Category = DocumentCategories.ProjectDocuments,
                    FilePath = relativePath,
                    OriginalFileName = Path.GetFileName(documentRequest.FileName),
                    FileSize = documentRequest.Length,
                    MimeType = documentRequest.ContentType,
                    UploaderId = actorUserId,
                    UploadedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            foreach (var relativePath in writtenFilePaths)
            {
                try
                {
                    await _fileStorage.DeleteAsync(relativePath, cancellationToken);
                }
                catch
                {
                    // Best-effort cleanup; the primary failure is reported below.
                }
            }

            return CreateProjectResult.Failure("The project could not be saved. Please try again.");
        }

        await _projectActivityService.AddAsync(project.ProjectId, actorUserId, ProjectActivityActions.ProjectCreated, cancellationToken: cancellationToken);

        return new CreateProjectResult(true, project, null);
    }

    public async Task<bool> UpdateProjectAsync(Project project, int requestingUserId)
    {
        var existingProject = await _context.Projects.FindAsync(project.ProjectId);
        if (existingProject == null) return false;

        // Authorization: Only project manager can update project
        if (existingProject.ProjectManagerId != requestingUserId)
        {
            return false; // User not authorized to update this project
        }

        existingProject.Name = project.Name;
        existingProject.Description = project.Description;
        existingProject.Status = project.Status;
        existingProject.TargetCompletionDate = project.TargetCompletionDate;
        existingProject.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddProjectMemberAsync(int projectId, int userId, string role, int requestingUserId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        // Authorization: Only project manager can add members
        if (project.ProjectManagerId != requestingUserId)
        {
            return false; // User not authorized to add members to this project
        }

        var existingMember = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

        if (existingMember != null) return false;

        var projectMember = new ProjectMember
        {
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            AssignedDate = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(projectMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<ProjectMember>> GetProjectMembersAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null) return new List<ProjectMember>();

        // Authorization: User must be project manager or member
        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isProjectManager && !isProjectMember)
        {
            return new List<ProjectMember>(); // User not authorized
        }

        return await _context.ProjectMembers
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync();
    }
}
