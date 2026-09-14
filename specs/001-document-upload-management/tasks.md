# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-upload-management/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md)
**Tests**: Included because the project constitution requires test-first, verifiable behavior for security-sensitive features.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the test project, feature directories, and local configuration boundaries required by the implementation.

- [x] T001 Create the `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` test project targeting `net9.0` with test SDK, xUnit, and the web project reference.
- [x] T002 [P] Create test directory structure under `ContosoDashboard.Tests/Services/`, `ContosoDashboard.Tests/Storage/`, and `ContosoDashboard.Tests/Authorization/`.
- [x] T003 [P] Add the local document storage root configuration key and training-only documentation to `ContosoDashboard/appsettings.json`.
- [x] T004 [P] Add the document management navigation entry and placeholder route link in `ContosoDashboard/Shared/NavMenu.razor`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement shared data, storage, validation, and authorization foundations before any user story is built.

**Critical**: User story work depends on this phase.

- [x] T005 [P] Create integer-keyed `Document` entity with required title/category, optional project/task associations, generated relative path, original filename, file size, MIME type, uploader, timestamps, and deletion state in `ContosoDashboard/Models/Document.cs`.
- [x] T006 [P] Create normalized custom tag entity in `ContosoDashboard/Models/DocumentTag.cs`.
- [x] T007 [P] Create explicit user/team sharing entity in `ContosoDashboard/Models/DocumentShare.cs`.
- [x] T008 [P] Create document replacement history entity in `ContosoDashboard/Models/DocumentVersion.cs`.
- [x] T009 [P] Create document audit event entity and action values in `ContosoDashboard/Models/DocumentActivity.cs`.
- [x] T010 Add document, tag, share, version, and activity DbSets, relationships, text category constraints, foreign-key delete behaviors, uniqueness rules, and indexes for uploader/project/category/upload date/search access in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [x] T011 Extend document navigation collections and relationships on `ContosoDashboard/Models/User.cs`, `ContosoDashboard/Models/Project.cs`, and `ContosoDashboard/Models/TaskItem.cs`.
- [x] T012 [P] Define `IFileStorageService` and storage result contracts for upload, delete, download, and URL/content metadata in `ContosoDashboard/Services/FileStorageService.cs`.
- [x] T013 [P] Define `IFileSafetyScanner` and the training-local scan result contract in `ContosoDashboard/Services/FileSafetyScanner.cs`.
- [x] T014 Implement `LocalFileStorageService` with configured storage-root validation, GUID-based relative paths, path traversal protection, directory creation, stream copy, download, and delete behavior in `ContosoDashboard/Services/FileStorageService.cs`.
- [x] T015 Implement allowed-extension, MIME, 25 MB size, title/category, and local safety validation in `ContosoDashboard/Services/FileSafetyScanner.cs`.
- [x] T016 Register storage, scanner, document, sharing, and activity services in `ContosoDashboard/Program.cs` without adding cloud dependencies.
- [x] T017 Create storage safety and upload validation tests covering path traversal rejection, GUID paths, supported types, unsupported types, 25 MB boundaries, and invalid metadata in `ContosoDashboard.Tests/Storage/FileStorageServiceTests.cs`.
- [x] T018 Create data-model and authorization fixture helpers using temporary filesystem storage and seeded user/project roles in `ContosoDashboard.Tests/Authorization/DocumentAuthorizationFixture.cs`.

**Checkpoint**: The database model, local storage boundary, file validation, dependency injection, and reusable authorization test setup are ready for story implementation.

---

## Phase 3: User Story 1 - Upload and Organize Work Documents (Priority: P1) MVP

**Goal**: Allow authenticated users to upload valid personal or project documents, store them securely, capture metadata, and see them in the appropriate document/project view.

**Independent Test**: As a seeded employee or project manager, upload a supported file with title and category, verify the file is outside `wwwroot`, verify metadata and project association, and verify invalid uploads create neither a database row nor an orphaned file.

