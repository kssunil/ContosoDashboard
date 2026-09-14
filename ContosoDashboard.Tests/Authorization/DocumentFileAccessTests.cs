using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Authorization;

public class DocumentFileAccessTests
{
    [Fact]
    public async Task OwnerCanAccessTheirDocument()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);

        var service = CreateService(context);
        var result = await service.GetDocumentForAccessAsync(document.DocumentId, 4);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UnauthorizedUserCannotAccessDocumentByGuessingId()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);

        var service = CreateService(context);
        var result = await service.GetDocumentForAccessAsync(document.DocumentId, 100);

        Assert.Null(result);
    }

    [Fact]
    public async Task ProjectMemberCanAccessProjectDocumentButOutsiderCannot()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        context.Projects.Add(new Project { ProjectId = 100, Name = "Training Project", ProjectManagerId = 2 });
        context.ProjectMembers.Add(new ProjectMember { ProjectMemberId = 100, ProjectId = 100, UserId = 3, Role = "TeamMember" });
        await context.SaveChangesAsync();
        var document = await SeedDocumentAsync(context, uploaderId: 2, projectId: 100);

        var service = CreateService(context);

        Assert.NotNull(await service.GetDocumentForAccessAsync(document.DocumentId, 3));
        Assert.Null(await service.GetDocumentForAccessAsync(document.DocumentId, 101));
    }

    [Fact]
    public async Task AdministratorCanAccessAnyDocument()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);

        var service = CreateService(context);
        var result = await service.GetDocumentForAccessAsync(document.DocumentId, 1);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExplicitShareGrantsAccessAndUnsharedUserIsDenied()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);
        context.DocumentShares.Add(new DocumentShare { DocumentId = document.DocumentId, UserId = 100, SharedByUserId = 4 });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        Assert.NotNull(await service.GetDocumentForAccessAsync(document.DocumentId, 100));
        Assert.Null(await service.GetDocumentForAccessAsync(document.DocumentId, 101));
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

    private static void SeedUsers(ApplicationDbContext context)
    {
        // UserId 1 (Administrator), 2 (ProjectManager), 3 (TeamLead), 4 (Employee) already exist via database seed data.
        context.Users.AddRange(
            new User { UserId = 100, Email = "other@contoso.com", DisplayName = "Other Employee", Role = UserRole.Employee },
            new User { UserId = 101, Email = "outsider@contoso.com", DisplayName = "Outsider", Role = UserRole.Employee }
        );
        context.SaveChanges();
    }

    private static async Task<Document> SeedDocumentAsync(ApplicationDbContext context, int uploaderId, int? projectId = null)
    {
        var document = new Document
        {
            Title = "Confidential Report",
            Category = DocumentCategories.Reports,
            FilePath = $"{uploaderId}/personal/{Guid.NewGuid():N}.pdf",
            OriginalFileName = "report.pdf",
            FileSize = 100,
            MimeType = "application/pdf",
            UploaderId = uploaderId,
            ProjectId = projectId
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        return document;
    }

    private static DocumentService CreateService(ApplicationDbContext context)
    {
        var root = Path.Combine(Path.GetTempPath(), "file-access-" + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = root
        }).Build();
        var storage = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
        return new DocumentService(context, storage, new LocalFileSafetyScanner(), new NotificationService(context), NullLogger<DocumentService>.Instance);
    }
}
