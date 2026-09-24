-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- OpenIddict 5.0+ renamed Applications.Type to ClientType and added ApplicationType, JsonWebKeySet and Settings.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'adminapi' AND table_name = 'applications' AND column_name = 'type'
    )
    AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'adminapi' AND table_name = 'applications' AND column_name = 'clienttype'
    )
    THEN
        ALTER TABLE adminapi.Applications RENAME COLUMN Type TO ClientType;
    END IF;
END $$;

ALTER TABLE adminapi.Applications
    ADD COLUMN IF NOT EXISTS ApplicationType VARCHAR(50) NULL,
    ADD COLUMN IF NOT EXISTS JsonWebKeySet VARCHAR NULL,
    ADD COLUMN IF NOT EXISTS Settings VARCHAR NULL;
