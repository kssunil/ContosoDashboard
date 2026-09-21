using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public static class ProjectSchemaInitializer
{
    public static void EnsureCreated(ApplicationDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            "IF COL_LENGTH(N'Projects', N'Progress') IS NULL " +
            "BEGIN " +
            "ALTER TABLE [Projects] ADD [Progress] int NOT NULL CONSTRAINT [DF_Projects_Progress] DEFAULT 0; " +
            "END");

        // Each step below is a separate batch (its own ExecuteSqlRaw call) because SQL Server
        // resolves column names against the schema as it existed when the batch was compiled:
        // referencing a column added earlier in the same batch fails with "Invalid column name".
        context.Database.ExecuteSqlRaw(
            "IF COL_LENGTH(N'Projects', N'CreatedByUserId') IS NULL " +
            "BEGIN " +
            "ALTER TABLE [Projects] ADD [CreatedByUserId] int NULL; " +
            "END");

        context.Database.ExecuteSqlRaw(
            "UPDATE [Projects] SET [CreatedByUserId] = [ProjectManagerId] WHERE [CreatedByUserId] IS NULL;");

        context.Database.ExecuteSqlRaw(
            "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = N'CreatedByUserId' AND is_nullable = 1) " +
            "BEGIN " +
            "ALTER TABLE [Projects] ALTER COLUMN [CreatedByUserId] int NOT NULL; " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Projects_Users_CreatedByUserId') " +
            "BEGIN " +
            "ALTER TABLE [Projects] ADD CONSTRAINT [FK_Projects_Users_CreatedByUserId] " +
            "FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([UserId]); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Projects_Name' AND object_id = OBJECT_ID(N'[Projects]')) " +
            "BEGIN " +
            "CREATE UNIQUE INDEX [IX_Projects_Name] ON [Projects] ([Name]); " +
            "END");

        context.Database.ExecuteSqlRaw(
            "IF OBJECT_ID(N'[ProjectActivities]', N'U') IS NULL " +
            "BEGIN " +
            "CREATE TABLE [ProjectActivities] (" +
            "[ProjectActivityId] int NOT NULL IDENTITY, " +
            "[ProjectId] int NOT NULL, " +
            "[ActorUserId] int NOT NULL, " +
            "[Action] nvarchar(50) NOT NULL, " +
            "[OccurredDate] datetime2 NOT NULL, " +
            "[Details] nvarchar(1000) NULL, " +
            "CONSTRAINT [PK_ProjectActivities] PRIMARY KEY ([ProjectActivityId]), " +
            "CONSTRAINT [FK_ProjectActivities_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([ProjectId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_ProjectActivities_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([UserId])" +
            "); " +
            "END");
    }
}
