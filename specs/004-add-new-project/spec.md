# Feature Specification: Add New Contoso Project

**Feature Branch**: `004-add-new-project`
**Created**: 2026-09-21
**Status**: Draft
**Input**: User description: "StakeholderDocs/AddNewContosoProject.md — Add New Project Feature: replace the current data-fix / compliance-approval process for onboarding new Contoso projects with a self-service Create New Project screen for Project Managers and Administrators, including project metadata, an in-line task table, document uploads, and team member assignment."

## Clarifications

### Session 2026-09-21

- Q: When a Project Manager manually enters a Progress value at project creation, should that value stay fixed as entered, or should it later be overridden by the app's existing task-completion calculation (the progress bars already shown on My Projects and Project Details)? → A: Manual value stays fixed — the entered Progress is stored and always displayed as-is for the project; the existing task-completion calculation is not used to override it.
- Q: When a Project Manager adds a task via the "New Task" modal and only enters a Title, who should that task be assigned to by default? → A: Default to creator — Assignee defaults to the Project Manager creating the project and Priority defaults to Medium; either can be changed in the modal.
- Q: If a Project Manager selects documents to upload while creating a project and then clicks Cancel, should those files ever be written to storage, or must nothing be persisted at all until Save succeeds? → A: Stage only, no write — files are held in the browser/session and are not written to storage or the database until Save succeeds; Cancel simply discards the staged selection.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and save a new project (Priority: P1)

A Project Manager needs to onboard a newly finalized project without filing an IT data-fix request. They open My Projects, select "New Project," fill in the project's core details, and save. The project immediately appears in My Projects for themselves and their team.

**Why this priority**: This is the core value of the feature — it removes the compliance/data-fix bottleneck entirely. Without this, there is no feature.

**Independent Test**: Log in as a Project Manager, create a project with only the required metadata fields filled in, save, and confirm the project appears in My Projects with the entered details.

**Acceptance Scenarios**:

1. **Given** a Project Manager is on the My Projects screen, **When** they select "New Project," **Then** they are taken to the Create New Project screen with empty input fields.
2. **Given** the Project Manager has filled in all required fields (Title, Description, Status, Project Manager, Start Date, End Date) with valid values, **When** they select Save, **Then** the project is created, and they are returned to My Projects where the new project is visible.
3. **Given** the Project Manager leaves Progress blank, **When** they save, **Then** the project is created with Progress set to 0.
4. **Given** the Project Manager enters a Title that already exists on another project, **When** they select Save, **Then** the system shows a user-friendly error explaining the title is already in use, and the entered data remains on screen.
5. **Given** the Project Manager has entered data, **When** they select Cancel, **Then** no project is created and they return to the My Projects screen.

---

### User Story 2 - Restrict project creation to authorized roles (Priority: P1)

Contoso needs assurance that only Project Managers and Administrators can create projects, since project creation was previously gated behind a compliance approval process. Employees and Team Leads must not be able to create projects through the UI or by any direct means.

**Why this priority**: Without this control, the feature reintroduces the governance risk the current data-fix process exists to manage. This must ship with the first release of the feature.

**Independent Test**: Log in as an Employee and as a Team Lead; confirm neither the "New Project" button nor the Create New Project screen is reachable, including via direct navigation.

**Acceptance Scenarios**:

1. **Given** a user with the Employee or Team Lead role is on My Projects, **When** the screen renders, **Then** no "New Project" button is shown.
2. **Given** a user with the Employee or Team Lead role, **When** they attempt to navigate directly to the Create New Project screen, **Then** access is denied and they are not able to create a project.
3. **Given** a user with the Project Manager or Administrator role, **When** they view My Projects, **Then** the "New Project" button is visible and usable.

---

### User Story 3 - Add project tasks during creation (Priority: P2)

A Project Manager wants the new project to start with an initial task list so team members can begin work immediately after the project is created, without a separate follow-up step.

**Why this priority**: Valuable and commonly needed, but a project can still be created and used without tasks pre-loaded, so it is independently deferrable from the core save flow.

**Independent Test**: While creating a new project, add one or more tasks via the task table before saving, then confirm the tasks are attached to the project after save.

**Acceptance Scenarios**:

1. **Given** the Project Manager is on the Create New Project screen, **When** they select "New Task," **Then** a modal opens to enter task details.
2. **Given** the task modal is open and Title is left blank, **When** the Project Manager selects Ok, **Then** the system shows a validation message and the task is not added.
3. **Given** the task modal is open with a valid Title entered, **When** the Project Manager selects Ok, **Then** the task is added to the task table on the Create New Project screen.
4. **Given** the task modal is open, **When** the Project Manager selects Cancel, **Then** the modal closes and no task is added.
5. **Given** one or more tasks were added to the table, **When** the Project Manager saves the project, **Then** all added tasks are persisted as part of the new project.

