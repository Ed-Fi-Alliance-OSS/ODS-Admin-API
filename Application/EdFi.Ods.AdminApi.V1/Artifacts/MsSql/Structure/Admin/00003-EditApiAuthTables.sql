-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'Applications')
BEGIN
    ALTER TABLE adminapi.Applications
	ADD
        [ApplicationType] NVARCHAR(MAX) NULL,
        [JsonWebKeySet] NVARCHAR(MAX) NULL,
        [Settings] NVARCHAR(MAX) NULL;
END

IF EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'Applications')
BEGIN
    EXEC sp_rename 'adminapi.Applications.Type', 'ClientType', 'COLUMN';
END
