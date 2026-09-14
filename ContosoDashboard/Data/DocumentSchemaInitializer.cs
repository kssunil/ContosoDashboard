using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public static class DocumentSchemaInitializer
{
    public static void EnsureCreated(ApplicationDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            "IF OBJECT_ID(N'[Documents]', N'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE [Documents] (" +
            "[DocumentId] int NOT NULL IDENTITY, " +
            "[Title] nvarchar(255) NOT NULL, " +
            "[Description] nvarchar(2000) NULL, " +
            "[Category] nvarchar(100) NOT NULL, " +
            "[FilePath] nvarchar(500) NOT NULL, " +
            "[OriginalFileName] nvarchar(255) NOT NULL, " +
            "[FileSize] bigint NOT NULL, " +
            "[MimeType] nvarchar(255) NOT NULL, " +
            "[UploaderId] int NOT NULL, " +
            "[ProjectId] int NULL, " +
            "[TaskId] int NULL, " +
            "[UploadedDate] datetime2 NOT NULL, " +
            "[UpdatedDate] datetime2 NOT NULL, " +
            "[IsDeleted] bit NOT NULL CONSTRAINT [DF_Documents_IsDeleted] DEFAULT 0, " +
            "CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]), " +
            "CONSTRAINT [FK_Documents_Users_UploaderId] FOREIGN KEY ([UploaderId]) REFERENCES [Users] ([UserId]), " +
            "CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]), " +
            "CONSTRAINT [FK_Documents_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId])" +
            "); " +
            "CREATE INDEX [IX_Documents_UploaderId_UploadedDate] ON [Documents] ([UploaderId], [UploadedDate]); " +
            "CREATE INDEX [IX_Documents_ProjectId_UploadedDate] ON [Documents] ([ProjectId], [UploadedDate]); " +
            "CREATE INDEX [IX_Documents_Category] ON [Documents] ([Category]); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF OBJECT_ID(N'[DocumentTags]', N'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE [DocumentTags] (" +
            "[DocumentTagId] int NOT NULL IDENTITY, " +
            "[DocumentId] int NOT NULL, " +
            "[Tag] nvarchar(100) NOT NULL, " +
            "CONSTRAINT [PK_DocumentTags] PRIMARY KEY ([DocumentTagId]), " +
            "CONSTRAINT [FK_DocumentTags_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE" +
            "); " +
            "CREATE UNIQUE INDEX [IX_DocumentTags_DocumentId_Tag] ON [DocumentTags] ([DocumentId], [Tag]); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF OBJECT_ID(N'[DocumentShares]', N'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE [DocumentShares] (" +
            "[DocumentShareId] int NOT NULL IDENTITY, " +
            "[DocumentId] int NOT NULL, " +
            "[UserId] int NULL, " +
            "[SharedByUserId] int NOT NULL, " +
            "[SharedDate] datetime2 NOT NULL, " +
            "CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([DocumentShareId]), " +
            "CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_DocumentShares_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]), " +
            "CONSTRAINT [FK_DocumentShares_Users_SharedByUserId] FOREIGN KEY ([SharedByUserId]) REFERENCES [Users] ([UserId])" +
            "); " +
            "CREATE UNIQUE INDEX [IX_DocumentShares_DocumentId_UserId] ON [DocumentShares] ([DocumentId], [UserId]); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF OBJECT_ID(N'[DocumentVersions]', N'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE [DocumentVersions] (" +
            "[DocumentVersionId] int NOT NULL IDENTITY, " +
            "[DocumentId] int NOT NULL, " +
            "[VersionNumber] int NOT NULL, " +
            "[FilePath] nvarchar(500) NOT NULL, " +
            "[OriginalFileName] nvarchar(255) NOT NULL, " +
            "[FileSize] bigint NOT NULL, " +
            "[MimeType] nvarchar(255) NOT NULL, " +
            "[UploadedByUserId] int NOT NULL, " +
            "[UploadedDate] datetime2 NOT NULL, " +
            "CONSTRAINT [PK_DocumentVersions] PRIMARY KEY ([DocumentVersionId]), " +
            "CONSTRAINT [FK_DocumentVersions_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_DocumentVersions_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId])" +
            "); " +
            "CREATE UNIQUE INDEX [IX_DocumentVersions_DocumentId_VersionNumber] ON [DocumentVersions] ([DocumentId], [VersionNumber]); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF OBJECT_ID(N'[DocumentActivities]', N'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE [DocumentActivities] (" +
            "[DocumentActivityId] int NOT NULL IDENTITY, " +
            "[DocumentId] int NOT NULL, " +
            "[ActorUserId] int NOT NULL, " +
            "[Action] nvarchar(50) NOT NULL, " +
            "[OccurredDate] datetime2 NOT NULL, " +
            "[Details] nvarchar(1000) NULL, " +
            "CONSTRAINT [PK_DocumentActivities] PRIMARY KEY ([DocumentActivityId]), " +
            "CONSTRAINT [FK_DocumentActivities_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_DocumentActivities_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([UserId])" +
            "); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF COL_LENGTH(N'Notifications', N'RelatedDocumentId') IS NULL " +
            "BEGIN " +
            "ALTER TABLE [Notifications] ADD [RelatedDocumentId] int NULL; " +
            "ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Documents_RelatedDocumentId] " +
            "FOREIGN KEY ([RelatedDocumentId]) REFERENCES [Documents] ([DocumentId]) ON DELETE SET NULL; " +
            "END");
    }
}
