-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- Adds ApiClients.InheritsApplicationEducationOrganizations. Clients that are not linked to every education
-- organization of their Application are backfilled to false (not inheriting). The backfill runs only when this
-- script creates the column; if the column already exists its values are left untouched.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'dbo' AND table_name = 'apiclients' AND column_name = 'inheritsapplicationeducationorganizations'
    )
    THEN
        ALTER TABLE dbo.ApiClients ADD COLUMN InheritsApplicationEducationOrganizations BOOLEAN NOT NULL DEFAULT TRUE;

        UPDATE dbo.ApiClients c
        SET InheritsApplicationEducationOrganizations = FALSE
        WHERE EXISTS (
            SELECT 1
            FROM dbo.ApplicationEducationOrganizations aeo
            WHERE aeo.Application_ApplicationId = c.Application_ApplicationId
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.ApiClientApplicationEducationOrganizations link
                  WHERE link.ApiClient_ApiClientId = c.ApiClientId
                    AND link.ApplicationEdOrg_ApplicationEdOrgId = aeo.ApplicationEducationOrganizationId
              )
        );
    END IF;
END $$;
