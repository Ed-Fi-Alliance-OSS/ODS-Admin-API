-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE IF NOT EXISTS adminapi.DbInstances (
    Id INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    Name VARCHAR(100) NOT NULL,
    OdsInstanceId INT,
    OdsInstanceName VARCHAR(100),
    Status VARCHAR(75) NOT NULL,
    DatabaseTemplate VARCHAR(100) NOT NULL,
    DatabaseName VARCHAR(255),
    LastRefreshed TIMESTAMP NOT NULL DEFAULT NOW(),
    LastModifiedDate TIMESTAMP,
    CONSTRAINT PK_DbInstances PRIMARY KEY (Id)
);
