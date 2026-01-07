-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE IF NOT EXISTS adminapi.EducationOrganizations (
    Id INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    InstanceId int NOT NULL,
    InstanceName VARCHAR(100) NOT NULL,
    EducationOrganizationId int NOT NULL,
    NameOfInstitution VARCHAR(75) NOT NULL,
    ShortNameOfInstitution VARCHAR(75),
    Discriminator VARCHAR(128) NOT NULL,
    ParentId int,
    OdsDatabaseName VARCHAR(255),
    LastRefreshed TIMESTAMP DEFAULT NOW(),
    LastModifiedDate TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_educationorganizations_instanceid
    ON adminapi.EducationOrganizations (InstanceId);

CREATE INDEX IF NOT EXISTS idx_educationorganizations_educationorganizationid
    ON adminapi.EducationOrganizations (EducationOrganizationId);