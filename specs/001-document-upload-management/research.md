# Research: Document Upload and Management

## Decision 1: Keep the feature in the existing Blazor Server and EF Core application

- **Decision**: Add the feature to the existing `ContosoDashboard` web project using its current Models, Data, Services, Pages, and Shared structure.
- **Rationale**: The application already provides cookie-based mock authentication, role policies, project membership checks, notifications, dashboard summaries, and task/project pages. A new application or API layer would violate the training-first and simplicity principles.
- **Alternatives considered**: A separate document microservice or SPA/API split was rejected because it adds infrastructure and deployment complexity without value for the offline training application.

## Decision 2: Use an integer-keyed document model with text categories

- **Decision**: Add document-related entities to the existing EF Core model. Use integer primary and foreign keys and store category names as text values.
- **Rationale**: This is explicitly required by the feature constraints and matches the existing `User`, `Project`, and `TaskItem` key conventions. Text categories keep the training model easy to inspect and modify.
- **Alternatives considered**: GUID keys and enum-backed integer categories were rejected because they conflict with the stated database constraints.

## Decision 3: Abstract file storage and use a local filesystem implementation

- **Decision**: Define `IFileStorageService` in the application services layer and register `LocalFileStorageService` through dependency injection. Store files below an application data directory outside `wwwroot` using a relative path shaped as `{userId}/{projectId-or-personal}/{guid}.{extension}`.
- **Rationale**: This works offline, prevents direct static-file exposure, supports authorization-aware download/preview endpoints, and preserves the future Azure Blob migration boundary without adding an Azure dependency.
- **Alternatives considered**: Storing bytes in SQL Server was rejected because the stakeholder requirements explicitly call for local filesystem storage and a portable file path. Storing files under `wwwroot` was rejected because it bypasses the intended authorization boundary.

## Decision 4: Save the file before saving document metadata, with cleanup on metadata failure

- **Decision**: Validate metadata and file properties, generate the unique relative path, save the stream, then persist the document row. If persistence fails after the file is written, attempt to delete the newly written file and return a failure result.
- **Rationale**: This follows the specified sequence and prevents database rows pointing at files that were never stored. Cleanup reduces orphaned files when the database operation fails.
- **Alternatives considered**: Saving the database row first was rejected because an empty or non-unique path can create duplicate-key or orphaned-record failures.

## Decision 5: Enforce authorization in the document service and protected file endpoints

- **Decision**: Centralize visibility and mutation checks in `IDocumentService` and repeat access checks in download/preview operations before opening a stream. Project members can view/download project documents by default; owners can manage their own documents; project managers can upload/delete documents for their projects; administrators have full access.
- **Rationale**: Existing services already enforce authorization using the requesting user ID. Applying the same pattern prevents IDOR issues even when a URL or document ID is manipulated.
- **Alternatives considered**: Relying only on page-level `[Authorize]` attributes was rejected because authentication alone does not establish document-level authorization.

## Decision 6: Use a bounded local virus-scanning boundary for training

- **Decision**: Model file-content validation behind an `IFileSafetyScanner` boundary. The offline implementation performs the training-safe validation available locally and returns an explicit scan result; no external malware service is added. The implementation and UI must identify this as a training limitation.
- **Rationale**: The requirement calls for scanning, while the constitution prohibits external services as the default runtime. A named boundary makes the limitation visible and keeps future integration possible.
- **Alternatives considered**: Silently claiming production-grade malware protection was rejected as misleading. Adding a cloud scanner was rejected because it breaks offline operation.

## Decision 7: Use database filtering with indexed fields for the initial search

- **Decision**: Implement title, description, category, project, uploader, date, and tag filtering in EF Core queries. Add indexes for uploader, project, category, upload date, and searchable ownership/access joins. Keep the result set paged and authorization-filtered before returning results.
- **Rationale**: The training target is up to 500 documents per list and a two-second response. Indexed relational queries are sufficient and keep the design understandable without introducing a search platform.
- **Alternatives considered**: SQL Server full-text search or an external search engine was rejected for the initial offline feature because it adds setup complexity not justified by the stated scale.

## Decision 8: Represent tags, sharing, versions, and audit events as relational entities

- **Decision**: Use separate entities for document tags, document shares, document versions, and document activity. Keep the current document row as the active file metadata and preserve replacement history through versions.
- **Rationale**: Separate rows support search, many-to-many sharing, replacement semantics, and audit reporting without putting unbounded structured data into a single column.
- **Alternatives considered**: Comma-separated tags, overwriting files without history, and an in-memory audit log were rejected because they are difficult to query, validate, and demonstrate.

## Decision 9: Add focused automated tests without changing the production architecture

- **Decision**: Add a test project for document service, authorization, validation, storage sequencing, and search behavior, plus manual browser scenarios for upload progress, preview, and responsive UI.
- **Rationale**: The repository has no existing test project, but the constitution requires verifiable behavior and the feature is security-sensitive. Tests should cover real service behavior and use temporary filesystem storage rather than only mock interaction assertions.
- **Alternatives considered**: Relying only on manual testing was rejected because it would not reliably protect authorization and file cleanup behavior.

## Repository Alignment Note

The current project file targets .NET 9.0 and uses EF Core 9.0 packages, while the constitution and README describe an ASP.NET Core 8 target. This plan preserves the repository's actual target and does not introduce a runtime migration. The mismatch should be reconciled in a separate documentation/constitution maintenance change; it does not block the document feature design.
