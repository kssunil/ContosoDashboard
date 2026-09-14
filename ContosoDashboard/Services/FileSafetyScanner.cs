using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed record FileSafetyResult(bool IsSafe, IReadOnlyList<string> Errors)
{
    public static FileSafetyResult Success { get; } = new(true, Array.Empty<string>());
}

public interface IFileSafetyScanner
{
    Task<FileSafetyResult> ValidateAsync(string fileName, string contentType, long length, string title, string category, CancellationToken cancellationToken = default);
}

public sealed class LocalFileSafetyScanner : IFileSafetyScanner
{
    private const long MaxFileSize = 25 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> SupportedTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".doc"] = ["application/msword"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
        [".xls"] = ["application/vnd.ms-excel"],
        [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        [".ppt"] = ["application/vnd.ms-powerpoint"],
        [".pptx"] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
        [".txt"] = ["text/plain"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"]
    };

    public Task<FileSafetyResult> ValidateAsync(string fileName, string contentType, long length, string title, string category, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var extension = Path.GetExtension(fileName);
        if (!SupportedTypes.TryGetValue(extension, out var mimeTypes))
            errors.Add("This file type is not supported.");
        else if (!string.IsNullOrWhiteSpace(contentType) && !mimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            errors.Add("The file content type does not match its extension.");

        if (length <= 0 || length > MaxFileSize)
            errors.Add("Each file must be larger than zero and no more than 25 MB.");
        if (string.IsNullOrWhiteSpace(title) || title.Length > 255)
            errors.Add("A document title is required and must be 255 characters or fewer.");
        if (!DocumentCategories.All.Contains(category))
            errors.Add("Select a valid document category.");

        return Task.FromResult(new FileSafetyResult(errors.Count == 0, errors));
    }
}
