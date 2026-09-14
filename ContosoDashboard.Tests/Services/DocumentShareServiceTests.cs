using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Tests.Services;

public class DocumentShareServiceTests
{
    [Fact]
    public async Task OwnerCanShareDocumentAndRecipientGainsAccessAndNotification()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);
        var service = CreateService(context);

        var result = await service.ShareAsync(document.DocumentId, recipientUserId: 100, requestingUserId: 4);

        Assert.True(result.Succeeded);
        Assert.Equal(1, await context.DocumentShares.CountAsync());
        Assert.Equal(1, await context.Notifications.CountAsync(n => n.UserId == 100 && n.Type == NotificationType.DocumentShared));
        Assert.Equal(1, await context.DocumentActivities.CountAsync(a => a.Action == DocumentActivityActions.Share));
    }

    [Fact]
    public async Task DuplicateShareIsRejected()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);
        var service = CreateService(context);

        await service.ShareAsync(document.DocumentId, 100, 4);
        var duplicate = await service.ShareAsync(document.DocumentId, 100, 4);

        Assert.False(duplicate.Succeeded);
        Assert.Equal(1, await context.DocumentShares.CountAsync());
    }

    [Fact]
    public async Task NonOwnerCannotShareDocument()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);
        var service = CreateService(context);

        var result = await service.ShareAsync(document.DocumentId, 101, requestingUserId: 100);

        Assert.False(result.Succeeded);
        Assert.Empty(context.DocumentShares);
    }

    [Fact]
    public async Task NonMemberCanBeExplicitlySharedAProjectDocument()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        context.Projects.Add(new Project { ProjectId = 100, Name = "Training Project", ProjectManagerId = 4 });
        await context.SaveChangesAsync();
        var document = await SeedDocumentAsync(context, uploaderId: 4, projectId: 100);
        var service = CreateService(context);

        var result = await service.ShareAsync(document.DocumentId, recipientUserId: 101, requestingUserId: 4);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task OwnerCanRevokeShare()
    {
        await using var context = CreateContext();
        SeedUsers(context);
        var document = await SeedDocumentAsync(context, uploaderId: 4);
        var service = CreateService(context);
        var shareResult = await service.ShareAsync(document.DocumentId, 100, 4);

        var revoked = await service.RevokeAsync(shareResult.Share!.DocumentShareId, 4);

        Assert.True(revoked);
        Assert.Empty(context.DocumentShares);
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
        // UserId 4 (Employee/owner) already exists via database seed data.
        context.Users.AddRange(
            new User { UserId = 100, Email = "recipient@contoso.com", DisplayName = "Recipient", Role = UserRole.Employee },
            new User { UserId = 101, Email = "outsider@contoso.com", DisplayName = "Outsider", Role = UserRole.Employee }
        );
        context.SaveChanges();
    }

    private static async Task<Document> SeedDocumentAsync(ApplicationDbContext context, int uploaderId, int? projectId = null)
    {
        var document = new Document
        {
            Title = "Shared Report",
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

    private static DocumentShareService CreateService(ApplicationDbContext context)
    {
        return new DocumentShareService(context, new NotificationService(context), new DocumentActivityService(context));
    }
}
