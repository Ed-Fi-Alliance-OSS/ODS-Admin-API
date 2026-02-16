# Description

> **Implementation Guide**: See `.github/feature-plans/ADMINAPI-1357-education-organization-restructure.md` for detailed AI agent implementation instructions.

The following improvements should be made to `/v2/educationorganizations`, `/v2/educationorganizations/{instanceId}`, and `/v2/tenants/{tenantName}/edOrgsByInstances`

## First, updating the `EducationOrganizationModel`

The response now shows a flat structure where `OdsInstance` properties are at the same level. It's better to have two levels, a parent level with the `OdsInstance` properties (id, name, and instanceType) and a child level with the list of `EducationOrganizations`. The update will remove the data duplication in the `EducationOrganization` items. This applies to the three endpoints.

## Second, renaming the `/v2/tenants/{tenantName}/edOrgsByInstances`

Rename the endpoint from `/v2/tenants/{tenantName}/edOrgsByInstances` to `/v2/tenants/{tenantName}/OdsInstances/edOrgs` to be more consistent and follow REST naming conventions for collections and sub-collection resources.

## Expected

* All EducationOrganization response models must follow the structure detailed in the "Sample response" section below.
* In `/v2/tenants/{tenantName}/edOrgsByInstances`, remove the `TenantEducationOrganizationModel` and instead reuse the `EducationOrganizationModel` to keep consistency among all endpoints' responses.
* Create a new endpoint `/v2/tenants/{tenantName}/OdsInstances/edOrgs` in place of `/v2/tenants/{tenantName}/edOrgsByInstances`.
* Consider changes to unit, integration, and e2e tests.

## Sample response

### Currently

[
  {
    "instanceId": 1,
    "instanceName": "odsinstance1 tenant1",
    "educationOrganizationId": 255901,
    "nameOfInstitution": "Grand Bend ISD",
    "shortNameOfInstitution": "GBISD",
    "discriminator": "edfi.LocalEducationAgency",
    "parentId": 255950
  },
  {
    "instanceId": 1,
    "instanceName": "odsinstance1 tenant1",
    "educationOrganizationId": 255950,
    "nameOfInstitution": "Region 99 Education Service Center",
    "shortNameOfInstitution": null,
    "discriminator": "edfi.EducationServiceCenter",
    "parentId": null
  }
]

### Expected response

[
    {
      "id": 1,
      "name": "Ods-test",
      "instanceType": "OdsInstance",
      "educationOrganizations": [
        {
          "educationOrganizationId": 255901,
          "nameOfInstitution": "Grand Bend ISD",
          "shortNameOfInstitution": "GBISD",
          "discriminator": "edfi.LocalEducationAgency",
          "parentId": 255950
        },
        {
          "educationOrganizationId": 255950,
          "nameOfInstitution": "Region 99 Education Service Center",
          "shortNameOfInstitution": null,
          "discriminator": "edfi.EducationServiceCenter",
          "parentId": null
        }
      ]
    }
  ]
