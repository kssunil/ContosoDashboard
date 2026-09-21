# Research: Add New Contoso Project

No `NEEDS CLARIFICATION` markers remain in the Technical Context — this feature extends an existing, well-established stack (ASP.NET Core 9 Blazor Server, EF Core 9 SQL Server, existing authorization policies and audit pattern), so research focused on how to fit the spec's clarified decisions into the current codebase rather than choosing new technology.

## Decision: Persist a `Progress` column on `Project`, decoupled from the computed `CompletionPercentage`

- **Decision**: Add a required `int Progress` column (0–100, default 0) to the `Project` entity. Update `Projects.razor` and `ProjectDetails.razor`, which currently render `@project.CompletionPercentage` (a `[NotMapped]` computed property based on task completion ratio), to render `@project.Progress` instead.
- **Rationale**: Clarification 1 established that a manually entered Progress value must stay fixed and must never be overridden by the task-completion calculation. Both existing screens already display a progress bar sourced from `CompletionPercentage` (`ContosoDashboard/Pages/Projects.razor:54`, `ContosoDashboard/Pages/ProjectDetails.razor:57`), so leaving that binding in place would silently ignore what the user entered — directly contradicting FR-009. The only way to honor the clarification is to make the stored value the one thing the UI displays.
- **Alternatives considered**:
  - *Keep `CompletionPercentage` as the display value and store `Progress` only for reporting*: rejected — it reproduces exactly the contradiction the clarification resolved (a backfilled completed project would show 0% despite the PM entering 100%).
  - *Show both values side by side*: rejected in the clarification itself as unnecessary complexity for a training app.
  - *Remove `CompletionPercentage` entirely*: rejected as out of scope — no other feature's behavior should change beyond what's needed to satisfy this feature's requirement; the computed property can remain in the model unused by these two screens without harm.

## Decision: Add `Inactive` to the existing `ProjectStatus` enum

- **Decision**: Add `Inactive` as a new member of `ContosoDashboard.Models.ProjectStatus` (alongside the existing `Planning`, `Active`, `OnHold`, `Completed`). The Create New Project screen's Status control only offers `Active` and `Inactive`, per FR-008.
- **Rationale**: The stakeholder doc and spec require exactly two selectable values at creation (Active/Inactive), but no such enum member exists today. There is no user-facing edit flow for `Project.Status` yet (`ProjectDetails.razor` only displays it; `ProjectService.UpdateProjectAsync` exists but is not called from any page), so adding one enum value is low-risk and keeps a single canonical status type instead of introducing a parallel boolean or string field. Existing status-to-badge-color switches (`GetStatusColor` in `Projects.razor` and `ProjectDetails.razor`) already have a `_ => "secondary"` fallback arm, so they render safely for the new value without a required code change, though an explicit case will be added for clarity.
- **Alternatives considered**:
  - *Separate `IsActive` boolean field*: rejected — would duplicate the concept of status alongside the existing enum and complicate the existing status badge rendering.
  - *Map "Inactive" to the existing `OnHold` value*: rejected — conflates two different business meanings (a project temporarily paused vs. one the stakeholder doc considers not active) and would surprise anyone reading `OnHold` in code expecting the original meaning.

## Decision: Add `CreatedByUserId` to `Project` for audit identity

- **Decision**: Add a required `int CreatedByUserId` foreign key to `User` on `Project`, set from the authenticated user in `ProjectService.CreateProjectAsync`, never from client input.
- **Rationale**: FR-022 requires recording who created a project. `Project.CreatedDate` already exists but there is no creator reference. This follows the exact pattern already used for `Document.UploaderId` and `TaskItem.CreatedByUserId`.
- **Alternatives considered**: *Rely solely on the new `ProjectActivity` audit row for identity*: rejected as the sole mechanism — keeping a direct `CreatedByUserId` column on `Project` matches the existing convention on `Document`/`TaskItem` and makes "who created this project" queryable without a join through an activity log, while the audit row still provides the durable, timestamped record FR-022 also asks for.

## Decision: Default task Assignee/Priority in the service layer, not just the UI

- **Decision**: When a task is added through the New Task modal without an explicit Assignee or Priority, the Create New Project page passes `AssignedUserId = <creator>` and `Priority = TaskPriority.Medium` into the same `TaskItem` construction used elsewhere, before calling the existing `ITaskService.CreateTaskAsync` (or an equivalent in-memory construction persisted as part of the project save).
- **Rationale**: Clarification 2 resolved this exactly. `TaskItem.AssignedUserId` and `TaskItem.Priority` are `[Required]` non-nullable fields (`ContosoDashboard/Models/TaskItem.cs:19,27`), so a task cannot be persisted without them regardless of what the UI collects. Defaulting in the service/page code (not only via a form default) guarantees the rule holds even if the modal's optional fields are left untouched.
- **Alternatives considered**: *Require Assignee/Priority in the modal*: this was the explicit alternative offered in the clarification question and was not chosen, because it conflicts with the stakeholder doc's explicit "Title is required" simplicity goal for the task modal.

