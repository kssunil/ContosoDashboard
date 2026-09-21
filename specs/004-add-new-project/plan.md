# Implementation Plan: Add New Contoso Project

**Branch**: `004-add-new-project` | **Date**: 2026-09-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/004-add-new-project/spec.md`

## Summary

Add a self-service "Create New Project" screen to the existing ContosoDashboard Blazor Server application so Project Managers and Administrators can create a project (metadata, initial tasks, documents, team members) without an IT data-fix request. The implementation extends the existing `Project`/`TaskItem`/`Document` EF Core entities with the fields the create flow needs (a persisted `Progress` value, an `Inactive` status option, and a `CreatedByUserId` audit column), reuses the existing `ProjectManager` authorization policy for both page access and server-side enforcement, stages selected task and document entries client-side in the Blazor component until Save succeeds, and records a `ProjectActivity` audit row on every successful creation — mirroring the existing `DocumentActivity` pattern. No new services, external APIs, or storage technologies are introduced.

## Technical Context

**Language/Version**: C# with ASP.NET Core / Blazor Server targeting .NET 9.0 (matches `ContosoDashboard.csproj`)
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 9 SQL Server provider, existing cookie-based mock authentication (`CustomAuthenticationStateProvider`), existing `ProjectManager` authorization policy, Bootstrap 5.3
**Storage**: Existing SQL Server/LocalDB database via `ApplicationDbContext` — no new storage technology; reuses `Projects`, `Tasks`, `Documents`, `ProjectMembers` tables and adds one new `ProjectActivities` table plus new columns on `Project`
**Testing**: Existing `ContosoDashboard.Tests` project (xUnit, bUnit, EF Core InMemory) — add service tests for creation/authorization/uniqueness/audit and component tests for the Create New Project page
**Target Platform**: Windows offline training environment with local SQL Server/LocalDB and browser
**Project Type**: Single web application (existing Blazor Server UI + service/data layers) — no separate frontend/backend split
**Performance Goals**: Save completes and returns the user to My Projects in under 2 seconds under normal local/training load; matches existing page navigation expectations elsewhere in the app
**Constraints**: No cloud services or Azure SDK at runtime; authorization enforced server-side in the service layer, not only via UI visibility; must not regress the existing computed-progress display for projects created before this feature (none exist without a `Progress` value, since the migration backfills it — see [research.md](research.md)); preserve mock-auth, offline, training-only scope per the constitution
**Scale/Scope**: Existing seeded users/projects (a handful) plus new projects created through this flow; one application, one database — no multi-tenant or high-volume scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Training-First Scope**: PASS. No cloud services, external APIs, or production identity are introduced; the feature stays within the existing offline, mock-authenticated Blazor Server app.
- **II. Secure-by-Default Learning**: PASS. FR-021 requires server-side authorization independent of UI visibility. The plan enforces this via the existing `ProjectManager` policy at the page level (`@attribute [Authorize(Policy = "ProjectManager")]`) and a matching role check inside `ProjectService.CreateProjectAsync`, so a direct service/request-level bypass is still denied — consistent with the IDOR-protection pattern already used in `ProjectService`/`DocumentService`.
- **III. Spec-Driven Delivery**: PASS. Every functional requirement (FR-001…FR-022) and the three clarified decisions map to a specific entity change, service behavior, or UI behavior documented in [data-model.md](data-model.md) and the contracts below.
- **IV. Test-First and Verifiable Behavior**: PASS WITH FOLLOW-UP. The repository already has `ContosoDashboard.Tests`; this plan adds service-layer tests (uniqueness, date bounds, authorization, audit logging, task/document defaults) and bUnit component tests for the Create New Project page before/alongside implementation, following the existing `Authorization/` and `Services/` test folder conventions.
- **V. Simplicity, Clarity, and Maintainability**: PASS. The plan extends existing `Models/`, `Data/`, `Services/`, `Pages/` patterns (mirroring `DocumentActivity`/`DocumentActivityService` for audit logging and the existing `IBrowserFile` staging pattern from `DocumentUpload.razor`) rather than introducing a new framework, background job, or external dependency.

No violations requiring justification — Complexity Tracking is not needed.

## Phase 0: Research Decisions

Research findings and alternatives are recorded in [research.md](research.md). The key resolved decisions are: add a persisted `Progress` column to `Project` that is never recalculated from task completion (per Clarification 1) and update the two existing progress-bar displays to read it instead of `CompletionPercentage`; add an `Inactive` value to the existing `ProjectStatus` enum rather than introducing a parallel status type; add a required `CreatedByUserId` column to `Project` for audit purposes; default a new task's `AssignedUserId` to the creator and `Priority` to `Medium` when not set in the modal (per Clarification 2); stage selected documents as `IBrowserFile` objects in the page (same pattern already used in `DocumentUpload.razor`) and only call `DocumentService.UploadAsync` after the project save succeeds (per Clarification 3); and add a `ProjectActivity` entity/service mirroring `DocumentActivity`/`DocumentActivityService` for the FR-022 audit log.

## Phase 1: Design Artifacts

- [data-model.md](data-model.md) defines the `Project` field additions, the new `ProjectActivity` entity, validation rules, and how `TaskItem`/`Document`/`ProjectMember` are associated during creation.
- [contracts/create-project-ui.md](contracts/create-project-ui.md) documents the Create New Project screen's fields, controls, validation messages, and navigation behavior as an internal UI contract (no external HTTP API is exposed by this feature).
- [contracts/create-project-service.md](contracts/create-project-service.md) documents the service-layer contract (`CreateProjectAsync` inputs/outputs, authorization enforcement, validation order, and audit logging) that both the UI and its tests rely on.
- [quickstart.md](quickstart.md) defines runnable manual and automated validation scenarios using the app's seeded users.

## Project Structure

### Documentation (this feature)

```text
specs/004-add-new-project/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── contracts/
│   ├── create-project-ui.md
│   └── create-project-service.md
├── quickstart.md
├── checklists/requirements.md
└── tasks.md                 # Created by /speckit.tasks
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs      # Add Progress/CreatedByUserId columns, unique Title index, ProjectActivities table, seed migration
├── Models/
│   ├── Project.cs                   # Add Progress, CreatedByUserId; add Inactive to ProjectStatus
│   ├── TaskItem.cs                  # No structural change; creation defaults applied in service
│   ├── Document.cs                  # No structural change; ProjectId already nullable, supports post-save linking
│   └── ProjectActivity.cs           # New: mirrors DocumentActivity
├── Services/
│   ├── ProjectService.cs            # Extend CreateProjectAsync: uniqueness, date-window validation, authorization, audit call, and single-SaveChangesAsync persistence of the project together with its tasks, documents, and team members (no separate post-save calls, per FR-018)
│   ├── ProjectActivityService.cs    # New: mirrors DocumentActivityService
│   ├── TaskService.cs               # Not called during project creation — task persistence is inlined in ProjectService.CreateProjectAsync's single save (see below) to preserve FR-018 atomicity; ITaskService.CreateTaskAsync remains for the existing standalone task-creation flow
│   └── DocumentService.cs           # File-write logic reused from within ProjectService.CreateProjectAsync's single save operation, with cleanup of written files if that save fails (not a separate call after the project is created)
├── Pages/
│   ├── Projects.razor               # Add "New Project" button (role-gated), read project.Progress instead of CompletionPercentage
│   ├── ProjectDetails.razor         # Read project.Progress instead of CompletionPercentage
│   └── ProjectCreate.razor          # New: Create New Project screen (metadata form, task table + modal, document staging, team multi-select, Save/Cancel)
└── Shared/
    └── NavMenu.razor                # No change expected; existing "My Projects" link already routes here

