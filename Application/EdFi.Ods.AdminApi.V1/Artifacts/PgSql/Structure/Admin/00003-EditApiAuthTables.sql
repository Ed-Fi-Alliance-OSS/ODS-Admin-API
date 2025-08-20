-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

ALTER TABLE adminapi.Applications
    ADD COLUMN
	    ApplicationType VARCHAR NULL,
    ADD COLUMN
	    JsonWebKeySet VARCHAR NULL,
    ADD COLUMN
	    Settings VARCHAR NULL;

ALTER TABLE adminapi.Applications 
    RENAME COLUMN Type TO ClientType;
