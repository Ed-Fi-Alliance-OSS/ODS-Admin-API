-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'JobStatuses')
BEGIN
CREATE TABLE [adminapi].[JobStatuses] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [JobId] NVARCHAR(150) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL,
    [ErrorMessage] NVARCHAR(1000) NULL
);
END

-- Unique constraint to prevent duplicate JobId
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE [type] = 'UQ'
      AND [name] = 'UQ_JobStatuses_JobId'
)
BEGIN
ALTER TABLE [adminapi].[JobStatuses]
ADD CONSTRAINT UQ_JobStatuses_JobId UNIQUE ([JobId]);
END
