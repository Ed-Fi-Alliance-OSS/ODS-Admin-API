-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- Adds ApiClients.InheritsApplicationEducationOrganizations. Clients that are not linked to every education
-- organization of their Application are backfilled to 0 (not inheriting). The backfill runs only when this
-- script creates the column; if the column already exists its values are left untouched.

IF NOT EXISTS (
    SELECT 1 FROM [INFORMATION_SCHEMA].[COLUMNS]
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'ApiClients' AND COLUMN_NAME = 'InheritsApplicationEducationOrganizations'
)
BEGIN
ALTER TABLE [dbo].[ApiClients]
    ADD [InheritsApplicationEducationOrganizations] BIT NOT NULL
    CONSTRAINT [DF_ApiClients_InheritsApplicationEducationOrganizations] DEFAULT (1);

-- Dynamic SQL because the column does not exist when this batch is compiled.
EXEC sp_executesql N'
UPDATE c
SET c.InheritsApplicationEducationOrganizations = 0
FROM dbo.ApiClients c
WHERE EXISTS (
    SELECT 1
    FROM dbo.ApplicationEducationOrganizations aeo
    WHERE aeo.Application_ApplicationId = c.Application_ApplicationId
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ApiClientApplicationEducationOrganizations link
          WHERE link.ApiClient_ApiClientId = c.ApiClientId
            AND link.ApplicationEducationOrganization_ApplicationEducationOrganizationId = aeo.ApplicationEducationOrganizationId
      )
);';
END
