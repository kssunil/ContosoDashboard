using ContosoDashboard.Models;
using ContosoDashboard.Services;
using ContosoDashboard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Authorization;

public class ProjectCreationAuthorizationTests
{
    // Seeded users: 1 = Administrator, 2 = ProjectManager, 3 = TeamLead, 4 = Employee.

    [Theory]
    [InlineData(3)] // TeamLead
    [InlineData(4)] // Employee
    public async Task DeniesCreationForRolesWithoutProjectManagerOrAdministrator(int actorUserId)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(ValidRequest(), actorUserId);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await context.Projects.CountAsync()); // only the seeded project remains
        Assert.Empty(await context.ProjectActivities.ToListAsync());
    }

    [Theory]
    [InlineData(1)] // Administrator
    [InlineData(2)] // ProjectManager
    public async Task AllowsCreationForProjectManagerOrAdministrator(int actorUserId)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateProjectAsync(ValidRequest(), actorUserId);

        Assert.True(result.Succeeded);
        Assert.Equal(2, await context.Projects.CountAsync());
    }

    [Fact]
    public async Task DeniedAttemptCreatesNoAssociatedTasksOrDocuments()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await using var context = CreateContext();
            var service = CreateService(context, root);
            var request = ValidRequest();
            request.Tasks.Add(new CreateProjectTaskRequest { Title = "Should not be created" });
            request.Documents.Add(new CreateProjectDocumentRequest
            {
                Content = new MemoryStream([1, 2, 3]),
                FileName = "charter.pdf",
                ContentType = "application/pdf",
                Length = 3
            });

            var result = await service.CreateProjectAsync(request, actorUserId: 4); // Employee

            Assert.False(result.Succeeded);
            Assert.Empty(await context.Tasks.Where(t => t.Title == "Should not be created").ToListAsync());
            Assert.Empty(await context.Documents.ToListAsync());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static CreateProjectRequest ValidRequest() => new()
    {
        Title = $"Authorization Test Project {Guid.NewGuid():N}",
        Description = "A project created for authorization testing.",
        Status = ProjectStatus.Active,
        ProjectManagerId = 2,
        StartDate = DateTime.UtcNow.AddDays(-1),
        EndDate = DateTime.UtcNow.AddDays(30)
    };

    private static ProjectService CreateService(ApplicationDbContext context, string? storageRoot = null)
    {
        var activityService = new ProjectActivityService(context);
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
