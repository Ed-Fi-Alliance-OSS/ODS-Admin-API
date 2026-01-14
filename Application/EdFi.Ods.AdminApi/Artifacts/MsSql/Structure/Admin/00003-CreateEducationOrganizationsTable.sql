-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

CREATE TABLE [adminapi].[EducationOrganizations] (
      [Id] INT IDENTITY(1,1) NOT NULL,
      [InstanceId] INT NOT NULL,
      [InstanceName] NVARCHAR(100) NOT NULL,
      [EducationOrganizationId] BIGINT NOT NULL,
      [NameOfInstitution] NVARCHAR(75) NOT NULL,
      [ShortNameOfInstitution] NVARCHAR(75) NULL,
      [Discriminator] NVARCHAR(128) NOT NULL,
      [ParentId] BIGINT NULL,
      [OdsDatabaseName] NVARCHAR(255) NULL,
      [LastRefreshed] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
      [LastModifiedDate] DATETIME2 NULL,
      CONSTRAINT [PK_EducationOrganizations] PRIMARY KEY ([Id])
  );

CREATE NONCLUSTERED INDEX [IX_EducationOrganizations_InstanceId]
    ON [adminapi].[EducationOrganizations] ([InstanceId]);

CREATE NONCLUSTERED INDEX [IX_EducationOrganizations_EducationOrganizationId]
    ON [adminapi].[EducationOrganizations] ([EducationOrganizationId]);