ContosoDashboard.Tests/
├── Services/
│   └── ProjectServiceCreationTests.cs   # New
├── Authorization/
│   └── ProjectCreationAuthorizationTests.cs   # New
└── Pages/ (or existing bUnit test location)
    └── ProjectCreateTests.cs            # New
```

**Structure Decision**: Keep the existing single-project Blazor Server structure. All new behavior lives in the existing `Models/`, `Services/`, `Pages/` folders alongside the equivalent document-upload and task features; no new project, controller, or external API is introduced. Tests follow the existing `ContosoDashboard.Tests` folder convention (`Services/`, `Authorization/`).

## Constitution Check: Post-Design

- **Training-first** remains satisfied: all new entities and services are local EF Core additions; no external dependency was introduced during design.
- **Secure-by-default** remains satisfied: `contracts/create-project-service.md` specifies that authorization is re-checked inside the service regardless of the calling page, matching the existing `ProjectService`/`DocumentService` pattern and satisfying FR-021/SC-004.
- **Spec-driven delivery** remains satisfied: `data-model.md` and both contracts trace directly back to FR-001…FR-022 and the three clarifications.
- **Verifiable behavior** remains satisfied: `quickstart.md` gives runnable scenarios per user story, and the test plan adds service and authorization tests before/alongside the UI.
- **Simplicity** remains satisfied: no new framework, background job, or storage technology; the design reuses the `DocumentActivity` audit pattern and the `IBrowserFile` staging pattern already present in the codebase.

## Complexity Tracking

*No violations to justify — this section is intentionally empty.*
