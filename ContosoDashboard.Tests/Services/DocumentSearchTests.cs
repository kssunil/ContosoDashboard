using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Services;

public class DocumentSearchTests
{
    [Fact]
    public async Task SearchFiltersByTextCategoryAndSortsResults()
    {
        await using var context = CreateContext();
        SeedUsersAndProject(context);
        context.Documents.AddRange(
            new Document { Title = "Budget Report", Category = DocumentCategories.Reports, FilePath = "1/personal/a.pdf", OriginalFileName = "a.pdf", FileSize = 100, MimeType = "application/pdf", UploaderId = 4, UploadedDate = DateTime.UtcNow.AddDays(-3) },
            new Document { Title = "Roadmap Slides", Category = DocumentCategories.Presentations, FilePath = "1/personal/b.pptx", OriginalFileName = "b.pptx", FileSize = 200, MimeType = "application/vnd.ms-powerpoint", UploaderId = 4, UploadedDate = DateTime.UtcNow.AddDays(-1) },
            new Document { Title = "Team Notes", Category = DocumentCategories.TeamResources, FilePath = "1/personal/c.txt", OriginalFileName = "c.txt", FileSize = 50, MimeType = "text/plain", UploaderId = 4, UploadedDate = DateTime.UtcNow.AddDays(-2) }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context, "search-root-1");

        var textResult = await service.SearchAsync(new DocumentSearchRequest { SearchText = "Roadmap" }, 4);
        Assert.Single(textResult.Items);
        Assert.Equal("Roadmap Slides", textResult.Items[0].Title);

        var categoryResult = await service.SearchAsync(new DocumentSearchRequest { Category = DocumentCategories.Reports }, 4);
        Assert.Single(categoryResult.Items);
        Assert.Equal("Budget Report", categoryResult.Items[0].Title);

        var sortedResult = await service.SearchAsync(new DocumentSearchRequest { SortBy = DocumentSortField.Title, SortDescending = false }, 4);
        Assert.Equal(new[] { "Budget Report", "Roadmap Slides", "Team Notes" }, sortedResult.Items.Select(d => d.Title));
    }

    [Fact]
    public async Task SearchPaginatesResultsAndOnlyReturnsAccessibleDocuments()
    {
        await using var context = CreateContext();
        SeedUsersAndProject(context);
        for (var i = 0; i < 5; i++)
        {
            context.Documents.Add(new Document
            {
                Title = $"Doc {i}",
                Category = DocumentCategories.Other,
                FilePath = $"4/personal/{i}.txt",
                OriginalFileName = $"{i}.txt",
                FileSize = 10,
                MimeType = "text/plain",
                UploaderId = 4,
                UploadedDate = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        context.Documents.Add(new Document
        {
            Title = "Other User Doc",
            Category = DocumentCategories.Other,
            FilePath = "100/personal/x.txt",
            OriginalFileName = "x.txt",
            FileSize = 10,
            MimeType = "text/plain",
            UploaderId = 100,
            UploadedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, "search-root-2");

        var firstPage = await service.SearchAsync(new DocumentSearchRequest { Page = 1, PageSize = 2 }, 4);
        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.DoesNotContain(firstPage.Items, d => d.Title == "Other User Doc");
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

    private static void SeedUsersAndProject(ApplicationDbContext context)
    {
        // UserId 4 (Employee) already exists via database seed data.
        context.Users.Add(new User { UserId = 100, Email = "other@contoso.com", DisplayName = "Other Employee", Role = UserRole.Employee });
        context.SaveChanges();
    }

    private static DocumentService CreateService(ApplicationDbContext context, string rootFolder)
    {
        var root = Path.Combine(Path.GetTempPath(), rootFolder + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DocumentStorage:RootPath"] = root
        }).Build();
        var storage = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
        return new DocumentService(context, storage, new LocalFileSafetyScanner(), new NotificationService(context), NullLogger<DocumentService>.Instance);
    }
}
