-- SPDX-License-Identifier: Apache-2.0
-- Licensed to the Ed-Fi Alliance under one or more agreements.
-- The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
-- See the LICENSE and NOTICES files in the project root for more information.

-- Create Ed-Fi ODS Admin Console ClaimSet
DO $$
    DECLARE claimset_name varchar(50) := 'Ed-Fi ODS Admin Console';
    DECLARE claimset_id int;
	DECLARE authorizationStrategy_id int;
BEGIN

	-- Creating Ed-Fi ODS Admin Console claim set
	IF EXISTS (SELECT 1 FROM dbo.claimsets WHERE claimsetname = claimset_name)
    THEN
        RAISE NOTICE '% claimset exists', claimset_name;
    ELSE
        RAISE NOTICE 'adding % claimset', claimset_name;
        INSERT INTO dbo.ClaimSets (ClaimSetName, isedfipreset) VALUES (claimset_name, True);
    END IF;

-- Configure Ed-Fi ODS Admin Console ClaimSet

	SELECT claimsetid INTO claimset_id
    FROM dbo.claimsets
    WHERE claimsetname = claimset_name;
	
	DELETE  
	FROM dbo.ClaimSetResourceClaimActionAuthorizationStrategyOverrides csrcaaso
	USING dbo.ClaimSetResourceClaimActions csrc
	WHERE csrcaaso.ClaimSetResourceClaimActionId = csrc.ClaimSetResourceClaimActionId AND csrc.ClaimSetId = claimset_id;

	DELETE FROM dbo.ClaimSetResourceClaimActions WHERE ClaimSetId = claimset_id;
	
	SELECT authorizationstrategyid INTO authorizationStrategy_id
    FROM dbo.authorizationstrategies
    WHERE authorizationstrategyname = 'NoFurtherAuthorizationRequired';

    IF EXISTS (SELECT 1 FROM dbo.ClaimSetResourceClaimActions WHERE ClaimSetId = claimset_id)
    THEN
        RAISE NOTICE 'claims already exist for claim %', claimset_name;
    ELSE
        RAISE NOTICE 'Configuring Claims for % Claimset...', claimset_name;
        INSERT INTO dbo.ClaimSetResourceClaimActions
            (ActionId
            ,ClaimSetId
            ,ResourceClaimId)
        SELECT ac.actionid, claimset_id, resourceclaimid
        FROM dbo.resourceclaims
        INNER JOIN LATERAL
            (
				SELECT actionid
            	FROM dbo.actions
            	WHERE actionname in ('Read')
			) AS ac ON true
        WHERE resourcename IN 
			(
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
				'courseTranscript'
			);
		
		INSERT INTO dbo.ClaimSetResourceClaimActionAuthorizationStrategyOverrides
            (
				AuthorizationStrategyId
            	,ClaimSetResourceClaimActionId
			)
        SELECT authorizationStrategy_id, csrc.ClaimSetResourceClaimActionId
        FROM dbo.ClaimSetResourceClaimActions csrc
        INNER JOIN dbo.ResourceClaims r 
			ON csrc.ResourceClaimId = r.ResourceClaimId AND csrc.ClaimSetId = claimset_id
        WHERE r.resourcename IN 
			(
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
				'courseTranscript'
			);
		
    END IF;	
	
	INSERT INTO dbo.ClaimSetResourceClaimActions
		(ActionId
		,ClaimSetId
		,ResourceClaimId)
	SELECT ac.actionid, claimset_id, resourceclaimid
	FROM dbo.resourceclaims
	INNER JOIN LATERAL
		(
			SELECT actionid
			FROM dbo.actions
			WHERE actionname in ('Read')
		) AS ac ON true
	WHERE resourcename IN ('types');
	
	INSERT INTO dbo.ClaimSetResourceClaimActionAuthorizationStrategyOverrides
		(AuthorizationStrategyId
		,ClaimSetResourceClaimActionId)
	SELECT authorizationStrategy_id, csrc.ClaimSetResourceClaimActionId
	FROM dbo.ClaimSetResourceClaimActions csrc
	INNER JOIN dbo.ResourceClaims r ON csrc.ResourceClaimId = r.ResourceClaimId
	INNER JOIN dbo.Actions a ON a.ActionId = csrc.ActionId AND a.ActionName in ('Read')
	WHERE resourcename IN ('types') AND csrc.ActionId = a.ActionId AND csrc.ClaimSetId = claimset_id;
	
END $$;
