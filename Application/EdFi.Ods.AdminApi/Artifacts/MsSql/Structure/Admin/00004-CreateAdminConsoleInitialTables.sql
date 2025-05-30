-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminconsole' and TABLE_NAME = 'HealthChecks')
BEGIN
    CREATE TABLE [adminconsole].[HealthChecks] (
        [DocId] int NOT NULL IDENTITY,
        [InstanceId] int NOT NULL,
        [EdOrgId] int NOT NULL,
        [TenantId] int NOT NULL,
        [Document] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_HealthChecks] PRIMARY KEY ([DocId])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminconsole' and TABLE_NAME = 'Instances')
BEGIN
    CREATE TABLE [adminconsole].[Instances] (
        [Id] int NOT NULL IDENTITY,
        [OdsInstanceId] int NULL,
        [TenantId] int NOT NULL,
        [BaseUrl] nvarchar(250) NULL,
        [CompletedAt] datetime2 NULL,
        [Credentials] VARBINARY(500) NULL,
        [InstanceName] nvarchar(100) NOT NULL DEFAULT N'',
        [InstanceType] nvarchar(100) NULL,
        [OAuthUrl] nvarchar(250) NULL,
        [ResourceUrl] nvarchar(250) NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT N'',
        [TenantName] nvarchar(250) NOT NULL DEFAULT N'',
        CONSTRAINT [PK_Instances] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminconsole' and TABLE_NAME = 'OdsInstanceContexts')
BEGIN
    CREATE TABLE [adminconsole].[OdsInstanceContexts] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [ContextKey] nvarchar(50) NOT NULL,
        [ContextValue] nvarchar(50) NOT NULL,
        [InstanceId] int NOT NULL,
        CONSTRAINT [PK_OdsInstanceContexts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OdsInstanceContexts_Instances_InstanceId] FOREIGN KEY ([InstanceId]) REFERENCES [adminconsole].[Instances] ([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminconsole' and TABLE_NAME = 'OdsInstanceDerivatives')
BEGIN
    CREATE TABLE [adminconsole].[OdsInstanceDerivatives] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NOT NULL,
        [DerivativeType] nvarchar(50) NOT NULL,
        [InstanceId] int NOT NULL,
        CONSTRAINT [PK_OdsInstanceDerivatives] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OdsInstanceDerivatives_Instances_InstanceId] FOREIGN KEY ([InstanceId]) REFERENCES [adminconsole].[Instances] ([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_HealthChecks_InstanceId' 
      AND object_id = OBJECT_ID('[adminconsole].[HealthChecks]')
)
BEGIN
    CREATE INDEX [IX_HealthChecks_InstanceId] ON [adminconsole].[HealthChecks] ([InstanceId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_HealthChecks_TenantId' 
      AND object_id = OBJECT_ID('[adminconsole].[HealthChecks]')
)
BEGIN
    CREATE INDEX [IX_HealthChecks_TenantId] ON [adminconsole].[HealthChecks] ([TenantId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Instances_OdsInstanceId' 
      AND object_id = OBJECT_ID('[adminconsole].[Instances]')
)
BEGIN
    CREATE INDEX [IX_Instances_OdsInstanceId] ON [adminconsole].[Instances] ([OdsInstanceId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Instances_TenantId' 
      AND object_id = OBJECT_ID('[adminconsole].[Instances]')
)
BEGIN
    CREATE INDEX [IX_Instances_TenantId] ON [adminconsole].[Instances] ([TenantId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Instances_Status' 
      AND object_id = OBJECT_ID('[adminconsole].[Instances]')
)
BEGIN
    CREATE INDEX [IX_Instances_Status] ON [adminconsole].[Instances] ([Status]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_OdsInstanceContexts_InstanceId' 
      AND object_id = OBJECT_ID('[adminconsole].[OdsInstanceContexts]')
)
BEGIN
    CREATE INDEX [IX_OdsInstanceContexts_InstanceId] ON [adminconsole].[OdsInstanceContexts] ([InstanceId]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_OdsInstanceDerivatives_InstanceId' 
      AND object_id = OBJECT_ID('[adminconsole].[OdsInstanceDerivatives]')
)
BEGIN
    CREATE INDEX [IX_OdsInstanceDerivatives_InstanceId] ON [adminconsole].[OdsInstanceDerivatives] ([InstanceId]);
END
GO
