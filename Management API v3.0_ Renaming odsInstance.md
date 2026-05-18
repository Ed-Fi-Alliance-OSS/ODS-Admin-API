
# Mission

We need to focus on our V3 Application\EdFi.Ods.AdminApi.V3 and associated tests in Application\EdFi.Ods.AdminApi.V3.DBTests (integration tests), Application\EdFi.Ods.AdminApi.V3.UnitTests (unit tests) and Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0\v3 (end to end tests).

The project has a number of endpoints mapped in Application\EdFi.Ods.AdminApi.V3\Features. We need to change the endpoints in Application\EdFi.Ods.AdminApi.V3\Features\OdsInstances, Application\EdFi.Ods.AdminApi.V3\Features\OdsInstanceContext and Application\EdFi.Ods.AdminApi.V3\Features\OdsInstanceDerivative.

1. /odsInstances will become /dataStores
2. /odsInstanceContexts will become /dataStoreContexts
3. /odsInstanceDerivatives will become /dataStoreDerivatives

## Important

1. Associated models, PTOs and fields will need to change as well.
2. Database structures and schemas won't change.

## Examples for Endpoint changes

| current endpoint | new endpoint |
| --- | --- |
| GET /v2/odsInstances | GET /v3/dataStores |
| POST /v2/odsInstances | POST /v3/dataStores |
| GET /v2/odsInstances/{id} | GET /v3/dataStores/{id} |
| PUT /v2/odsInstances/{id} | PUT /v3/dataStores/{id} |
| DELETE /v2/odsInstances/{id} | DELETE /v3/dataStores/{id}  |
| GET /v2/odsInstanceContexts | GET /v3/dataStoreContexts |
| POST /v2/odsInstanceContexts | POST /v3/dataStoreContexts |
| GET /v2/odsInstanceContexts/{id} | GET /v3/dataStoreContexts/{id} |
| PUT /v2/odsInstanceContexts/{id} | PUT /v3/dataStoreContexts/{id} |
| DELETE /v2/odsInstanceContexts/{id} | DELETE /v3/dataStoreContexts/{id} |
| GET /v2/odsInstanceDerivatives | GET /v3/dataStoreDerivatives |
| POST /v2/odsInstanceDerivatives | POST /v3/dataStoreDerivatives |
| GET /v2/odsInstanceDerivatives/{id} | GET /v3/dataStoreDerivatives/{id} |
| PUT /v2/odsInstanceDerivatives/{id} | PUT /v3/dataStoreDerivatives/{id} |
| DELETE /v2/odsInstanceDerivatives/{id} | DELETE /v3/dataStoreDerivatives/{id} |

## Other references

References to odsInstanceIds in the applications and apiClients resources also change to dataStoreIds.

## Examples

### Examples for Field-level changes

| current field | new field | Appears in |
| --- | --- | --- |
| odsInstanceId | dataStoreId | context and derivative bodies |
| odsInstanceIds | dataStoreIds | application and API client bodies |
| odsInstanceContexts | dataStoreContexts | |
| odsInstanceDerivatives | dataStoreDerivatives | |
| instanceType | dataStoreType | |

### Example payloads

GET /v3/dataStores/42 — retrieve a data store with its children

{
  "id": 42,
  "name": "District-2025",
  "dataStoreType": "ods",
  "dataStoreContexts": [
    {
      "id": 7,
      "dataStoreId": 42,
      "contextKey": "schoolYear",
      "contextValue": "2025"
    }
  ],
  "dataStoreDerivatives": [
    {
      "id": 3,
      "dataStoreId": 42,
      "derivativeType": "ReadReplica",
      "connectionString": "Server=replica.example.com;Database=EdFi_District2025;..."
    }
  ]
}

POST /v3/dataStoreContexts — add routing context to a data store

{
  "dataStoreId": 42,
  "contextKey": "schoolYear",
  "contextValue": "2025"
}

POST /v3/applications — create an application referencing data stores

The dataStoreIds field (formerly odsInstanceIds) associates the application's API client credentials with one or more data stores.

{
  "applicationName": "SIS Integration",
  "vendorId": 5,
  "claimSetName": "SIS-Vendor",
  "educationOrganizationIds": [255901],
  "dataStoreIds": [42]
}
