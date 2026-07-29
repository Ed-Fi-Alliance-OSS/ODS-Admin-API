-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'DbInstances')
   AND NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE TABLE_SCHEMA = 'adminapi' AND TABLE_NAME = 'OdsInstanceManages')
BEGIN
    EXEC sp_rename 'adminapi.DbInstances', 'OdsInstanceManages';
    EXEC sp_rename 'adminapi.PK_DbInstances', 'PK_OdsInstanceManages';
    EXEC sp_rename 'adminapi.OdsInstanceManages.IX_DbInstances_Name', 'IX_OdsInstanceManages_Name', 'INDEX';
    EXEC sp_rename 'adminapi.OdsInstanceManages.IX_DbInstances_OdsInstanceId', 'IX_OdsInstanceManages_OdsInstanceId', 'INDEX';
END
