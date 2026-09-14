using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
}

public sealed record StoredFile(string RelativePath, long Length);

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    string CreateRelativePath(int userId, int? projectId, string extension);
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        var configuredRoot = configuration["DocumentStorage:RootPath"] ?? "AppData/uploads";
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(AppContext.BaseDirectory, configuredRoot));
        _logger = logger;
        Directory.CreateDirectory(_rootPath);
    }

    public string CreateRelativePath(int userId, int? projectId, string extension)
    {
        var safeExtension = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        var scope = projectId.HasValue ? projectId.Value.ToString() : "personal";
        return Path.Combine(userId.ToString(), scope, $"{Guid.NewGuid():N}{safeExtension}").Replace('\\', '/');
    }

    public async Task<StoredFile> SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await content.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
            return new StoredFile(relativePath, output.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage failure while saving {RelativePath}.", relativePath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(relativePath);
        try
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage failure while deleting {RelativePath}.", relativePath);
            throw;
        }
        return Task.CompletedTask;
    }

    private string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("The document path must be a non-empty relative path.");

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Rejected path traversal attempt for relative path {RelativePath}.", relativePath);
            throw new InvalidOperationException("The document path is outside the configured storage root.");
        }
        return fullPath;
    }
}