## Decision: Stage documents client-side; upload only after project Save succeeds

- **Decision**: The Create New Project page holds selected files as `List<IBrowserFile>` (the same in-memory staging pattern already used in `DocumentUpload.razor`'s `selectedFiles` list) and only calls `IDocumentService.UploadAsync` — passing the newly created project's ID — after `ProjectService.CreateProjectAsync` succeeds. Cancel simply clears the in-memory list; nothing is written to storage or the database.
- **Rationale**: Clarification 3 resolved this exactly, and `Document.ProjectId` is already nullable (`ContosoDashboard/Models/Document.cs:34`), which is what makes deferring the project link until after save straightforward — no schema change is needed for this decision, only sequencing in the page's Save handler.
- **Alternatives considered**: *Upload immediately on selection and delete on Cancel* and *upload immediately and leave orphaned on Cancel*: both were offered as alternatives in the clarification and rejected in favor of the simpler stage-only approach, which needs no cleanup logic and trivially satisfies FR-020's "nothing persisted on Cancel."

## Decision: Add a `ProjectActivity` entity/service mirroring `DocumentActivity`

- **Decision**: Add `ProjectActivity` (`ProjectActivityId`, `ProjectId`, `ActorUserId`, `Action`, `OccurredDate`, `Details`) and `IProjectActivityService`/`ProjectActivityService`, structured exactly like `DocumentActivity`/`DocumentActivityService`. `ProjectService.CreateProjectAsync` calls `ProjectActivityService.AddAsync(project.ProjectId, actorUserId, "ProjectCreated")` after a successful save.
- **Rationale**: FR-022 requires an auditable creation record. The codebase already has an established, tested pattern for this exact need (`ContosoDashboard/Models/DocumentActivity.cs`, `ContosoDashboard/Services/DocumentActivityService.cs`); reusing it keeps the feature consistent with Constitution Principle V (Simplicity) instead of inventing a new logging mechanism.
- **Alternatives considered**: *Log via `Project.CreatedDate`/`CreatedByUserId` alone*: rejected as insufficient for SC-005's "retrievable audit record" if project rows are ever later updated — a dedicated append-only activity row is more durable and consistent with how document activity is already audited.

## Decision: Enforce authorization with the existing `ProjectManager` policy, at both page and service layers

- **Decision**: Apply `@attribute [Authorize(Policy = "ProjectManager")]` on the new `ProjectCreate.razor` page (matching `Program.cs`'s existing policy: `RequireRole("ProjectManager", "Administrator")`), and add an explicit role check inside `ProjectService.CreateProjectAsync` (or a new authorization-aware overload) so a direct call to the service or a future non-UI entry point is still denied for other roles.
- **Rationale**: FR-021 explicitly requires server-side enforcement independent of UI visibility (IDOR-style protection, per the constitution's Secure-by-Default principle). The `ProjectManager` policy already exists and already means exactly "ProjectManager or Administrator" (`ContosoDashboard/Program.cs:37`), so no new policy is needed.
- **Alternatives considered**: *UI-only gating (hide the button)*: explicitly rejected by FR-021 and by the constitution's authorization requirements.

## Decision: Enforce Title uniqueness with a case-insensitive unique database index plus a service-level pre-check

- **Decision**: Add a unique index on `Project.Name` in `ApplicationDbContext`. Because the SQL Server/LocalDB default collation used by this project is case-insensitive (`SQL_Latin1_General_CP1_CI_AS`), a standard unique index enforces FR-006's case-insensitive uniqueness without extra normalization columns. `ProjectService.CreateProjectAsync` performs an explicit pre-check (trimmed, case-insensitive comparison) so the user gets FR-019's friendly validation message rather than a raw database constraint exception, with the index as a defense-in-depth guarantee against races.
- **Rationale**: Matches the existing pattern for `User.Email` (`ApplicationDbContext.cs:69-70`, `HasIndex(u => u.Email).IsUnique()`), keeping uniqueness enforcement consistent across the codebase.
- **Alternatives considered**: *Application-level check only, no index*: rejected — leaves a race-condition gap under concurrent creates, which the constitution's security posture guidance says to avoid where an inexpensive index-based guarantee is available.