### Tests for User Story 1

- [x] T019 [P] [US1] Add upload workflow tests for validation, generated path before persistence, save-file-before-database ordering, successful metadata creation, and cleanup after database failure in `ContosoDashboard.Tests/Services/DocumentServiceUploadTests.cs`.
- [x] T020 [P] [US1] Add role and project-membership tests for personal uploads, project uploads, project-manager upload permission, non-member rejection, and uploader identity sourced from the request context in `ContosoDashboard.Tests/Authorization/DocumentUploadAuthorizationTests.cs`.

### Implementation for User Story 1

- [x] T021 [US1] Define upload request, metadata, validation error, and document result contracts in `ContosoDashboard/Services/DocumentService.cs`.
- [x] T022 [US1] Implement `IDocumentService.UploadAsync` to authorize the user, validate the project/task context, generate a unique relative path, save the file, persist metadata, and remove the file if persistence fails in `ContosoDashboard/Services/DocumentService.cs`.
- [x] T023 [US1] Implement upload activity creation and project-member notification dispatch for successful project uploads in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/DocumentActivityService.cs`.
- [x] T024 [US1] Add document-related notification enum values and preserve existing notification behavior in `ContosoDashboard/Models/Notification.cs` and `ContosoDashboard/Services/NotificationService.cs`.
- [x] T025 [US1] Create the upload page with multi-file selection, required title/category/project fields, optional description/tags, progress state, success state, and clear validation errors in `ContosoDashboard/Pages/DocumentUpload.razor`.
- [x] T026 [US1] Add document list and project document sections showing title, category, upload date, size, MIME type, uploader, and project association in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/ProjectDetails.razor`.
- [x] T027 [US1] Add document upload controls and project context propagation to the task workflow in `ContosoDashboard/Pages/Tasks.razor`.
- [x] T028 [US1] Add document summary count and current-user recent five-document widget to `ContosoDashboard/Services/DashboardService.cs` and `ContosoDashboard/Pages/Index.razor`.
- [x] T029 [US1] Add focused upload and project-document styles for progress, validation, empty, success, and error states in `ContosoDashboard/wwwroot/css/site.css`.
- [x] T030 [US1] Register and validate the seeded employee, team lead, project manager, and administrator upload scenarios in `specs/001-document-upload-management/quickstart.md`.

**Checkpoint**: User Story 1 is independently demoable: upload, validation, secure storage, metadata, project visibility, dashboard summary, and task/project entry points work together.

---

## Phase 4: User Story 2 - Search, Browse, Preview, and Download (Priority: P2)

**Goal**: Let users find documents quickly and safely access only documents they are authorized to view, preview, or download.

**Independent Test**: Seed documents across users, projects, dates, categories, and tags; search and filter as multiple roles; verify results are permission-filtered and PDF/image preview and downloads work only for authorized users.

### Tests for User Story 2

- [ ] T031 [P] [US2] Add search, sort, filter, pagination, and two-second target dataset tests in `ContosoDashboard.Tests/Services/DocumentSearchTests.cs`.
- [ ] T032 [P] [US2] Add IDOR and file-access tests for unauthorized list, preview, download, and manipulated document identifiers in `ContosoDashboard.Tests/Authorization/DocumentFileAccessTests.cs`.

### Implementation for User Story 2

