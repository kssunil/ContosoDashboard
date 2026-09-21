using ContosoDashboard.Models;
using ContosoDashboard.Services;
using ContosoDashboard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Services;

public class ProjectServiceCreationTests
{
    // Seeded users: 1 = Administrator, 2 = ProjectManager, 3 = TeamLead, 4 = Employee.
    // Seeded project: ProjectId 1, Name "ContosoDashboard Development", ProjectManagerId 2.

    private static CreateProjectRequest ValidRequest(string title = "New Onboarding Project") => new()
    {
        Title = title,
        Description = "A project created for testing.",
        Status = ProjectStatus.Active,
        ProjectManagerId = 2,
        StartDate = DateTime.UtcNow.AddDays(-1),
        EndDate = DateTime.UtcNow.AddDays(30)
    };

    [Fact]
    public async Task RejectsBlankTitle()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);

        var result = await service.CreateProjectAsync(ValidRequest(title: "   "), actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync());
    }

    [Fact]
    public async Task RejectsDuplicateTitleCaseInsensitive()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);

        var result = await service.CreateProjectAsync(ValidRequest(title: "contosodashboard development"), actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync());
    }

    [Fact]
    public async Task RejectsProjectManagerThatDoesNotHaveProjectManagerRole()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var valid = ValidRequest();
        var request = new CreateProjectRequest
        {
            Title = valid.Title,
            Description = valid.Description,
            Status = valid.Status,
            ProjectManagerId = 4, // Employee, not a ProjectManager
            StartDate = valid.StartDate,
            EndDate = valid.EndDate
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync());
    }

    [Fact]
    public async Task RejectsMissingDescription()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = "  ",
            Status = request.Status,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RejectsStatusOutsideActiveOrInactive()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = request.Description,
            Status = ProjectStatus.Planning,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task DefaultsProgressToZeroWhenOmitted()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);

        var result = await service.CreateProjectAsync(ValidRequest(), actorUserId: 2);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Project!.Progress);
    }

    [Fact]
    public async Task PersistsAndDisplaysProgressExactlyAsEntered()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Progress = 100,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.True(result.Succeeded);
        Assert.Equal(100, result.Project!.Progress);
    }

    [Fact]
    public async Task RejectsProgressOutOfRange()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Progress = 101,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RejectsStartDateMoreThanTwelveMonthsInPast()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = DateTime.UtcNow.AddMonths(-13),
            EndDate = request.EndDate
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AllowsBackfilledStartDateWithinTwelveMonths()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = request.Description,
            Status = ProjectStatus.Inactive,
            Progress = 100,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow.AddMonths(-1)
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RejectsEndDateEarlierThanStartDate()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request = new CreateProjectRequest
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SuccessfulSaveRecordsCreatorAndAuditActivity()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);

        var result = await service.CreateProjectAsync(ValidRequest(), actorUserId: 2);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Project!.CreatedByUserId);
        var activity = await context.ProjectActivities.SingleAsync(a => a.ProjectId == result.Project.ProjectId);
        Assert.Equal(ProjectActivityActions.ProjectCreated, activity.Action);
        Assert.Equal(2, activity.ActorUserId);
    }

    [Fact]
    public async Task RejectsTaskWithBlankTitle()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request.Tasks.Add(new CreateProjectTaskRequest { Title = "   " });

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync());
    }

    [Fact]
    public async Task DefaultsTaskAssigneeAndPriorityWhenNotSupplied()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request.Tasks.Add(new CreateProjectTaskRequest { Title = "Kickoff meeting" });

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.True(result.Succeeded);
        var task = await context.Tasks.SingleAsync(t => t.ProjectId == result.Project!.ProjectId);
        Assert.Equal(2, task.AssignedUserId);
        Assert.Equal(TaskPriority.Medium, task.Priority);
    }

    [Fact]
    public async Task TasksAreNotPersistedWhenSaveFails()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest(title: "ContosoDashboard Development"); // duplicate -> fails
        request.Tasks.Add(new CreateProjectTaskRequest { Title = "Should not be saved" });

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.DoesNotContain(await context.Tasks.ToListAsync(), t => t.Title == "Should not be saved");
    }

    [Fact]
    public async Task PersistsTeamMembersWithinSameCall()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request.TeamMemberUserIds.Add(3);
        request.TeamMemberUserIds.Add(4);

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.True(result.Succeeded);
        var memberIds = await context.ProjectMembers
            .Where(m => m.ProjectId == result.Project!.ProjectId)
            .Select(m => m.UserId)
            .ToListAsync();
        Assert.Equal(new[] { 3, 4 }, memberIds.OrderBy(id => id));
    }

    [Fact]
    public async Task RejectsInvalidTeamMemberId()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);
        var request = ValidRequest();
        request.TeamMemberUserIds.Add(999);

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync());
    }

    [Fact]
    public async Task TeamMembersAreNotPersistedWhenSaveFails()
    {
        await using var context = CreateContext();
        var seededMemberCount = await context.ProjectMembers.CountAsync();
        var service = CreateService(context, out _);
        var request = ValidRequest(title: "ContosoDashboard Development"); // duplicate -> fails
        request.TeamMemberUserIds.Add(3);

        var result = await service.CreateProjectAsync(request, actorUserId: 2);

        Assert.False(result.Succeeded);
        Assert.Equal(seededMemberCount, await context.ProjectMembers.CountAsync());
    }

    [Fact]
    public async Task PersistsStagedDocumentWithinSameCallAndWritesFile()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await using var context = CreateContext();
            var service = CreateService(context, out _, root);
            var request = ValidRequest();
            request.Documents.Add(new CreateProjectDocumentRequest
            {
                Content = new MemoryStream([1, 2, 3]),
                FileName = "charter.pdf",
                ContentType = "application/pdf",
                Length = 3
            });

            var result = await service.CreateProjectAsync(request, actorUserId: 2);

            Assert.True(result.Succeeded);
            var document = await context.Documents.SingleAsync(d => d.ProjectId == result.Project!.ProjectId);
            Assert.True(File.Exists(Path.Combine(root, document.FilePath.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DoesNotCreateDocumentRowOrFileWhenSaveFails()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await using var context = CreateContext();
            var service = CreateService(context, out _, root);
            var request = ValidRequest(title: "ContosoDashboard Development"); // duplicate -> fails before any write
            request.Documents.Add(new CreateProjectDocumentRequest
            {
                Content = new MemoryStream([1, 2, 3]),
                FileName = "charter.pdf",
                ContentType = "application/pdf",
                Length = 3
            });

            var result = await service.CreateProjectAsync(request, actorUserId: 2);

            Assert.False(result.Succeeded);
            Assert.Empty(await context.Documents.ToListAsync());
            Assert.False(Directory.Exists(root) && Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Any());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RejectsWhenActorIsNotProjectManagerOrAdministrator()
    {
        await using var context = CreateContext();
        var service = CreateService(context, out _);

        var result = await service.CreateProjectAsync(ValidRequest(), actorUserId: 4); // Employee

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync());
    }

    private static ProjectService CreateService(ApplicationDbContext context, out IProjectActivityService activityService, string? storageRoot = null)
    {
        activityService = new ProjectActivityService(context);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = storageRoot ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        }).Build();
        var storage = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
        return new ProjectService(context, activityService, storage, new LocalFileSafetyScanner());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
