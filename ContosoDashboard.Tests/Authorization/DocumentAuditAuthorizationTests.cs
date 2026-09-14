using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Authorization;

public class DocumentAuditAuthorizationTests
{
    [Fact]
    public async Task AdministratorCanRetrieveAuditReport()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        await SeedActivityAsync(context);
        var service = CreateService(context);

        var report = await service.GetAuditReportAsync(1);

        Assert.NotNull(report);
        Assert.Equal(1, report!.TotalDocuments);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task NonAdministratorCannotRetrieveAuditReport(int requestingUserId)
    {
        await using var context = CreateContext();
        SeedUsers(context);
        await SeedActivityAsync(context);
        var service = CreateService(context);

        var report = await service.GetAuditReportAsync(requestingUserId);

        Assert.Null(report);
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
        // UserId 1 (Administrator), 2 (ProjectManager), and 4 (Employee) already exist via database seed data.
    }

    private static async Task SeedActivityAsync(ApplicationDbContext context)
    {
        var document = new Document
        {
            Title = "Audit Document",
            Category = DocumentCategories.Reports,
            FilePath = "4/personal/audit.pdf",
            OriginalFileName = "audit.pdf",
            FileSize = 10,
            MimeType = "application/pdf",
            UploaderId = 4
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            ActorUserId = 4,
            Action = DocumentActivityActions.Upload
        });
        await context.SaveChangesAsync();
    }

    private static DocumentService CreateService(ApplicationDbContext context)
    {
        var root = Path.Combine(Path.GetTempPath(), "audit-" + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = root
        }).Build();
        var storage = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
        return new DocumentService(context, storage, new LocalFileSafetyScanner(), new NotificationService(context), NullLogger<DocumentService>.Instance);
    }
}
