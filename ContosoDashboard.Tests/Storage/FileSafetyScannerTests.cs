using ContosoDashboard.Services;

namespace ContosoDashboard.Tests.Storage;

public class FileSafetyScannerTests
{
    private readonly LocalFileSafetyScanner _scanner = new();

    [Fact]
    public async Task AcceptsSupportedPdfWithinLimit()
    {
        var result = await _scanner.ValidateAsync("brief.pdf", "application/pdf", 1024, "Brief", "Other");
        Assert.True(result.IsSafe);
    }

    [Fact]
    public async Task RejectsUnsupportedTypeAndOversizedFile()
    {
        var result = await _scanner.ValidateAsync("script.exe", "application/octet-stream", 25 * 1024 * 1024 + 1, "Script", "Other");
        Assert.False(result.IsSafe);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task RejectsMissingTitleAndCategory()
    {
        var result = await _scanner.ValidateAsync("brief.pdf", "application/pdf", 100, "", "Invalid");
        Assert.False(result.IsSafe);
        Assert.Equal(2, result.Errors.Count);
    }
}
