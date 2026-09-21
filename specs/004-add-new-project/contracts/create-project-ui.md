# UI Contract: Create New Project Screen

## Boundary

This contract governs the new "Create New Project" screen (`ProjectCreate.razor`) and the "New Project" entry point on the existing My Projects screen (`Projects.razor`). It does not change any other screen's behavior except the two progress-bar bindings called out below.

## Entry Point: My Projects Screen

- A "New Project" button is visible only when the signed-in user holds the ProjectManager or Administrator role (FR-001, FR-002).
- Selecting it navigates to the Create New Project screen (FR-003).
- The existing progress bar on each project card now reads the project's stored `Progress` value instead of the previously computed task-completion percentage.

## Create New Project Screen — Fields

| Field | Control | Required | Notes |
|---|---|---|---|
| Title | Text input | Yes | Must be unique application-wide, case-insensitive, trimmed (FR-006) |
| Description | Text area | Yes | FR-007 |
| Status | Select (Active / Inactive) | Yes | FR-008 |
| Progress | Numeric input, 0–100 | No | Blank defaults to 0 (FR-009) |
| Project Manager | Single-select dropdown | Yes | Lists all users with the ProjectManager role (FR-005) |
| Start Date | Date input | Yes | May be a past date, not more than 12 months back (FR-010) |
| End Date | Date input | Yes | Must not be earlier than Start Date (FR-011) |

## Create New Project Screen — Task Table

- A table lists tasks added so far, using the same presentation as the Project Details task table (FR-012).
- A "New Task" control in the table's top-right corner opens a modal (FR-013).
- Modal fields: Title (required), with optional Description, Priority, Status, Due Date, and Assignee (matching the existing task-entry conventions).
- Modal buttons:
  - **Ok**: validates Title is present; if valid, adds the task to the table (applying default Assignee = creator and default Priority = Medium when left unset) and closes the modal. If Title is missing, shows a validation message and keeps the modal open (FR-014).
  - **Cancel**: discards the entry and closes the modal without adding a row.
- Tasks in the table are held in memory only until the overall Save succeeds.

## Create New Project Screen — Documents

- An "Upload Documents" control lets the user select one or more files (FR-015).
- Selected files appear in a documents table on the screen.
- Files are staged in memory only; nothing is written to storage or the database until the overall Save succeeds (Clarification 3).

## Create New Project Screen — Team Members

- A multi-select control lists available team members (FR-016).
- Selected members are shown as comma-separated text below the control, updating live as the selection changes.

## Create New Project Screen — Save / Cancel

- **Save** (FR-017, FR-018, FR-019):
  1. Validates all fields per the table above.
  2. On success: persists the project, its staged tasks, its staged documents, and its selected team members together, then navigates to My Projects where the new project is visible.
  3. On failure: displays a specific, user-friendly message describing the problem (e.g., "A project with this title already exists.") and remains on the screen with all previously entered data intact — nothing is cleared.
- **Cancel** (FR-020): discards all entered data (metadata, staged tasks, staged documents, team member selections) and returns to My Projects. Nothing is persisted.

## Access Denial

- A user without the ProjectManager or Administrator role who navigates directly to the Create New Project route is denied access and cannot reach the form (FR-002, FR-021).

## Out of Scope for This Contract

- Editing an existing project (no such screen exists yet).
- Any confirmation dialog beyond the validation/error messaging described above.
