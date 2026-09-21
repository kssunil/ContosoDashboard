# Quickstart Validation: Add New Contoso Project

## Prerequisites

- Windows with the .NET SDK compatible with the repository's `net9.0` target.
- SQL Server LocalDB or the configured local SQL Server instance, with migrations applied (including this feature's `Project.Progress`, `Project.CreatedByUserId`, `ProjectStatus.Inactive`, and `ProjectActivities` additions).
- PowerShell from the repository root.
- A browser that can access the local HTTPS development URL.

## Start the application

```powershell
cd ContosoDashboard
dotnet restore
dotnet build
dotnet run
```

Open the URL printed by `dotnet run`, then sign in through `/login` using one of the seeded mock users:

| Role | Email |
|---|---|
| Administrator | `admin@contoso.com` |
| Project Manager | `camille.nicole@contoso.com` |
| Team Lead | `floris.kregel@contoso.com` |
| Employee | `ni.kang@contoso.com` |

## Scenario 1: Create a project with only required fields (User Story 1)

1. Sign in as the Project Manager (`camille.nicole@contoso.com`).
2. Open `/projects` and select **New Project**.
3. Enter a unique Title (e.g., "Vendor Onboarding Portal"), a Description, Status = Active, leave Progress blank, select a Project Manager, and set Start Date/End Date.
4. Select **Save**.

Expected result: redirected to My Projects; the new project appears with Progress shown as 0%, and its progress bar reflects that stored value (not a task-completion calculation).

## Scenario 2: Reject a duplicate title

1. While signed in as a Project Manager, attempt to create a project titled `ContosoDashboard Development` (the seeded project's exact title) or `contosodashboard development` (same title, different case).
2. Select **Save**.

Expected result: a specific, user-friendly error message about the title already being in use; the user remains on the Create New Project screen with all entered data intact; no project row is created.

## Scenario 3: Backfill a completed project with a past Start Date and non-zero Progress

1. Create a project with a Start Date 6 months in the past, an End Date in the past, Status = Inactive, and Progress = 100.
2. Save.

Expected result: the project is created; My Projects and the project's details page show Progress as 100%, not 0%, even though it has no tasks.

## Scenario 4: Reject a Start Date more than 12 months in the past

1. Attempt to create a project with a Start Date 13 months before today.
2. Save.

Expected result: a specific validation message about the 12-month limit; no project is created.

## Scenario 5: Add a task with only a Title (User Story 3, Clarification 2)

1. On the Create New Project screen, select **New Task**.
2. Enter only a Title and select **Ok**.
3. Confirm the task appears in the table.
4. Complete the remaining required project fields and Save.

Expected result: the project is created with the task attached; opening the project's task list shows the task assigned to the Project Manager who created it, with Medium priority.

## Scenario 6: Cancel a task modal and Cancel the whole screen (User Story 3 edge case)

1. On the Create New Project screen, select **New Task**, enter a Title, then select **Cancel** in the modal.
2. Confirm no task was added to the table.
3. Select **Upload Documents** and choose a file; confirm it appears in the documents table.
4. Select **Cancel** on the overall screen.

Expected result: returned to My Projects; no project, task, or document was created or uploaded to storage.

## Scenario 7: Upload documents during creation (User Story 4, Clarification 3)

1. On the Create New Project screen, select **Upload Documents** and choose one or more files.
2. Confirm the files appear in the documents table but are not yet visible under `/documents` for anyone.
3. Complete the required fields and Save.

Expected result: after Save, the documents are visible under the new project's document list; before Save (or if Cancel is used instead), no document record or stored file exists.

## Scenario 8: Assign team members during creation (User Story 5)

1. On the Create New Project screen, open the team members multi-select and choose two or more users.
2. Confirm the selected names appear as comma-separated text below the control.
3. Complete the required fields and Save.

Expected result: the created project's member list includes the selected users.

## Scenario 9: Role-based access restriction (User Story 2)

1. Sign in as the Team Lead (`floris.kregel@contoso.com`) or the Employee (`ni.kang@contoso.com`).
2. Confirm no **New Project** button appears on `/projects`.
3. Attempt to navigate directly to the Create New Project route.

Expected result: access is denied for both roles; no project can be created through either path.

## Scenario 10: Audit trail (User Story 1 / FR-022)

1. Sign in as the Administrator and create a project (or use one created in an earlier scenario).
2. Verify a `ProjectActivity` row exists with `Action = "ProjectCreated"`, the correct `ActorUserId`, and a timestamp close to creation time (via a debugging query or a future admin view, per [data-model.md](data-model.md)).

Expected result: every project created through this flow has a corresponding, retrievable creation audit record.

## Automated Test Coverage

Run the existing test project to execute the service and authorization tests added for this feature:

```powershell
cd ContosoDashboard.Tests
dotnet test
```

Expected result: `ProjectServiceCreationTests` (uniqueness, date bounds, defaults, audit logging) and `ProjectCreationAuthorizationTests` (role enforcement independent of UI) pass, alongside the existing suite.

## Validation Results (2026-09-21)

**Automated tests**: `dotnet test ContosoDashboard.Tests` — **75/75 passed**, including 21 `ProjectServiceCreationTests`, 5 `ProjectCreationAuthorizationTests`, and 6 `ProjectCreateTests` (bUnit) added for this feature. These cover Scenarios 1–8 and 10's business logic (validation rules, defaulting, atomicity, audit logging) at the service and component level.

**Live run against a real SQL Server LocalDB** (`dotnet run`, fresh `ContosoDashboard` database): confirmed via authenticated HTTP requests as each seeded user (login → cookie session):

- Schema initialization succeeded end-to-end, including the `Progress`/`CreatedByUserId` column additions and the `ProjectActivities` table creation in `ProjectSchemaInitializer`. (This surfaced and fixed a real bug: SQL Server cannot resolve a column added earlier in the *same batch*, so the `CreatedByUserId` backfill had to be split into separate `ExecuteSqlRaw` calls — see the corresponding fix in `ProjectSchemaInitializer.cs`.)
- **Scenario 9** (role-based access): confirmed end-to-end for all four seeded roles. `camille.nicole@contoso.com` (ProjectManager) and `admin@contoso.com` (Administrator) see the "New Project" link on `/projects` and successfully render the Create New Project form at `/projects/create`. `floris.kregel@contoso.com` (TeamLead) and `ni.kang@contoso.com` (Employee) do not see the link, and requesting `/projects/create` renders the app's "Access Denied" view instead of the form — confirming FR-001/FR-002/FR-021/SC-004 hold against the real authorization pipeline, not just in-memory tests.
- Confirmed the Create New Project form renders all required fields (Title, Description, Status, Progress, Project Manager, Start/End Date), the New Task button, the file upload input, the team-member multi-select, and Save/Cancel controls, with no server-side rendering errors.
- Confirmed `/projects` renders the seeded project's progress bar from the new `Progress` column (`aria-valuenow="0"`), verifying the FR-009/Clarification-1 display change didn't regress the existing dashboard.

**Not exercised in this pass**: interactive Save/Cancel/New-Task-modal button clicks and multi-file upload require a live Blazor Server SignalR circuit (a real browser), which wasn't available as a tool in this session. That interactive behavior is what the bUnit `ProjectCreateTests` and the `ProjectServiceCreationTests` atomicity tests verify instead — a manual click-through in a browser is still recommended before sign-off, following Scenarios 1–8 and 10 above with the seeded users.
