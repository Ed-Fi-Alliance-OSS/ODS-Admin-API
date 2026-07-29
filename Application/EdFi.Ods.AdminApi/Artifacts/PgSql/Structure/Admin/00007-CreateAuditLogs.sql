-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE IF NOT EXISTS adminapi.AuditLogs (
    Id BIGINT NOT NULL GENERATED ALWAYS AS IDENTITY,
    EventType VARCHAR(30) NOT NULL,
    "Timestamp" TIMESTAMP NOT NULL,
    ClientId VARCHAR(100),
    SourceIpAddress VARCHAR(45),
    HttpVerb VARCHAR(10),
    HttpUrl VARCHAR(2048),
    StatusCode INT,
    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
);

CREATE INDEX IF NOT EXISTS idx_auditlogs_timestamp
    ON adminapi.AuditLogs ("Timestamp");

CREATE INDEX IF NOT EXISTS idx_auditlogs_clientid
    ON adminapi.AuditLogs (ClientId);