---

### User Story 4 - Upload supporting documents during creation (Priority: P2)

A Project Manager wants to attach charter documents, budgets, or other supporting files to the project at the time it is created, rather than uploading them separately afterward.

**Why this priority**: Useful and expected, but the project is still functional without documents attached at creation time, so it can be delivered after the core create/save flow.

**Independent Test**: While creating a new project, use "Upload Documents" to select one or more files, confirm they appear in the documents table, then save and confirm the documents are associated with the created project.

**Acceptance Scenarios**:

1. **Given** the Project Manager is on the Create New Project screen, **When** they select "Upload Documents," **Then** they can choose one or more files to attach.
2. **Given** one or more files have been selected, **When** the selection completes, **Then** the files appear in a table of documents on the Create New Project screen.
3. **Given** documents were selected, **When** the Project Manager saves the project, **Then** the documents are persisted and associated with the new project.

---

### User Story 5 - Assign team members during creation (Priority: P3)

A Project Manager wants to identify who is on the project team as part of creating it, so team members have visibility into the project from day one.

**Why this priority**: Adds convenience but team members can be assigned after project creation as a follow-up action, so this is the most deferrable capability.

**Independent Test**: While creating a new project, select multiple team members from the multi-select control, confirm they render as comma-separated text, save, and confirm the assignments are attached to the created project.

**Acceptance Scenarios**:

1. **Given** the Project Manager is on the Create New Project screen, **When** they open the team members multi-select, **Then** they can select one or more team members.
2. **Given** one or more team members are selected, **When** the selection changes, **Then** the selected names are displayed as comma-separated text below the control.
3. **Given** team members were selected, **When** the Project Manager saves the project, **Then** the selected team members are persisted as part of the new project.

---

### Edge Cases

- What happens when the entered Title matches an existing project's title with different casing or surrounding whitespace? Title uniqueness checks are case-insensitive and ignore leading/trailing whitespace.
- What happens when Start Date is more than 12 months in the past? The system rejects the save with a user-friendly validation message and keeps the user on the screen.
- What happens when End Date is earlier than Start Date? The system rejects the save with a user-friendly validation message and keeps the user on the screen.
- What happens when Progress is entered outside the 0-100 range? The system rejects the save with a user-friendly validation message.
- What happens when the Project Manager dropdown has no eligible project managers (e.g., none provisioned yet)? The dropdown is shown empty and Save is blocked until a Project Manager is selected, with a clear message indicating one is required.
- What happens when a network or server error occurs during Save? The system shows a user-friendly error message, keeps the user on the Create New Project screen, and preserves all entered data (metadata, tasks, documents, team members) so nothing needs to be re-entered.
- What happens when a user without Project Manager/Administrator role attempts to reach the Create New Project screen or trigger project creation directly (bypassing the UI)? The action is denied and no project is created.
- How does the system handle a Cancel action after tasks or documents were already added in the current session? All in-progress entries (metadata, tasks, documents, team members) are discarded and the user returns to My Projects with no project created.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a "New Project" button on the My Projects screen, visible only to users with the Project Manager or Administrator role.
- **FR-002**: System MUST hide the "New Project" button from users with the Employee or Team Lead role.
- **FR-003**: Selecting "New Project" MUST navigate the user to a Create New Project screen.
- **FR-004**: Create New Project screen MUST present labeled input controls for: Title, Description, Status, Progress, Project Manager, Start Date, and End Date (Target Completion Date).
- **FR-005**: Project Manager field MUST be a single-select dropdown listing all available project managers; the user MUST select exactly one.
- **FR-006**: Title MUST be required and MUST be unique across all projects in the application (case-insensitive, ignoring leading/trailing whitespace).
- **FR-007**: Description MUST be required.
- **FR-008**: Status MUST be required and limited to a choice of Active or Inactive.
- **FR-009**: Progress MUST accept a numeric value between 0 and 100 inclusive; if left blank, the system MUST default it to 0. Once saved, this value MUST be stored and displayed as-is for the project; it MUST NOT be recalculated or overridden by task-completion status.
- **FR-010**: Start Date MUST be required and MAY be set to a past date to support backfilling completed projects, but MUST NOT be more than 12 months before the current date.
- **FR-011**: End Date MUST be required and MUST NOT be earlier than the Start Date.
- **FR-012**: Create New Project screen MUST include a table of project tasks, presented consistently with the task list already used on the Project Details screen.
- **FR-013**: The task table MUST provide a "New Task" control that opens a modal dialog for entering a task's details.
- **FR-014**: The task modal MUST require Title before a task can be added, and MUST provide Ok (validate and add the task to the table, then close) and Cancel (discard the entry and close) actions. If Assignee or Priority are not explicitly set in the modal, the system MUST default Assignee to the user creating the project and Priority to Medium.
- **FR-015**: Create New Project screen MUST provide an "Upload Documents" control and a table listing the documents selected for the project. Selected files MUST be staged locally (not written to storage or the database) until the project is successfully saved.
- **FR-016**: Create New Project screen MUST provide a multi-select control for choosing team members, and MUST display the currently selected team members as comma-separated text below the control.
- **FR-017**: Create New Project screen MUST provide Save and Cancel actions.
- **FR-018**: Selecting Save MUST validate all entered data and, when valid, persist the project together with its associated tasks, documents, and team member assignments, then navigate the user to the My Projects screen where the new project is visible.
- **FR-019**: Selecting Save MUST, when validation fails, display a user-friendly message describing the specific problem and keep the user on the Create New Project screen with previously entered data intact.
- **FR-020**: Selecting Cancel MUST discard all entered data (metadata, tasks, documents, team members) without persisting anything, and return the user to the My Projects screen.
- **FR-021**: System MUST enforce project-creation authorization server-side, independent of UI visibility, so that users without the Project Manager or Administrator role cannot create a project by any means, including direct navigation or direct request.
- **FR-022**: System MUST record, for every created project, the identity of the user who created it and the date/time of creation.

