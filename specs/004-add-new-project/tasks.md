# Tasks: Add New Contoso Project

**Input**: Design documents from `specs/004-add-new-project/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)
**Tests**: Included because the project constitution requires test-first, verifiable behavior for security-sensitive features (this feature adds server-side authorization and an audit trail, per FR-021/FR-022).

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the one new test location this feature needs; everything else reuses the existing solution structure.

- [X] T001 [P] Create the `ContosoDashboard.Tests/Pages/` test directory for new page-level component tests, mirroring the existing `ContosoDashboard.Tests/Shared/`, `Services/`, `Authorization/`, and `Storage/` convention.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the schema, entities, and audit service every user story depends on.

**Critical**: No user story task can be completed until this phase is complete.

- [X] T002 [P] Add a required `Progress` property (`int`, range 0–100, default 0, per data-model.md) and a required `CreatedByUserId` property (`int`, FK to `User`) to `Project`, and add `Inactive` as a new member of the `ProjectStatus` enum, in `ContosoDashboard/Models/Project.cs`.
- [X] T003 [P] Create the `ProjectActivity` entity (`ProjectActivityId` PK, required `ProjectId` FK to `Project`, required `ActorUserId` FK to `User`, required `Action` text, `OccurredDate` UTC datetime, optional bounded `Details` text) and a `ProjectActivityActions` static class with a `ProjectCreated` constant, in `ContosoDashboard/Models/ProjectActivity.cs`, mirroring `ContosoDashboard/Models/DocumentActivity.cs`.
- [X] T004 Add a `DbSet<ProjectActivity> ProjectActivities`, a unique index on `Project.Name` (FR-006 uniqueness), and the `ProjectActivity` foreign-key/relationship configuration in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [X] T005 Update the existing seeded `Project.HasData` block (ProjectId 1) in `ContosoDashboard/Data/ApplicationDbContext.cs` to supply values for the new required `Progress` (e.g., 0) and `CreatedByUserId` (e.g., the existing `ProjectManagerId`) columns, so model building does not fail on the new required fields.
- [X] T006 [P] Create `ContosoDashboard/Data/ProjectSchemaInitializer.cs` with idempotent raw-SQL schema patches — `IF COL_LENGTH(N'Projects', N'Progress') IS NULL ... ALTER TABLE [Projects] ADD [Progress] int NOT NULL CONSTRAINT [DF_Projects_Progress] DEFAULT 0`, an equivalent guarded `ALTER TABLE` for `CreatedByUserId`, a guarded `CREATE UNIQUE INDEX` on `[Projects]([Name])`, and an `IF OBJECT_ID(N'[ProjectActivities]', N'U') IS NULL ... CREATE TABLE [ProjectActivities] (...)` block — mirroring the pattern in `ContosoDashboard/Data/DocumentSchemaInitializer.cs`.
- [X] T007 Call `ProjectSchemaInitializer.EnsureCreated(context)` alongside the existing `DocumentSchemaInitializer.EnsureCreated(context)` call, and register `IProjectActivityService`/`ProjectActivityService` for dependency injection, in `ContosoDashboard/Program.cs`.
- [X] T008 [P] Create `IProjectActivityService` and `ProjectActivityService` (an `AddAsync(projectId, actorUserId, action, details, cancellationToken)` method and a `GetAsync` method scoped to authorized viewers) in `ContosoDashboard/Services/ProjectActivityService.cs`, mirroring `ContosoDashboard/Services/DocumentActivityService.cs`.

**Checkpoint**: Schema, entities, and the audit service are ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Create and save a new project (Priority: P1) 🎯 MVP

**Goal**: A Project Manager or Administrator can open the Create New Project screen, enter required metadata, and save — the project is created with a correct, non-overridden Progress value and an audit record, or a specific validation message is shown and nothing is lost.

**Independent Test**: Log in as a Project Manager, create a project with only the required metadata fields filled in, save, and confirm the project appears in My Projects with Progress displayed exactly as entered (or 0 if left blank).

### Tests for User Story 1

- [X] T009 [P] [US1] Add `ProjectServiceCreationTests` covering: Title required and unique case-insensitively after trimming (FR-006); Description required (FR-007); ProjectManagerId required, and that ProjectManagerId references an existing user with the ProjectManager role; Status limited to Active/Inactive (FR-008); Progress must be 0–100, defaults to 0 when omitted, and is never recalculated after save (FR-009, Clarification 1); Start Date required and rejected when more than 12 months in the past (FR-010); End Date required and rejected when earlier than Start Date (FR-011); and a successful save sets `CreatedByUserId`/`CreatedDate` and writes one `ProjectActivity` row with `Action = "ProjectCreated"` (FR-022) — in `ContosoDashboard.Tests/Services/ProjectServiceCreationTests.cs`.

### Implementation for User Story 1

- [X] T010 [US1] Extend `IProjectService.CreateProjectAsync` (or add an overload accepting a creation request) in `ContosoDashboard/Services/ProjectService.cs` to validate, in order, Title presence/uniqueness, ProjectManagerId presence and that ProjectManagerId references an existing user with the ProjectManager role, Description presence, Status value, Progress range/default, Start Date's 12-month window, and End Date ≥ Start Date, returning a typed result that carries a specific failure message for whichever rule fails first, and setting `CreatedByUserId`, `CreatedDate`, and `UpdatedDate` on success.
- [X] T011 [US1] Call `ProjectActivityService.AddAsync(project.ProjectId, actorUserId, ProjectActivityActions.ProjectCreated)` immediately after a successful save inside `CreateProjectAsync`, in `ContosoDashboard/Services/ProjectService.cs` (depends on T008, T010).
- [X] T012 [US1] Create `ContosoDashboard/Pages/ProjectCreate.razor` at route `/projects/create` with `@attribute [Authorize(Policy = "ProjectManager")]` and labeled controls for Title, Description, Status (Active/Inactive dropdown), Progress (numeric 0–100), Project Manager (single-select dropdown listing all users with the ProjectManager role), Start Date, End Date, and Save/Cancel buttons.
- [X] T013 [US1] Wire the Save button in `ContosoDashboard/Pages/ProjectCreate.razor` to call the extended `CreateProjectAsync`: on failure, display the returned message and keep all entered field values; on success, navigate to `/projects`. Wire Cancel to navigate to `/projects` without calling any save method (depends on T010, T012). Later stories (T020, T023, T026) extend the single request passed to this same call rather than adding separate post-save calls, so the project, tasks, documents, and team members commit as one unit per FR-018.
- [X] T014 [US1] Update `ContosoDashboard/Pages/Projects.razor` and `ContosoDashboard/Pages/ProjectDetails.razor` to bind their progress bars to `@project.Progress` instead of the computed `@project.CompletionPercentage` (depends on T002).

**Checkpoint**: User Story 1 is independently functional — a Project Manager can create and save a project with correct validation, defaulting, and audit logging, and the stored Progress value displays exactly as entered.

---

## Phase 4: User Story 2 - Restrict project creation to authorized roles (Priority: P1)

**Goal**: Employees and Team Leads cannot see the "New Project" button, cannot reach the Create New Project screen, and cannot create a project by calling the service directly; Project Managers and Administrators can.

**Independent Test**: Log in as an Employee and as a Team Lead; confirm neither the "New Project" button nor the Create New Project screen is reachable, including via direct navigation or a direct service call.

### Tests for User Story 2

- [X] T015 [P] [US2] Add `ProjectCreationAuthorizationTests` covering: `CreateProjectAsync` denies a caller whose role is Employee or Team Lead (no project, task, document, or `ProjectActivity` row is created) and succeeds for ProjectManager and Administrator callers, exercised by calling the service directly (bypassing any page/UI layer) — in `ContosoDashboard.Tests/Authorization/ProjectCreationAuthorizationTests.cs`.

### Implementation for User Story 2

- [X] T016 [US2] Add an explicit role check inside `CreateProjectAsync` — independent of the page's `[Authorize]` attribute — that returns a denial result for any caller whose role is not ProjectManager or Administrator, in `ContosoDashboard/Services/ProjectService.cs` (FR-021).
- [X] T017 [US2] Show the "New Project" button (linking to `/projects/create`) on `ContosoDashboard/Pages/Projects.razor` only when the current user's role, read from the existing authentication state, is ProjectManager or Administrator (FR-001, FR-002).

**Checkpoint**: User Stories 1 and 2 together deliver the secured MVP: only ProjectManager/Administrator users can see, reach, or successfully call project creation, by any path.

---

## Phase 5: User Story 3 - Add project tasks during creation (Priority: P2)

**Goal**: While creating a project, a Project Manager can add one or more tasks via a modal; each added task requires only a Title and is saved with the project.

**Independent Test**: While creating a new project, add one or more tasks via the task table before saving, then confirm the tasks are attached to the project after save, with a default Assignee and Priority when not explicitly set.

### Tests for User Story 3

- [X] T018 [P] [US3] Add tests to `ContosoDashboard.Tests/Services/ProjectServiceCreationTests.cs` covering: a staged task with only a Title is rejected if Title is blank; a staged task with a Title but no Assignee/Priority is persisted with `AssignedUserId` defaulted to the creator and `Priority` defaulted to `Medium` (Clarification 2); and staged tasks are persisted only as part of a successful project save.

### Implementation for User Story 3

- [X] T019 [US3] Add a project task table (presented consistently with the existing task table on `ContosoDashboard/Pages/ProjectDetails.razor`) and a "New Task" control that opens a modal with Title (required) plus optional Description, Priority, Status, Due Date, and Assignee, with Ok (validates Title, adds the row, closes) and Cancel (discards, closes) actions, in `ContosoDashboard/Pages/ProjectCreate.razor` (FR-012, FR-013, FR-014).
- [X] T020 [US3] Extend `CreateProjectAsync`'s request handling in `ContosoDashboard/Services/ProjectService.cs` to accept a list of staged tasks, defaulting `AssignedUserId` to the creator and `Priority` to `Medium` when not supplied, and persist each as a `TaskItem` linked to the new project on success (depends on T010).

**Checkpoint**: User Stories 1–3 are independently functional; a project can be created with an initial task list.

---

## Phase 6: User Story 4 - Upload supporting documents during creation (Priority: P2)

**Goal**: While creating a project, a Project Manager can select files to attach; nothing is written to storage until the project save succeeds.

**Independent Test**: While creating a new project, use "Upload Documents" to select one or more files, confirm they appear in the documents table, then save and confirm the documents are associated with the created project — and confirm Cancel leaves no stored file or Document row behind.

### Tests for User Story 4

- [X] T021 [P] [US4] Add tests confirming that staged-but-uncommitted document selections create no `Document` row and no stored file, that a failure elsewhere in `CreateProjectAsync` (e.g., a validation error) leaves no file written to storage, and that staged documents are written to storage and persisted as `Document` rows linked to the new project within the same `CreateProjectAsync` call that creates the project (not via a separate post-save step), in `ContosoDashboard.Tests/Services/ProjectServiceCreationTests.cs`.

### Implementation for User Story 4

- [X] T022 [US4] Add an "Upload Documents" control and a staged-documents table backed by an in-memory `List<IBrowserFile>` (mirroring the `selectedFiles` staging pattern in `ContosoDashboard/Pages/DocumentUpload.razor`) to `ContosoDashboard/Pages/ProjectCreate.razor` (FR-015).
- [X] T023 [US4] Extend `CreateProjectAsync`'s request handling in `ContosoDashboard/Services/ProjectService.cs` to accept the staged file content (stream, original filename, content type) for each document: within the same call, write each file via `IFileStorageService` and add the resulting `Document` rows to the same `DbContext` as the `Project`, its tasks, and its team members so everything commits in one `SaveChangesAsync`; if that save fails, delete any files already written before returning the failure result (mirroring the cleanup pattern already used in `DocumentService.UploadAsync`). Wire `ContosoDashboard/Pages/ProjectCreate.razor` to pass the staged `IBrowserFile` content into this single call instead of calling `IDocumentService.UploadAsync` separately after the project is created; Cancel continues to clear the staged list without calling `CreateProjectAsync` at all (depends on T010, T020, T022; supersedes a separate post-save upload step so FR-018's single-logical-unit guarantee holds; Clarification 3).

**Checkpoint**: User Stories 1–4 are independently functional; a project can be created with initial tasks and documents.

---

## Phase 7: User Story 5 - Assign team members during creation (Priority: P3)

**Goal**: While creating a project, a Project Manager can select team members from a multi-select control; selections are shown as comma-separated text and persisted with the project.

**Independent Test**: While creating a new project, select multiple team members from the multi-select control, confirm they render as comma-separated text, save, and confirm the assignments are attached to the created project.

### Tests for User Story 5

- [X] T024 [P] [US5] Add tests confirming selected team members are persisted as `ProjectMember` rows within the same `CreateProjectAsync` call that creates the project (not via a separate post-save step), and that a failure elsewhere in that call (e.g., a validation error) leaves no `ProjectMember` rows behind, in `ContosoDashboard.Tests/Services/ProjectServiceCreationTests.cs`.

### Implementation for User Story 5

- [X] T025 [US5] Add a multi-select team member control and, below it, a live comma-separated summary of the current selection, to `ContosoDashboard/Pages/ProjectCreate.razor` (FR-016).
- [X] T026 [US5] Extend `CreateProjectAsync`'s request handling in `ContosoDashboard/Services/ProjectService.cs` to accept the selected team member user IDs and add the resulting `ProjectMember` rows to the same `DbContext` as the `Project`, its tasks, and its documents so everything commits in one `SaveChangesAsync`. Wire `ContosoDashboard/Pages/ProjectCreate.razor` to pass the current multi-select value into this single call instead of calling `ProjectService.AddProjectMemberAsync` separately after the project is created (depends on T010, T020, T025; supersedes a separate post-save assignment step so FR-018's single-logical-unit guarantee holds).

**Checkpoint**: All five user stories are independently functional and demonstrable together as the complete feature.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Round out verification and presentation once all stories are complete.

- [X] T027 [P] Add bUnit component tests for `ProjectCreate.razor` covering: role-gated rendering (visible for ProjectManager/Administrator, inaccessible otherwise); validation-error display with preserved field values; successful-save navigation to `/projects`; the New Task modal rejecting a blank Title by showing a validation message, keeping the modal open, and adding no row (FR-014); and the team-member multi-select's comma-separated summary updating live as the selection changes (FR-016) — in `ContosoDashboard.Tests/Pages/ProjectCreateTests.cs`.
- [X] T028 [P] Add form, task-modal, and staged-document-table styling consistent with the existing form and modal styles in `ContosoDashboard/wwwroot/css/site.css`.
- [X] T029 Execute all 10 scenarios in `specs/004-add-new-project/quickstart.md` using the seeded users (`admin@contoso.com`, `camille.nicole@contoso.com`, `floris.kregel@contoso.com`, `ni.kang@contoso.com`) and record results.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **User Stories (Phase 3–7)**: All depend on Foundational completion.
  - US1 and US2 are both P1 and together form the secured MVP; US1 delivers the create/save mechanics, US2 hardens access to it. Both should be complete before any demo.
  - US3, US4, and US5 each add an independent capability to the same `ProjectCreate.razor` page and the same `CreateProjectAsync` method, so within a single-developer flow they are easiest to build sequentially in priority order, even though each is independently testable.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### Within Each User Story

- Tests are written before implementation for that story.
- Foundational data/entities before service logic; service logic before UI wiring.
- Story complete and checkpoint-verified before moving to the next priority.

### Parallel Opportunities

- T002, T003, T006, and T008 (Foundational) touch different files and can run in parallel; T004 and T005 both edit `ApplicationDbContext.cs` and must be sequential after T002/T003; T007 depends on T006 and T008.
- T009 (US1 tests) can be written in parallel with Foundational work once the entities it references (T002, T003) are defined.
- T015, T018, T021, and T024 (tests for US2–US5) can each be written in parallel with their preceding story's implementation, since they target the same shared test file incrementally but describe independent scenarios.
- T020, T023, and T026 all extend the same `CreateProjectAsync` request/method in `ProjectService.cs`, so they are not parallelizable with each other — implement them sequentially in story-priority order (US3 → US4 → US5) so each builds on the request shape the previous one introduced.
- T027 and T028 (Polish) can run in parallel with each other.

---

## Parallel Example: Foundational Phase

```bash
# Launch independent Foundational file creation together:
Task: "Add Progress/CreatedByUserId/Inactive to ContosoDashboard/Models/Project.cs"
Task: "Create ProjectActivity entity in ContosoDashboard/Models/ProjectActivity.cs"
Task: "Create ProjectSchemaInitializer.cs with idempotent schema patches"
Task: "Create IProjectActivityService/ProjectActivityService in ContosoDashboard/Services/ProjectActivityService.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (critical — blocks all stories).
3. Complete Phase 3: User Story 1.
4. Complete Phase 4: User Story 2.
5. **STOP and VALIDATE**: run Scenarios 1, 2, 3, 4, 9, and 10 from `quickstart.md` — a secured, working create/save flow is the MVP.
6. Deploy/demo if ready.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 + US2 → secured MVP → validate → demo.
3. US3 → validate task-adding → demo.
4. US4 → validate document staging → demo.
5. US5 → validate team-member assignment → demo.
6. Polish → full validation pass.

---

## Notes

- [P] tasks = different files, no unresolved dependencies.
- [Story] label maps each task to its user story for traceability.
- US1 and US2 share priority P1 because access control is inseparable from a safe MVP; they are listed as separate phases only because the spec keeps them independently testable.
- Commit after each task or logical group.
- Stop at any checkpoint to validate a story independently before continuing.
