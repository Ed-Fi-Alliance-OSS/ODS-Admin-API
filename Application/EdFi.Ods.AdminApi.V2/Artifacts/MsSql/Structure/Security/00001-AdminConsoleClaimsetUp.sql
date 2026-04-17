-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- Create Ed-Fi ODS Admin Console ClaimSet

DECLARE @claimSetName nvarchar(32)

SET @claimSetName = 'Ed-Fi ODS Admin Console'

PRINT 'Ensuring Ed-Fi ODS Admin Console Claimset exists.'

INSERT INTO dbo.ClaimSets (ClaimSetName, IsEdfiPreset)
SELECT DISTINCT @claimSetName, 1 FROM dbo.ClaimSets
WHERE NOT EXISTS (SELECT 1
    FROM dbo.ClaimSets
		WHERE ClaimSetName = @claimSetName  )
GO

-- Configure Ed-Fi ODS Admin Console ClaimSet

DECLARE @actionName nvarchar(32)
DECLARE @claimSetName nvarchar(255)
DECLARE @resourceNames TABLE (ResourceName nvarchar(64))
DECLARE @resourceClaimIds TABLE (ResourceClaimId int)
DECLARE @authorizationStrategyId INT
DECLARE @ResourceClaimId INT

SET @claimSetName = 'Ed-Fi ODS Admin Console'

IF  EXISTS (SELECT 1 FROM dbo.ClaimSets c WHERE c.ClaimSetName = @claimSetName)
BEGIN
    DECLARE @edFiOdsAdminConsoleClaimSetId as INT

    SELECT @edFiOdsAdminConsoleClaimSetId = ClaimsetId
    FROM dbo.ClaimSets
    WHERE ClaimSets.ClaimSetName = @claimSetName

    DELETE csrcaaso
    FROM dbo.ClaimSetResourceClaimActionAuthorizationStrategyOverrides csrcaaso
    INNER JOIN dbo.ClaimSetResourceClaimActions ON csrcaaso.ClaimSetResourceClaimActionId = dbo.ClaimSetResourceClaimActions.ClaimSetResourceClaimActionId
    WHERE dbo.ClaimSetResourceClaimActions.ClaimSetId = @edFiOdsAdminConsoleClaimSetId

    DELETE FROM dbo.ClaimSetResourceClaimActions
    WHERE ClaimSetId = @edFiOdsAdminConsoleClaimSetId

    PRINT 'Creating Temporary Records.'
    INSERT INTO @resourceNames VALUES
				('section'),
				('school'),
				('student'),
				('studentSchoolAssociation'),
				('studentSpecialEducationProgramAssociation'),
				('studentDisciplineIncidentBehaviorAssociation'),
				('studentSchoolAssociation'),
				('studentSchoolAttendanceEvent'),
				('studentSectionAssociation'),
				('staffEducationOrganizationAssignmentAssociation'),
				('staffSectionAssociation'),
				('courseTranscript')
    INSERT INTO @resourceClaimIds SELECT ResourceClaimId FROM dbo.ResourceClaims WHERE ResourceName IN (SELECT ResourceName FROM @resourceNames)
END

SELECT @authorizationStrategyId = AuthorizationStrategyId
FROM   dbo.AuthorizationStrategies
WHERE  AuthorizationStrategyName = 'NoFurtherAuthorizationRequired'

DECLARE @actionId int
DECLARE @claimSetId int

SELECT @claimSetId = ClaimSetId FROM dbo.ClaimSets WHERE ClaimSetName = @claimSetName

PRINT 'Configuring Claims for Ed-Fi ODS Admin Console Claimset...'

IF NOT EXISTS (SELECT 1
    FROM dbo.ClaimSetResourceClaimActions csraa,dbo.Actions a, @resourceClaimIds rc
		WHERE csraa.ActionId = a.ActionId AND ClaimSetId = @claimSetId AND csraa.ResourceClaimId = rc.ResourceClaimId)

BEGIN
    INSERT INTO dbo.ClaimSetResourceClaimActions (ActionId, ClaimSetId, ResourceClaimId)
        SELECT ActionId, @claimSetId, rc.ResourceClaimId  
        FROM dbo.Actions, @resourceClaimIds rc
        WHERE ActionName in ('Read')
        AND NOT EXISTS (
            SELECT 1
            FROM dbo.ClaimSetResourceClaimActions
            WHERE ActionId = Actions.ActionId AND ClaimSetId = @claimSetId AND ResourceClaimId = rc.ResourceClaimId
        )

    INSERT INTO dbo.ClaimSetResourceClaimActionAuthorizationStrategyOverrides (AuthorizationStrategyId, ClaimSetResourceClaimActionId)
        SELECT @authorizationStrategyId, ClaimSetResourceClaimActionId
        FROM dbo.ClaimSetResourceClaimActions csrc
            INNER JOIN dbo.ResourceClaims r 
                ON csrc.ResourceClaimId = r.ResourceClaimId AND csrc.ClaimSetId = @claimSetId
        WHERE r.ResourceName IN (
				    'section',
				    'school',
				    'student',
				    'studentSchoolAssociation',
				    'studentSpecialEducationProgramAssociation',
				    'studentDisciplineIncidentBehaviorAssociation',
				    'studentSchoolAssociation',
				    'studentSchoolAttendanceEvent',
				    'studentSectionAssociation',
				    'staffEducationOrganizationAssignmentAssociation',
				    'staffSectionAssociation',
				    'courseTranscript')
END 

SELECT @actionId = ActionId FROM dbo.Actions WHERE ActionName = 'Read'
SELECT @ResourceClaimId = ResourceClaimId FROM dbo.ResourceClaims WHERE ResourceName = 'types'

IF NOT EXISTS (
    SELECT 1 FROM dbo.ClaimSetResourceClaimActions
		WHERE ClaimSetResourceClaimActions.ActionId = @actionId AND ClaimSetResourceClaimActions.ClaimSetId = @claimSetId
			   AND ClaimSetResourceClaimActions.ResourceClaimId = @ResourceClaimId)
BEGIN
    INSERT INTO dbo.ClaimSetResourceClaimActions (ActionId, ClaimSetId, ResourceClaimId)
		    VALUES (@actionId, @claimSetId, @ResourceClaimId)

	  INSERT INTO dbo.ClaimSetResourceClaimActionAuthorizationStrategyOverrides (AuthorizationStrategyId, ClaimSetResourceClaimActionId)
	      SELECT @authorizationStrategyId, ClaimSetResourceClaimActions.ClaimSetResourceClaimActionId
	      FROM dbo.ClaimSetResourceClaimActions
	          INNER JOIN dbo.ResourceClaims r
                ON ClaimSetResourceClaimActions.ResourceClaimId = r.ResourceClaimId
	          INNER JOIN dbo.Actions
                ON Actions.actionId = ClaimSetResourceClaimActions.ActionId AND Actions.ActionName in ('Read')
	      WHERE r.ResourceName IN  ('types') AND ClaimSetResourceClaimActions.ActionId = @actionId AND ClaimSetResourceClaimActions.ClaimSetId = @claimSetId
END
GO
