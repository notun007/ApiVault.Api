BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    CREATE TABLE [API_PROJECT] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2(6) NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [UpdatedAtUtc] datetime2(6) NULL,
        [UpdatedBy] nvarchar(200) NULL,
        CONSTRAINT [PK_API_PROJECT] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    CREATE UNIQUE INDEX [IX_API_PROJECT_Code] ON [API_PROJECT] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    CREATE UNIQUE INDEX [IX_API_PROJECT_Name] ON [API_PROJECT] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN

                    INSERT INTO [API_PROJECT]
                        ([Id], [Code], [Name], [Description], [IsActive],
                         [CreatedAtUtc], [CreatedBy], [UpdatedAtUtc], [UpdatedBy])
                    SELECT NEWID(),
                        LEFT('LEGACY-' + CONVERT(varchar(64), HASHBYTES('SHA2_256', [ApiProjectName]), 2), 100),
                        [ApiProjectName], NULL, CAST(1 AS bit), SYSUTCDATETIME(),
                        'migration:AddApiProject', NULL, NULL
                    FROM [API_ASSET]
                    GROUP BY [ApiProjectName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    ALTER TABLE [API_ASSET] ADD [ApiProjectId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN

                    UPDATE asset
                    SET [ApiProjectId] = project.[Id]
                    FROM [API_ASSET] asset
                    INNER JOIN [API_PROJECT] project ON project.[Name] = asset.[ApiProjectName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN

                    IF EXISTS (SELECT 1 FROM [API_ASSET] WHERE [ApiProjectId] IS NULL)
                        THROW 51000, 'ApiProject migration could not map every existing API asset to a project.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    DROP INDEX [IX_API_ASSET_Name_ApiProjectName] ON [API_ASSET];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[API_ASSET]') AND [c].[name] = N'ApiProjectName');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [API_ASSET] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [API_ASSET] DROP COLUMN [ApiProjectName];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[API_ASSET]') AND [c].[name] = N'ApiProjectId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [API_ASSET] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [API_ASSET] ALTER COLUMN [ApiProjectId] uniqueidentifier NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    CREATE INDEX [IX_API_ASSET_ApiProjectId] ON [API_ASSET] ([ApiProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    CREATE UNIQUE INDEX [IX_API_ASSET_Name_ApiProjectId] ON [API_ASSET] ([Name], [ApiProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    ALTER TABLE [API_ASSET] ADD CONSTRAINT [FK_API_ASSET_API_PROJECT_ApiProjectId] FOREIGN KEY ([ApiProjectId]) REFERENCES [API_PROJECT] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729092850_AddApiProject'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729092850_AddApiProject', N'10.0.10');
END;

COMMIT;
GO

