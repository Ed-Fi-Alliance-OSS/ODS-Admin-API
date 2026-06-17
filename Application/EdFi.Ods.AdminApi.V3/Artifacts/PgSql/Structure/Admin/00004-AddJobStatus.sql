-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE IF NOT EXISTS adminapi.JobStatuses (
    Id SERIAL PRIMARY KEY,
    JobId VARCHAR(150) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    ErrorMessage VARCHAR(1000)
);

-- Unique constraint to prevent duplicate JobId
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_jobstatuses_jobid'
          AND conrelid = 'adminapi.jobstatuses'::regclass
    ) THEN
        ALTER TABLE adminapi.JobStatuses
        ADD CONSTRAINT UQ_JobStatuses_JobId UNIQUE (JobId);
    END IF;
END $$;