-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- Adds ApiClients.InheritsApplicationEducationOrganizations. Clients that are not linked to every education
-- organization of their Application are backfilled to false (not inheriting), so no client's access changes on upgrade.
-- That includes clients with no links at all (for example credentials created without links by released versions):
-- they keep no access until they are explicitly set to inherit.
-- Clients whose Application has no education organizations, or with a NULL Application, keep true.
-- The backfill runs whether or not this script creates the column, so a database where Ed-Fi-ODS (ODS-6912) already
-- added the column with every client at the default is still backfilled. AdminApiDeployJournal runs this script once.
-- The backfill only sets false, never true, so it can't widen a client's access, and running it again by hand is
-- harmless. The DO block runs atomically, so a failed backfill leaves the database as it was and a rerun starts over.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'dbo' AND table_name = 'apiclients' AND column_name = 'inheritsapplicationeducationorganizations'
    )
    THEN
        ALTER TABLE dbo.ApiClients ADD COLUMN InheritsApplicationEducationOrganizations BOOLEAN NOT NULL DEFAULT TRUE;
    END IF;

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
END $$;
