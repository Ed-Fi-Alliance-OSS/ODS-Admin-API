-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'Applications')
BEGIN
    CREATE TABLE adminapi.Applications (
        [Id] int identity NOT NULL,
        [ConcurrencyToken] NVARCHAR(128) NULL,
        [ClientId] NVARCHAR(256) NULL,
        [ClientSecret] NVARCHAR(256) NULL,
        [Type] NVARCHAR(256) NULL,
        [ConsentType] NVARCHAR(256) NULL,
        [Permissions] NVARCHAR(MAX) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [Requirements] NVARCHAR(MAX) NULL,
        [DisplayName] NVARCHAR(256) NULL,
        [DisplayNames] NVARCHAR(MAX) NULL,
        [RedirectUris] NVARCHAR(MAX) NULL,
        [PostLogoutRedirectUris] NVARCHAR(MAX) NULL,
        CONSTRAINT PK_Applications PRIMARY KEY (Id)
    );
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'Scopes')
BEGIN
    CREATE TABLE adminapi.Scopes (
        [Id] int identity NOT NULL,
        [Name] NVARCHAR(256) NULL,
        [ConcurrencyToken] NVARCHAR(128) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Descriptions] NVARCHAR(MAX) NULL,
        [DisplayName] NVARCHAR(256) NULL,
        [DisplayNames] NVARCHAR(MAX) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [Resources] NVARCHAR(MAX) NULL,
        CONSTRAINT PK_Scopes PRIMARY KEY (Id)
    );
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'Authorizations')
BEGIN
    CREATE TABLE adminapi.Authorizations (
        [Id] int identity NOT NULL,
        [ConcurrencyToken] NVARCHAR(128) NULL,
        [ApplicationId] int NOT NULL,
        [Scopes] NVARCHAR(MAX) NULL,
        [Subject] NVARCHAR(256) NULL,
        [Status] NVARCHAR(256) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [CreationDate] DATETIME NULL,
        CONSTRAINT PK_Authorizations PRIMARY KEY (Id),
        CONSTRAINT FK_AuthorizationsId_ApplicationId FOREIGN KEY (ApplicationId) REFERENCES adminapi.Applications (Id) ON DELETE NO ACTION,
    );
END

IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' and TABLE_NAME = 'Tokens')
BEGIN
    CREATE TABLE adminapi.Tokens (
        [Id] int identity NOT NULL,
        [ConcurrencyToken] NVARCHAR(128) NULL,
        [ApplicationId] int NULL,
        [AuthorizationId] int NULL,
        [Type] NVARCHAR(256) NULL,
        [CreationDate] DATETIME NULL,
        [ExpirationDate] DATETIME NULL,
        [RedemptionDate] DATETIME NULL,
        [Payload] NVARCHAR(MAX) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [Subject] NVARCHAR(256) NULL,
        [Status] NVARCHAR(256) NULL,
        [ReferenceId] NVARCHAR(256) NULL,
        CONSTRAINT PK_Tokens PRIMARY KEY (Id)
    );
END