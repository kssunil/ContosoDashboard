using ContosoDashboard.Models;
using ContosoDashboard.Services;
using ContosoDashboard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Services;

public class DocumentServiceUploadTests
{
    [Fact]
    public void UploadRequestCanCarryProjectAndTaskContext()
    {
        var request = new DocumentUploadRequest
        {
            Content = new MemoryStream([1]),
            FileName = "requirements.pdf",
            ContentType = "application/pdf",
            Length = 1,
            Title = "Requirements",
            Category = DocumentCategories.ProjectDocuments,
            ProjectId = 1,
            TaskId = 2
        };

        Assert.Equal(1, request.ProjectId);
        Assert.Equal(2, request.TaskId);
    }

    [Fact]
    public async Task UploadsProjectDocumentAndCreatesMetadata()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            await using var context = CreateContext();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DocumentStorage:RootPath"] = root
            }).Build();
            var storage = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
            var service = new DocumentService(context, storage, new LocalFileSafetyScanner(), new NotificationService(context), NullLogger<DocumentService>.Instance);

            var result = await service.UploadAsync(new DocumentUploadRequest
            {
                Content = new MemoryStream([1, 2, 3]),
                FileName = "requirements.pdf",
                ContentType = "application/pdf",
                Length = 3,
                Title = "Requirements",
                Category = DocumentCategories.ProjectDocuments,
                ProjectId = 1,
                Tags = ["mvp"]
            }, 4);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Document);
            Assert.Equal(4, result.Document!.UploaderId);
            Assert.Equal(1, await context.Documents.CountAsync());
            Assert.Equal(1, await context.DocumentActivities.CountAsync());
            Assert.True(File.Exists(Path.Combine(root, result.Document.FilePath.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RejectsProjectUploadForNonMember()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await using var context = CreateContext();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = root
        }).Build();
        var storage = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
        var service = new DocumentService(context, storage, new LocalFileSafetyScanner(), new NotificationService(context), NullLogger<DocumentService>.Instance);

        var result = await service.UploadAsync(new DocumentUploadRequest
        {
            Content = new MemoryStream([1]),
            FileName = "requirements.pdf",
            ContentType = "application/pdf",
            Length = 1,
            Title = "Requirements",
            Category = DocumentCategories.ProjectDocuments,
            ProjectId = 1
        }, 1);

        Assert.False(result.Succeeded);
        Assert.Empty(context.Documents);
        if (Directory.Exists(root)) Directory.Delete(root, true);
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
