using System.Security.Claims;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContosoDashboard.Controllers;

[Authorize]
[Route("documents/files")]
public sealed class DocumentFilesController : Controller
{
    private readonly IDocumentService _documentService;

    public DocumentFilesController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("{id:int}/preview")]
    public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Forbid();

        var document = await _documentService.GetDocumentForAccessAsync(id, userId, cancellationToken);
        if (document == null) return NotFound();

        var stream = await _documentService.OpenDocumentContentAsync(id, userId, DocumentActivityActions.Preview, cancellationToken);
        if (stream == null) return NotFound();

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{Uri.EscapeDataString(document.OriginalFileName)}\"";
        return File(stream, document.MimeType);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Forbid();

        var document = await _documentService.GetDocumentForAccessAsync(id, userId, cancellationToken);
        if (document == null) return NotFound();

        var stream = await _documentService.OpenDocumentContentAsync(id, userId, DocumentActivityActions.Download, cancellationToken);
        if (stream == null) return NotFound();

        return File(stream, document.MimeType, document.OriginalFileName);
    }

    private bool TryGetUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
