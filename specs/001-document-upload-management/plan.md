# Implementation Plan: Document Upload and Management

**Branch**: `main` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/001-document-upload-management/spec.md`

## Summary

Add offline document upload and management to the existing ContosoDashboard Blazor Server application. The implementation will add integer-keyed document metadata and access entities to EF Core, define a local filesystem storage abstraction outside `wwwroot`, enforce document authorization in services and file endpoints, add upload/list/search/preview/download/share flows, integrate project and task contexts, issue in-app notifications, and record document activity. The feature remains training-only and local; cloud storage and production malware scanning are explicit future boundaries rather than runtime dependencies.

## Technical Context

**Language/Version**: C# with ASP.NET Core/Blazor Server targeting .NET 9.0 (the repository project file is authoritative; constitution/README currently mention .NET 8)  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 9 SQL Server provider, existing cookie mock authentication, Bootstrap 5.3 and Bootstrap Icons  
**Storage**: Existing SQL Server/LocalDB database for metadata; local filesystem under an application data directory outside `wwwroot` for file content  
**Testing**: New focused .NET test project for service/storage/authorization behavior; `dotnet build`, `dotnet test`, and manual browser validation for Blazor upload/preview flows  
**Target Platform**: Windows offline training environment with local SQL Server/LocalDB and browser  
**Project Type**: Single web application with Blazor Server UI and service/data layers  
**Performance Goals**: Upload files up to 25 MB within 30 seconds on a typical local training network; lists and searches for up to 500 documents within 2 seconds; previews within 3 seconds where browser-supported  
**Constraints**: No cloud services or Azure SDK at runtime; files outside `wwwroot`; maximum 25 MB per file; allowed PDF, Word, Excel, PowerPoint, text, JPEG, and PNG types; authorization required for every document operation; integer IDs and text categories; preserve mock-auth training scope  
**Scale/Scope**: Existing seeded users/projects plus approximately 500 documents per list; one application and one local storage root; no production multi-tenant or enterprise search scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Training-First Scope**: PASS. The runtime is offline, local, and mock-authenticated. Virus scanning is represented as an explicit training placeholder boundary and cloud migration is not part of the default implementation.
- **II. Secure-by-Default Learning**: PASS. Service-layer authorization, project membership rules, protected file endpoints, generated paths, extension allowlists, and IDOR regression tests are required.
- **III. Spec-Driven Delivery**: PASS. The design artifacts trace the feature requirements to entities, workflows, contracts, and validation scenarios.
- **IV. Test-First and Verifiable Behavior**: PASS WITH FOLLOW-UP. The repository has no test project, so the plan creates one and requires focused tests before feature completion; manual browser scenarios cover UI-only behavior.
- **V. Simplicity, Clarity, and Maintainability**: PASS. The plan extends existing Models/Data/Services/Pages patterns and avoids a separate API, search engine, cloud SDK, or new application framework.
- **Repository target discrepancy**: ACCEPTED AND DOCUMENTED. The repository currently targets .NET 9.0 despite constitution/README references to .NET 8. This plan does not change the target; documentation reconciliation is separate follow-up work.

## Phase 0: Research Decisions

Research findings and alternatives are recorded in [research.md](research.md). The key resolved decisions are local filesystem storage behind `IFileStorageService`, service-layer authorization, database filtering with indexes, relational tags/shares/versions/audit events, and a local safety-scanner boundary.

## Phase 1: Design Artifacts

- [data-model.md](data-model.md) defines entities, fields, constraints, relationships, authorization rules, and upload/deletion state transitions.
- No external HTTP API contract is required for the current internal Blazor application. The storage and service interfaces are internal application contracts and should be documented in code and covered by tests rather than exposed as public API files.
- [quickstart.md](quickstart.md) defines runnable manual and automated validation scenarios.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── checklists/requirements.md
└── tasks.md                 # Created by /speckit.tasks
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Document.cs
│   ├── DocumentTag.cs
│   ├── DocumentShare.cs
│   ├── DocumentVersion.cs
│   ├── DocumentActivity.cs
│   ├── Notification.cs       # Extend document notification types
│   ├── Project.cs            # Add document navigation where useful
│   ├── TaskItem.cs           # Add task-document navigation where useful
│   └── User.cs               # Add document/share/activity navigation where useful
├── Services/
│   ├── DocumentService.cs
│   ├── FileStorageService.cs
│   ├── FileSafetyScanner.cs
│   ├── DocumentShareService.cs
│   ├── DocumentActivityService.cs
│   ├── NotificationService.cs
│   └── DashboardService.cs
├── Pages/
│   ├── Documents.razor
│   ├── DocumentUpload.razor
│   ├── DocumentDetails.razor
│   ├── ProjectDetails.razor
│   ├── Tasks.razor
│   └── Index.razor
├── Shared/
│   └── NavMenu.razor
├── Controllers/              # Protected preview/download endpoint if MVC endpoint is selected
│   └── DocumentFilesController.cs
└── wwwroot/css/site.css

ContosoDashboard.Tests/
├── Services/
├── Storage/
└── Authorization/
```

**Structure Decision**: Keep the existing single-project Blazor Server structure and add a sibling test project. Domain behavior belongs in services and EF entities, while Blazor pages provide the user workflows. A protected controller or equivalent mapped endpoint is required because files are intentionally outside `wwwroot` and must be streamed only after authorization.

## Implementation Boundaries

1. **Data and schema**: Add entities, relationships, indexes, constraints, and seed-safe configuration to `ApplicationDbContext`. Preserve integer keys and text categories.
2. **Storage and safety**: Implement `IFileStorageService`, local provider, generated relative paths, stream handling, allowed-file validation, size checks, and local scanner boundary. Ensure cleanup on failed persistence.
3. **Document business service**: Implement upload, list/search/filter, metadata edit, replacement/versioning, authorization, delete, and activity logging.
4. **Sharing and notifications**: Implement explicit user/team sharing policy, recipient access, duplicate prevention, and document-related notification types through the existing notification service.
5. **Protected file access**: Add preview/download response handling with content type, safe download name, authorization checks, and activity logging.
6. **UI integration**: Add navigation and documents pages, progress/error/success states, filters/sort/search, preview/download/share/edit/delete controls, project document section, task attachment flow, recent widget, and dashboard count.
7. **Verification**: Add service/storage/security tests, build/test commands, and execute the quickstart scenarios with seeded roles.

## Constitution Check: Post-Design

- **Training-first** remains satisfied: no cloud service is required, and the malware scan limitation is explicit.
- **Secure-by-default** remains satisfied: no file is exposed through static files; all access paths use the same service authorization policy and audit activity.
- **Spec-driven delivery** remains satisfied: each design artifact maps directly to the spec's requirements and acceptance scenarios.
- **Verifiable behavior** remains satisfied: the plan introduces a test project and defines manual UI scenarios for browser-only behavior.
- **Simplicity** remains satisfied: relational entities and local EF queries are preferred over external search or storage systems.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| New test project | The repository has no test infrastructure, but document authorization, storage cleanup, and search are security-sensitive and need repeatable verification. | Manual-only validation would not reliably catch IDOR, orphaned files, or authorization regressions. |
| Separate version/activity/tag/share entities | The requirements include custom tags, sharing, replacement, and audit reporting. | Comma-separated tags, overwritten files, and unstructured logs would be difficult to query and validate. |