- [ ] T033 [US2] Implement permission-filtered document listing, title/description/tag/uploader/project search, category/project/date/file-size filters, and title/date/category/file-size sorting in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T034 [US2] Implement authorized preview/download stream retrieval with safe content disposition, MIME handling, and download/preview activity logging in `ContosoDashboard/Controllers/DocumentFilesController.cs`.
- [ ] T035 [US2] Register MVC controllers and protected document file routes in `ContosoDashboard/Program.cs` without exposing the storage directory through static files.
- [ ] T036 [US2] Add search box, filters, sort controls, result paging, empty/error states, preview links, and download actions to `ContosoDashboard/Pages/Documents.razor`.
- [ ] T037 [US2] Add document details and browser preview experience for PDF and image content in `ContosoDashboard/Pages/DocumentDetails.razor`.
- [ ] T038 [US2] Add responsive table and preview/download styles in `ContosoDashboard/wwwroot/css/site.css`.
- [ ] T039 [US2] Execute the search, permission, preview, download, and performance scenarios in `specs/001-document-upload-management/quickstart.md` and record any training-environment limitations.

**Checkpoint**: User Stories 1 and 2 are independently testable; users can upload, locate, preview, and download authorized documents without direct filesystem exposure.

---

## Phase 5: User Story 3 - Sharing, Notifications, Versioning, and Audit (Priority: P3)

**Goal**: Provide controlled document sharing, recipient notifications, metadata/file management, replacement history, deletion, and administrator audit/reporting views.

**Independent Test**: Share a document as its owner, verify recipient notification and shared-with-me access, replace and delete documents as authorized roles, then review activity records as administrator.

### Tests for User Story 3

- [ ] T040 [P] [US3] Add share/revoke, duplicate-share, recipient-access, non-member, and notification tests in `ContosoDashboard.Tests/Services/DocumentShareServiceTests.cs`.
- [ ] T041 [P] [US3] Add metadata-edit, file-replacement/version, deletion, cleanup-failure, and activity-log tests in `ContosoDashboard.Tests/Services/DocumentLifecycleTests.cs`.
- [ ] T042 [P] [US3] Add administrator audit/report authorization tests in `ContosoDashboard.Tests/Authorization/DocumentAuditAuthorizationTests.cs`.

### Implementation for User Story 3

- [ ] T043 [US3] Implement metadata edit, file replacement, version creation, owner/project-manager authorization, and activity logging in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T044 [US3] Implement explicit user/team share creation, duplicate prevention, revocation, recipient visibility, and share activity logging in `ContosoDashboard/Services/DocumentShareService.cs`.
- [ ] T045 [US3] Add document-shared and project-document-added notification types and recipient notification creation in `ContosoDashboard/Models/Notification.cs` and `ContosoDashboard/Services/NotificationService.cs`.
- [ ] T046 [US3] Implement authorized deletion with confirmation-ready result handling, permanent file removal, metadata state update, and delete activity logging in `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T047 [US3] Add document details controls for metadata editing, replacement upload, share/revoke, and confirmed deletion in `ContosoDashboard/Pages/DocumentDetails.razor`.
- [ ] T048 [US3] Add shared-with-me view and recipient notification links in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/Notifications.razor`.
- [ ] T049 [US3] Add administrator document activity, upload-type, active-uploader, and access-pattern reporting view in `ContosoDashboard/Pages/DocumentReports.razor`.
- [ ] T050 [US3] Add sharing, version history, deletion confirmation, and audit-report styles in `ContosoDashboard/wwwroot/css/site.css`.
- [ ] T051 [US3] Execute the share, notification, lifecycle, and audit scenarios in `specs/001-document-upload-management/quickstart.md` using seeded roles.

**Checkpoint**: All three user stories are independently demonstrable with role-aware sharing, lifecycle management, notifications, and audit reporting.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Complete integration quality, documentation, security review, and end-to-end validation.