### Key Entities *(include if feature involves data)*

- **Project**: The record being created. Attributes include Title (unique), Description, Status (Active/Inactive), Progress (0-100, default 0, stored and displayed as entered — not recalculated from task completion), assigned Project Manager, Start Date, End Date, the user who created it, and the creation timestamp. Relates to one or more Project Tasks, Documents, and Team Members.
- **Project Task**: An item of work belonging to a Project, entered via the task modal during project creation. Title is required; Assignee defaults to the project's creator and Priority defaults to Medium when not explicitly set; other descriptive, status, or assignment details follow the same structure already used for tasks elsewhere in the application.
- **Document**: A file selected for upload and associated with the Project at creation time, shown in the documents table before save. Remains staged locally and is not written to storage or the database until the project is successfully saved.
- **Team Member Assignment**: An association between the Project and a user designated as part of the project team, established via the multi-select control.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A Project Manager or Administrator can create a fully-configured new project (metadata, initial tasks, documents, and team members) and see it appear in My Projects in under 5 minutes, without any IT or compliance involvement.
- **SC-002**: 100% of new project onboarding is completed through the self-service Create New Project screen, eliminating the need for data-fix requests and their associated approval cycle.
- **SC-003**: Users who encounter a validation error while saving can correct the issue and successfully save on their next attempt without having to re-enter any previously provided data.
- **SC-004**: 0% of project-creation attempts by Employee or Team Lead roles succeed, whether attempted through the UI or by direct request.
- **SC-005**: 100% of created projects have a retrievable audit record identifying who created them and when.

## Assumptions

- Title uniqueness is evaluated case-insensitively and application-wide, not scoped to a single Project Manager or organizational unit.
- End Date must be on or after Start Date; both dates may be set in the past to support backfilling already-completed projects, subject to the 12-month look-back limit on Start Date.
- Tasks added during project creation use the same fields as the existing task list (Title required; other fields such as description, priority, status, due date, and assignee follow existing task conventions and default per the Clarifications above when not explicitly set).
- Document upload during project creation follows the same file-handling behavior (accepted types, size limits) as the application's existing document upload feature, except that files remain staged locally rather than being written to storage immediately upon selection (see Clarifications).
- Save is an all-or-nothing operation: the project, its tasks, its documents, and its team member assignments are created together in a single successful save; there is no partial or draft save state.
- Progress entered at creation is a manually specified value that remains fixed for the life of the project unless a user directly edits it; it is independent of, and never overridden by, any automatically computed completion percentage used elsewhere in the application.

## Out of Scope

- Editing, updating, or deleting a project after it has been created.
- Bulk or file-based import of multiple projects at once.
- Changing the pool of eligible project managers or the rules for who can be a project manager.
- Notifications or emails to team members when they are assigned to a new project.
- Any approval workflow for project creation beyond the existing role-based authorization check.
