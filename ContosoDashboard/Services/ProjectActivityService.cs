using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public interface IProjectActivityService
{
    Task<ProjectActivity> AddAsync(int projectId, int actorUserId, string action, string? details = null, CancellationToken cancellationToken = default);
    Task<List<ProjectActivity>> GetAsync(int projectId, int requestingUserId, CancellationToken cancellationToken = default);
}

public sealed class ProjectActivityService : IProjectActivityService
{
    private readonly ApplicationDbContext _context;

    public ProjectActivityService(ApplicationDbContext context) => _context = context;

    public async Task<ProjectActivity> AddAsync(int projectId, int actorUserId, string action, string? details = null, CancellationToken cancellationToken = default)
    {
        var activity = new ProjectActivity
        {
            ProjectId = projectId,
            ActorUserId = actorUserId,
            Action = action,
            Details = details,
            OccurredDate = DateTime.UtcNow
        };
        _context.ProjectActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
        return activity;
    }

    public async Task<List<ProjectActivity>> GetAsync(int projectId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var project = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId, cancellationToken);
        if (project == null) return new List<ProjectActivity>();

        var isProjectManager = project.ProjectManagerId == requestingUserId;
        var isAdministrator = await _context.Users.AsNoTracking().AnyAsync(u => u.UserId == requestingUserId && u.Role == UserRole.Administrator, cancellationToken);
        if (!isProjectManager && !isAdministrator) return new List<ProjectActivity>();

        return await _context.ProjectActivities.AsNoTracking()
            .Include(a => a.ActorUser)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.OccurredDate)
            .ToListAsync(cancellationToken);
    }
}
