-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

IF OBJECT_ID('GetEducationOrganizations', 'P') IS NOT NULL
    DROP PROCEDURE GetEducationOrganizations;
GO

CREATE PROCEDURE GetEducationOrganizations
AS
BEGIN
    SELECT
        edorg.educationorganizationid,
        edorg.nameofinstitution,
        edorg.shortnameofinstitution,
        edorg.discriminator,
        edorg.id,
        COALESCE(
            scl.localeducationagencyid,
            lea.parentlocaleducationagencyid,
            lea.educationservicecenterid,
            lea.stateeducationagencyid,
            esc.stateeducationagencyid
        ) AS parentid
    FROM
        edfi.educationorganization edorg
        LEFT JOIN edfi.school scl
            ON edorg.educationorganizationid = scl.schoolid
        LEFT JOIN edfi.localeducationagency lea
            ON edorg.educationorganizationid = lea.localeducationagencyid
        LEFT JOIN edfi.educationservicecenter esc
            ON edorg.educationorganizationid = esc.educationservicecenterid
    WHERE
        edorg.discriminator IN (
            'edfi.StateEducationAgency',
            'edfi.EducationServiceCenter',
            'edfi.LocalEducationAgency',
            'edfi.School'
        );
END
GO
