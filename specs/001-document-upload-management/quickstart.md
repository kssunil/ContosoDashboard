# Quickstart Validation: Document Upload and Management

## Prerequisites

- Windows with the .NET SDK compatible with the repository's `net9.0` target.
- SQL Server LocalDB or the configured local SQL Server instance.
- PowerShell from the repository root.
- A browser that can access the local HTTPS development URL.

## Start the application

```powershell
cd ContosoDashboard
dotnet restore
dotnet build
dotnet run
```

Open the URL printed by `dotnet run`, then sign in through `/login` using one of the seeded mock users.

## Scenario 1: Upload a personal document

1. Sign in as the seeded employee (`ni.kang@contoso.com`).
2. Open `/documents/upload` or select **Upload Documents** from `/documents`.
3. Select a supported PDF or image, enter a title, choose `Personal Files`, and submit.
4. Confirm the progress indicator and success message appear.
5. Confirm the document appears in `/documents` and in the recent-documents dashboard widget on `/`.
6. Confirm the physical file is outside `ContosoDashboard/wwwroot` and its stored name is GUID-based.

Expected result: a Document record and file exist, and the current user can preview/download it.

## Scenario 2: Reject invalid files

- Submit a file larger than 25 MB.
- Submit a file with an unsupported extension.
- Submit a request without a title or category.

Expected result: each request is rejected with a specific validation message, no document row is created, and no leftover file remains in local storage.

## Scenario 3: Project access and role behavior

1. Sign in as the seeded project manager (`camille.nicole@contoso.com`) and upload a document from `/projects/1` or `/documents/upload?projectId=1`.
2. Sign in as the seeded team lead and confirm the document is visible and downloadable.
3. Sign in as the seeded employee who is a project member and confirm the same access.
4. Attempt a project upload as a user outside the project and confirm the upload is denied.
5. Sign in as administrator and confirm administrative access.

Expected result: project members can view/download by default; project managers can upload/delete project documents; unauthorized users cannot access the file endpoint by changing the document ID.

## Scenario 4: Search, filter, and task integration

1. Create documents with different categories, tags, projects, upload dates, and uploaders.
2. Search by title, description, tag, uploader, and project.
3. Sort and filter by title, category, file size, project, and date range.
4. Attach or upload a document from a task detail page and confirm its project association.

Expected result: results are permission-filtered, relevant records appear within two seconds for the target dataset, and task uploads inherit the task project.

## Scenario 5: Share and notify

1. Sign in as a document owner and share a document with another seeded user.
2. Sign in as the recipient and confirm an in-app notification and shared-with-me result.
3. Share a project document with a user outside the project and verify the configured sharing rule is enforced.

Expected result: the share is recorded, notification is created, and access follows the authorization rules in [data-model.md](data-model.md).

## Scenario 6: Audit and replacement

1. Download, preview, edit metadata, replace, share, and delete a document as authorized users.
2. Review the activity records as administrator.
3. Confirm replacement creates a new version and does not lose the audit history.
4. Confirm deletion removes the file and hides the document from authorized lists after confirmation.

Expected result: each activity is recorded and unauthorized actions do not mutate data.

## Automated validation

When the test project exists, run:

```powershell
dotnet test
```

The automated suite should cover validation, storage sequencing and cleanup, authorization, search filtering, notification creation, version replacement, deletion, and audit records. Browser-only progress and preview behavior remains a manual validation scenario unless a browser test harness is introduced later.
