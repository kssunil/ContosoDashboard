# Service Contract: Project Creation

## Boundary

This is an internal application contract (no external HTTP API is exposed by this feature). It governs the behavior of `IProjectService.CreateProjectAsync` and its collaborators (`ITaskService`, `IDocumentService`, `IProjectActivityService`) when invoked from `ProjectCreate.razor`, and is what the new service and authorization tests validate against.

## Authorization

- The caller's role MUST be re-validated inside the service (or an authorization-aware entry point it calls), independent of any page-level `[Authorize]` attribute (FR-021).
- Only `ProjectManager` and `Administrator` roles may successfully create a project. Any other role, or an unauthenticated/unresolvable caller, MUST result in a denial — the method returns a failure result and no rows are written to `Projects`, `Tasks`, `Documents`, `ProjectMembers`, or `ProjectActivities`.

## Input Shape (conceptual — implementation may use a request DTO or the `Project` entity directly)

- Title (string, required)
- Description (string, required)
- Status (Active or Inactive)
- Progress (int, 0–100, optional — defaults to 0)
- ProjectManagerId (int, required — must reference an existing user with the ProjectManager role)
- StartDate (date, required)
- EndDate (date, required)
- Tasks (list, each with Title required; Description/Priority/Status/DueDate/AssignedUserId optional)
- Team member user IDs (list, optional)
- Staged document references (list of already-selected `IBrowserFile`-equivalent content, optional)
- ActorUserId (int, required — the authenticated creator, never trusted from client-editable input)

## Validation Order and Failure Behavior

Validation MUST be checked server-side even though the UI performs the same checks, and MUST fail closed (no partial writes) on the first violation encountered:

1. Authorization (see above).
2. Title present and, after trimming, unique case-insensitively across all projects (FR-006).
3. Description present (FR-007).
4. Status is Active or Inactive (FR-008).
5. Progress within 0–100 if provided (FR-009).
6. StartDate present and not more than 12 months before the current date (FR-010).
7. EndDate present and not earlier than StartDate (FR-011).
8. ProjectManagerId references an existing user with the ProjectManager role (FR-005).
9. Each task's Title is present; Assignee defaults to ActorUserId and Priority defaults to Medium when not supplied (FR-014, Clarification 2).

Any failure returns a result the caller can render as a specific, user-friendly message (FR-019) — no exception should propagate to the UI as a raw/unhandled error for expected validation failures.

## Success Behavior

On passing all validation:

1. Persist the `Project` row with `CreatedByUserId = ActorUserId` and `CreatedDate`/`UpdatedDate` set by the service (never from client input).
2. Persist each staged task, associated to the new `ProjectId`.
3. For each staged document, invoke the existing upload path with the new `ProjectId` so the document is written to storage and recorded, associated with the project.
4. Persist `ProjectMember` rows for each selected team member.
5. Record a `ProjectActivity` row: `Action = "ProjectCreated"`, `ActorUserId = ActorUserId`, `ProjectId = <new project id>`, `OccurredDate = now` (FR-022).
6. Return a success result containing the new project's identity so the UI can navigate to My Projects.

Steps 1–5 are treated as a single logical unit: if any step fails, the caller MUST NOT be left with a partially created project (e.g., a project row with no matching audit entry, or tasks with no owning project). This matches the "all-or-nothing" assumption already documented in the spec.

## Idempotency / Concurrency

- A unique index on `Project.Name` (see [data-model.md](../data-model.md)) is the final guard against a race between two concurrent creates with the same title; the service's pre-check handles the common case with a friendly message, and a database constraint violation on the rare race MUST also be translated into the same friendly "title already in use" failure rather than surfacing a raw database error.

## Consumers of This Contract

- `ProjectCreate.razor` (primary caller).
- `ProjectServiceCreationTests` and `ProjectCreationAuthorizationTests` (verify this contract directly, without going through the UI).
