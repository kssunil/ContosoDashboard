using ContosoDashboard.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ContosoDashboard.Tests.Storage;

public class FileStorageServiceTests
{
    [Fact]
    public async Task SavesAndReadsFileUnderConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentStorage:RootPath"] = root })
                .Build();
            var service = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);
            var relativePath = service.CreateRelativePath(4, null, ".pdf");

            await service.SaveAsync(relativePath, new MemoryStream([1, 2, 3]));
            await using var stored = await service.OpenReadAsync(relativePath);
            using var copy = new MemoryStream();
            await stored!.CopyToAsync(copy);

            Assert.Equal(new byte[] { 1, 2, 3 }, copy.ToArray());
            Assert.StartsWith("4/personal/", relativePath);
            Assert.EndsWith(".pdf", relativePath);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RejectsPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DocumentStorage:RootPath"] = root })
            .Build();
        var service = new LocalFileStorageService(configuration, NullLogger<LocalFileStorageService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveAsync("../outside.txt", new MemoryStream([1])));
    }
}
