using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Services;

public class DocumentLifecycleTests
{
    [Fact]
    public async Task OwnerCanEditMetadataAndActivityIsLogged()
    {
        var root = CreateRoot();
        try
        {
            await using var context = CreateContext();
            SeedUsers(context);
            var storage = CreateStorage(root);
            var document = await SeedStoredDocumentAsync(context, storage, uploaderId: 4);
            var service = CreateService(context, storage);

            var result = await service.EditMetadataAsync(document.DocumentId, new DocumentMetadataUpdate("Updated Title", "New description", DocumentCategories.Other, ["alpha", "beta"]), 4);

            Assert.True(result.Succeeded);
            var updated = await context.Documents.Include(d => d.Tags).FirstAsync(d => d.DocumentId == document.DocumentId);
            Assert.Equal("Updated Title", updated.Title);
            Assert.Equal(2, updated.Tags.Count);
            Assert.Equal(1, await context.DocumentActivities.CountAsync(a => a.Action == DocumentActivityActions.MetadataEdit));
        }
        finally
        {
            CleanupRoot(root);
        }
    }

    [Fact]
    public async Task NonOwnerCannotEditMetadata()
    {
        var root = CreateRoot();
        try
        {
            await using var context = CreateContext();
            SeedUsers(context);
            var storage = CreateStorage(root);
            var document = await SeedStoredDocumentAsync(context, storage, uploaderId: 4);
            var service = CreateService(context, storage);

            var result = await service.EditMetadataAsync(document.DocumentId, new DocumentMetadataUpdate("Hacked Title", null, DocumentCategories.Other, []), 100);

            Assert.False(result.Succeeded);
        }
        finally
        {
            CleanupRoot(root);
        }
    }

    [Fact]
    public async Task ReplacingFileCreatesVersionHistoryAndUpdatesDocument()
    {
        var root = CreateRoot();
        try
        {
            await using var context = CreateContext();
            SeedUsers(context);
            var storage = CreateStorage(root);
            var document = await SeedStoredDocumentAsync(context, storage, uploaderId: 4);
            var service = CreateService(context, storage);

            var result = await service.ReplaceFileAsync(document.DocumentId, new MemoryStream([9, 9, 9]), "replacement.pdf", "application/pdf", 3, 4);

            Assert.True(result.Succeeded);
            var updated = await context.Documents.Include(d => d.Versions).FirstAsync(d => d.DocumentId == document.DocumentId);
            Assert.Single(updated.Versions);
            Assert.Equal(1, updated.Versions.First().VersionNumber);
            Assert.Equal("replacement.pdf", updated.OriginalFileName);
            Assert.Equal(1, await context.DocumentActivities.CountAsync(a => a.Action == DocumentActivityActions.Replacement));
        }
        finally
        {
            CleanupRoot(root);
        }
    }

    [Fact]
    public async Task DeleteRemovesFileAndMarksDocumentDeletedWithActivity()
    {
        var root = CreateRoot();
        try
        {
            await using var context = CreateContext();
            SeedUsers(context);
            var storage = CreateStorage(root);
            var document = await SeedStoredDocumentAsync(context, storage, uploaderId: 4);
            var service = CreateService(context, storage);

            var result = await service.DeleteAsync(document.DocumentId, 4);

            Assert.True(result.Succeeded);
            var deleted = await context.Documents.FirstAsync(d => d.DocumentId == document.DocumentId);
            Assert.True(deleted.IsDeleted);
            Assert.Equal(1, await context.DocumentActivities.CountAsync(a => a.Action == DocumentActivityActions.Delete));
            Assert.Null(await storage.OpenReadAsync(document.FilePath));
        }
        finally
        {
            CleanupRoot(root);
        }
    }

    [Fact]
    public async Task ProjectManagerCanDeleteProjectDocumentButNotEditMetadata()
    {
        var root = CreateRoot();
        try
        {
            await using var context = CreateContext();
            SeedUsers(context);
            context.Projects.Add(new Project { ProjectId = 100, Name = "Training Project", ProjectManagerId = 2 });
            await context.SaveChangesAsync();
            var storage = CreateStorage(root);
            var document = await SeedStoredDocumentAsync(context, storage, uploaderId: 4, projectId: 100);
            var service = CreateService(context, storage);

            var editResult = await service.EditMetadataAsync(document.DocumentId, new DocumentMetadataUpdate("Changed", null, DocumentCategories.Other, []), 2);
            Assert.False(editResult.Succeeded);

            var deleteResult = await service.DeleteAsync(document.DocumentId, 2);
            Assert.True(deleteResult.Succeeded);
        }
        finally
        {
            CleanupRoot(root);
        }
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
        // UserId 2 (ProjectManager) and 4 (Employee) already exist via database seed data.
    }

    private static async Task<Document> SeedStoredDocumentAsync(ApplicationDbContext context, LocalFileStorageService storage, int uploaderId, int? projectId = null)
    {
        var relativePath = storage.CreateRelativePath(uploaderId, projectId, ".pdf");
        await storage.SaveAsync(relativePath, new MemoryStream([1, 2, 3]));
        var document = new Document
        {
            Title = "Lifecycle Document",
            Category = DocumentCategories.Reports,
            FilePath = relativePath,
            OriginalFileName = "original.pdf",
            FileSize = 3,
            MimeType = "application/pdf",
            UploaderId = uploaderId,
            ProjectId = projectId,
            UpdatedDate = DateTime.UtcNow
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        return document;
    }

    private static string CreateRoot() => Path.Combine(Path.GetTempPath(), "lifecycle-" + Guid.NewGuid().ToString("N"));

    private static void CleanupRoot(string root)
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private static LocalFileStorageService CreateStorage(string root)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = root
        }).Build();
        return new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
    }

    private static DocumentService CreateService(ApplicationDbContext context, LocalFileStorageService storage)
    {
        return new DocumentService(context, storage, new LocalFileSafetyScanner(), new NotificationService(context), NullLogger<DocumentService>.Instance);
    }
}
