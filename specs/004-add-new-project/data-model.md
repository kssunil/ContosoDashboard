# Data Model: Add New Contoso Project

This feature extends three existing entities (`Project`, `TaskItem`, `Document`) and adds one new entity (`ProjectActivity`). No existing field is removed or repurposed; `TaskItem` and `Document` require no structural change — only new usage patterns during creation.

## Project (extended)

Represents the project being created, and every existing project once the migration below backfills it.

| Field | Type/constraint | Rules |
|---|---|---|
| ProjectId | integer, primary key | Existing; unchanged |
| Name | string, required, max 255 | Existing. **New**: unique index (case-insensitive via default collation); service pre-checks trimmed, case-insensitive duplicates before save (FR-006) |
| Description | string, optional, max 2000 | Existing column is nullable; **new rule**: required (non-empty) for projects created through this feature (FR-007) |
| ProjectManagerId | integer, required, FK to User | Existing; selected from the single-select dropdown of all users with the ProjectManager role (FR-005) |
| StartDate | UTC datetime, required | Existing. **New rule**: may be in the past but not more than 12 months before the current date (FR-010) |
| TargetCompletionDate | UTC datetime, optional in schema | **New rule for this feature**: required at creation and must not be earlier than StartDate (FR-011); referred to as "End Date" in the UI |
| Status | `ProjectStatus` enum, required | Existing enum gains a new member: `Inactive` (see below). Creation screen offers only `Active`/`Inactive` (FR-008) |
| **Progress** *(new column)* | integer, required, range 0–100, default 0 | New persisted value (FR-009). Defaults to 0 if left blank. Stored and displayed as-is; never recalculated from task completion (Clarification 1) |
| **CreatedByUserId** *(new column)* | integer, required, FK to User | New. Set from the authenticated user creating the project, never from client input (FR-022) |
| CreatedDate | UTC datetime, required | Existing; already set by `CreateProjectAsync` |
| UpdatedDate | UTC datetime, required | Existing |

**`ProjectStatus` enum change**: add `Inactive` alongside the existing `Planning`, `Active`, `OnHold`, `Completed`. Only `Active` and `Inactive` are offered by the Create New Project screen; the other values remain valid for existing/future flows outside this feature's scope.

**Migration note**: existing seeded/production-like rows need a backfill default for the two new required columns — `Progress = 0` (matches the existing computed value's typical starting state) and `CreatedByUserId` set to the existing `ProjectManagerId` for pre-existing rows (the closest available approximation, since no creator was previously recorded).

## TaskItem (no structural change — new usage rules during project creation)

| Aspect | Rule |
|---|---|
| Title | Required in the New Task modal (FR-014); maps directly to the existing required `Title` field |
| AssignedUserId | If not explicitly set in the modal, defaults to the project's creator (Clarification 2) |
| Priority | If not explicitly set in the modal, defaults to `Medium` (Clarification 2) |
| ProjectId | Set to the new project's ID once the project is saved; tasks added during creation are only persisted as part of a successful Save (FR-018) |
| CreatedByUserId | Set to the project's creator, consistent with existing task-creation behavior |

No new fields or constraints are added to `TaskItem`. The existing `[Required]` constraints on `AssignedUserId` and `Priority` are why the defaulting rule above is necessary — without it, a task entered with only a Title could not be persisted.

## Document (no structural change — new staging rule during project creation)

| Aspect | Rule |
|---|---|
| Selection | Files selected via "Upload Documents" are held client-side (`IBrowserFile` list in the page component) and shown in the documents table (FR-015) |
| Persistence timing | Not written to storage or the database until the project Save succeeds (Clarification 3); Cancel discards the in-memory selection with nothing to clean up |
| ProjectId | Already nullable on `Document` — set to the new project's ID at upload time, immediately after the project row exists |
| UploaderId | Set to the project's creator |

No new fields or constraints are added to `Document`. `Document.ProjectId` being nullable is what makes deferring the link until after save possible without a schema change.

## ProjectMember (no structural change)

Team members selected via the multi-select control become `ProjectMember` rows once the project is saved (FR-016, FR-018), using the existing `ProjectMember` shape (`ProjectId`, `UserId`, `Role`, `AssignedDate`) already used by `ProjectService.AddProjectMemberAsync`.

## ProjectActivity (new entity)

Represents an auditable project action, mirroring the existing `DocumentActivity` pattern.

- `ProjectActivityId`: integer primary key.
- `ProjectId`: required foreign key to `Project`.
- `ActorUserId`: required foreign key to `User`.
- `Action`: required text value. Initial value used by this feature: `ProjectCreated`.
- `OccurredDate`: UTC datetime, set by the service.
- `Details`: optional bounded text for additional non-sensitive context (e.g., initial task/document/team-member counts).

## Relationships

- User 1-to-many Project through the existing `ProjectManagerId`, and now also through the new `CreatedByUserId`.
- Project 1-to-many TaskItem, Document, ProjectMember, and (new) ProjectActivity — all existing relationships except ProjectActivity, which follows the same shape as Project's other child collections.
- User 1-to-many ProjectActivity through `ActorUserId`.

## Validation Rules Summary (traceability to functional requirements)

| Rule | Source |
|---|---|
| Title required, unique (case-insensitive, trimmed) | FR-006 |
| Description required | FR-007 |
| Status limited to Active/Inactive at creation | FR-008 |
| Progress 0–100, default 0, never recalculated | FR-009, Clarification 1 |
| Start Date required, ≤ 12 months in the past | FR-010 |
| End Date required, ≥ Start Date | FR-011 |
| Task Title required; Assignee/Priority default when unset | FR-014, Clarification 2 |
| Documents staged, not persisted until Save | FR-015, Clarification 3 |
| Save persists project + tasks + documents + team members together | FR-018 |
| Cancel persists nothing | FR-020 |
| Server-side authorization independent of UI | FR-021 |
| Creator identity + timestamp recorded | FR-022 |

## Authorization Rules

- Only users with the `ProjectManager` or `Administrator` role (existing `ProjectManager` authorization policy) may reach the Create New Project page or successfully call the creation service method.
- The service re-validates the caller's role independently of page-level `[Authorize]` attributes, so a direct or future non-UI call path cannot bypass the restriction (FR-021).
