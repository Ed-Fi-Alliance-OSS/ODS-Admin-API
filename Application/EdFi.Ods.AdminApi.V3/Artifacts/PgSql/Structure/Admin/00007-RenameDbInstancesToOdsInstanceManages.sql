-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'adminapi' AND table_name = 'dbinstances')
       AND NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'adminapi' AND table_name = 'odsinstancemanages')
    THEN
        ALTER TABLE adminapi.DbInstances RENAME TO OdsInstanceManages;
        ALTER TABLE adminapi.OdsInstanceManages RENAME CONSTRAINT pk_dbinstances TO pk_odsinstancemanages;
        ALTER INDEX adminapi.idx_dbinstances_name RENAME TO idx_odsinstancemanages_name;
        ALTER INDEX adminapi.idx_dbinstances_odsinstanceid RENAME TO idx_odsinstancemanages_odsinstanceid;
    END IF;
END $$;
