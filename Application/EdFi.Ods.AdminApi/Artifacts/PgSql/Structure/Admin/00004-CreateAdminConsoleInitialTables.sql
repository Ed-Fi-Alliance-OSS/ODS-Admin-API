-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE IF NOT EXISTS adminconsole."HealthChecks" (
    "DocId" INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    "InstanceId" INT NOT NULL,
    "EdOrgId" INT NOT NULL,
    "TenantId" INT NOT NULL,
    "Document" VARCHAR NOT NULL,
	CONSTRAINT PK_HealthChecks PRIMARY KEY ("DocId")
);

CREATE TABLE IF NOT EXISTS adminconsole."Instances" (
    "Id" INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    "OdsInstanceId" INT NULL,
    "TenantId" INT NOT NULL,
    "BaseUrl" VARCHAR(250) NULL,
    "CompletedAt" TIMESTAMP NULL,
    "Credentials" BYTEA NULL,
    "InstanceName" VARCHAR(100) NOT NULL DEFAULT '',
    "InstanceType" VARCHAR(100) NULL,
    "OAuthUrl" VARCHAR(250) NULL,
    "ResourceUrl" VARCHAR(250) NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT '',
    "TenantName" VARCHAR(250) NOT NULL DEFAULT '',
	CONSTRAINT PK_Instances PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS adminconsole."OdsInstanceContexts" (
    "Id" INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    "TenantId" INT NOT NULL,
    "ContextKey" VARCHAR(50) NOT NULL,
    "ContextValue" VARCHAR(50) NOT NULL,
    "InstanceId" INT NOT NULL,
    CONSTRAINT FK_OdsInstanceContexts_Instances_InstanceId FOREIGN KEY ("InstanceId") REFERENCES adminconsole."Instances" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS adminconsole."OdsInstanceDerivatives" (
    "Id" INT NOT NULL GENERATED ALWAYS AS IDENTITY,
    "TenantId" INT NOT NULL,
    "DerivativeType" VARCHAR(50) NOT NULL,
    "InstanceId" INT NOT NULL,
    CONSTRAINT FK_OdsInstanceDerivatives_Instances_InstanceId FOREIGN KEY ("InstanceId") REFERENCES adminconsole."Instances" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_HealthChecks_InstanceId" ON adminconsole."HealthChecks" ("InstanceId");

CREATE INDEX IF NOT EXISTS "IX_HealthChecks_TenantId" ON adminconsole."HealthChecks" ("TenantId");

CREATE INDEX IF NOT EXISTS "IX_Instances_OdsInstanceId" ON adminconsole."Instances" ("OdsInstanceId");

CREATE INDEX IF NOT EXISTS "IX_Instances_TenantId" ON adminconsole."Instances" ("TenantId");

CREATE INDEX IF NOT EXISTS "IX_Instances_Status" ON adminconsole."Instances" ("Status");

CREATE INDEX IF NOT EXISTS "IX_OdsInstanceContexts_InstanceId" ON adminconsole."OdsInstanceContexts" ("InstanceId");

CREATE INDEX IF NOT EXISTS "IX_OdsInstanceDerivatives_InstanceId" ON adminconsole."OdsInstanceDerivatives" ("InstanceId");