- [ ] T052 [P] Add build/test project wiring to `ContosoDashboard.sln` or the repository workspace solution configuration so `dotnet test` discovers `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj`.
- [ ] T053 [P] Add user-facing training-scope and local-storage limitations to `README.md` and document the future cloud-storage replacement boundary.
- [ ] T054 [P] Add structured logging for upload rejection, storage failure, unauthorized access, cleanup failure, and audit persistence in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/FileStorageService.cs`.
- [ ] T055 Review every document list, search, preview, download, edit, replace, share, and delete path for service-level authorization and IDOR resistance in `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/DocumentShareService.cs`, and `ContosoDashboard/Controllers/DocumentFilesController.cs`.
- [ ] T056 Run `dotnet build` and `dotnet test` from `ContosoDashboard/` and record the results against `specs/001-document-upload-management/quickstart.md`.
- [ ] T057 Run the complete manual quickstart, including upload progress, responsive UI, preview, project/task integration, notifications, deletion confirmation, and administrator reporting, and update `specs/001-document-upload-management/quickstart.md` with verified outcomes.
- [ ] T058 Review and reconcile the .NET target documentation discrepancy between `ContosoDashboard/ContosoDashboard.csproj`, `.specify/memory/constitution.md`, and `README.md` in a separate documented maintenance change without changing the feature runtime target.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; T001-T004 can begin immediately and are mostly parallel.
- **Foundational (Phase 2)**: Depends on Phase 1; T005-T018 block all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 and is the MVP increment.
- **User Story 2 (Phase 4)**: Depends on the document model/storage and US1 upload results, especially T022 and T026, but can be developed in parallel after those contracts stabilize.
- **User Story 3 (Phase 5)**: Depends on the document model/storage and US1 persistence; it can begin after T022/T023 and does not require the complete US2 UI.
- **Polish (Phase 6)**: Depends on all desired user stories and their focused tests being complete.

### User Story Completion Order

1. US1: Upload and organize work documents (P1 MVP)
2. US2: Search, browse, preview, and download (P2)
3. US3: Sharing, notifications, versioning, and audit (P3)

### Parallel Opportunities

- Phase 1: T002, T003, and T004 can run in parallel after T001 establishes the test project context.
- Phase 2: T005-T009, T012-T013, and T017-T018 can be developed in parallel when their contracts are agreed; T010-T016 integrate the foundations.
- US1: T019-T020 can be written in parallel; T025, T026, T027, and T029 can proceed in parallel after the service contract in T021 is stable.
- US2: T031-T032 can be written in parallel; T036-T038 can proceed in parallel after T033-T035 define the access flow.
- US3: T040-T042 can be written in parallel; T047-T050 can proceed in parallel after T043-T046 establish service behavior.
- After the foundational checkpoint, separate developers can work on US1, US2, and US3 service/UI slices where shared contract changes are coordinated.

## Parallel Example: User Story 1

```text
Task: T019 [US1] Upload workflow tests in ContosoDashboard.Tests/Services/DocumentServiceUploadTests.cs
Task: T020 [US1] Upload authorization tests in ContosoDashboard.Tests/Authorization/DocumentUploadAuthorizationTests.cs
Task: T025 [US1] Upload page in ContosoDashboard/Pages/DocumentUpload.razor
Task: T026 [US1] Documents and project document views in ContosoDashboard/Pages/Documents.razor and ContosoDashboard/Pages/ProjectDetails.razor
Task: T029 [US1] Upload UI styles in ContosoDashboard/wwwroot/css/site.css
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational entities, storage, validation, DI, and tests.
3. Complete Phase 3 US1 upload, organization, project/task integration, and dashboard summary.
4. Run the US1 focused tests and quickstart scenarios.
5. Stop for an independent MVP review before beginning search or sharing.

### Incremental Delivery

1. Deliver US1 as the offline upload and organization MVP.
2. Deliver US2 as permission-filtered discovery and file access.
3. Deliver US3 as collaboration, lifecycle management, and governance.
4. Complete polish, security review, and full quickstart validation.

## Notes

- Every task has a sequential ID and an exact file path.
- `[P]` marks only tasks that can proceed independently without incomplete-task dependencies.
- User-story tasks include `[US1]`, `[US2]`, or `[US3]` labels.
- The generated `specs/main/plan.md` is not used; the authoritative feature artifacts remain under `specs/001-document-upload-management/`.
