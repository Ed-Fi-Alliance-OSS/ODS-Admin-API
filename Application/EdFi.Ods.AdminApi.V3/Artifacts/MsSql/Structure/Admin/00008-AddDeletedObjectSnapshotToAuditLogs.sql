-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF NOT EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'AuditLogs' AND COLUMN_NAME = 'DeletedObjectSnapshot'
)
BEGIN
ALTER TABLE [adminapi].[AuditLogs]
    ADD [DeletedObjectSnapshot] NVARCHAR(MAX) NULL;
END
