-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- OpenIddict 5.0+ renamed Applications.Type to ClientType and added ApplicationType, JsonWebKeySet and Settings.

IF EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'Applications' AND COLUMN_NAME = 'Type'
)
AND NOT EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'Applications' AND COLUMN_NAME = 'ClientType'
)
BEGIN
EXEC sp_rename 'adminapi.Applications.Type', 'ClientType', 'COLUMN';
END

IF NOT EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'Applications' AND COLUMN_NAME = 'ApplicationType'
)
BEGIN
ALTER TABLE [adminapi].[Applications]
    ADD [ApplicationType] NVARCHAR(50) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'Applications' AND COLUMN_NAME = 'JsonWebKeySet'
)
BEGIN
ALTER TABLE [adminapi].[Applications]
    ADD [JsonWebKeySet] NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'Applications' AND COLUMN_NAME = 'Settings'
)
BEGIN
ALTER TABLE [adminapi].[Applications]
    ADD [Settings] NVARCHAR(MAX) NULL;
END
