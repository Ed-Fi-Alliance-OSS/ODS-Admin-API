-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'AuditLogs')
BEGIN
CREATE TABLE [adminapi].[AuditLogs] (
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [EventType] NVARCHAR(30) NOT NULL,
    [Timestamp] DATETIME2 NOT NULL,
    [ClientId] NVARCHAR(256) NULL,
    [SourceIpAddress] NVARCHAR(45) NULL,
    [HttpVerb] NVARCHAR(10) NULL,
    [HttpUrl] NVARCHAR(2048) NULL,
    [StatusCode] INT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);

CREATE NONCLUSTERED INDEX [IX_AuditLogs_Timestamp]
    ON [adminapi].[AuditLogs] ([Timestamp]);

CREATE NONCLUSTERED INDEX [IX_AuditLogs_ClientId]
    ON [adminapi].[AuditLogs] ([ClientId]);
END
