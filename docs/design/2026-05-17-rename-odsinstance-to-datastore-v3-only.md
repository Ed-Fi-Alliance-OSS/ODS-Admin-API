# Design: Rename OdsInstance to DataStore (Management API v3.0)

## Problem Statement

The V3 Management API needs to rename the `odsInstance*` resource family to `dataStore*` across endpoint paths, JSON field names, C# models/DTOs, and supporting infrastructure. Database schemas remain unchanged.

## Approach

Incremental feature-area-by-area rename (Approach A): rename one feature area at a time so each step compiles and tests pass independently. Easier to review and isolate regressions.

## Scope

### In scope (V3 only)

* `Application/EdFi.Ods.AdminApi.V3` — Features, Infrastructure

* `Application/EdFi.Ods.AdminApi.V3.UnitTests`
* `Application/EdFi.Ods.AdminApi.V3.DBTests`
* `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3`

### Out of scope

* `EdFi.Ods.AdminApi.V1`, `EdFi.Ods.AdminApi` (V1 and non-versioned endpoints unchanged)

* `EdFi.Ods.AdminApi.Common` — shared infrastructure unchanged
* Database schema and EF Core entity classes (`OdsInstance`, `OdsInstanceContext`, `OdsInstanceDerivative` in `EdFi.Admin.DataAccess`)

## Rename Mapping

### Endpoint Routes

| Old | New |
| --- | --- |
| `GET/POST /v3/odsInstances` | `GET/POST /v3/dataStores` |
| `GET/PUT/DELETE /v3/odsInstances/{id}` | `GET/PUT/DELETE /v3/dataStores/{id}` |
| `GET /v3/odsInstances/{id}/applications` | `GET /v3/dataStores/{id}/applications` |
| `GET/POST /v3/odsInstanceContexts` | `GET/POST /v3/dataStoreContexts` |
| `GET/PUT/DELETE /v3/odsInstanceContexts/{id}` | `GET/PUT/DELETE /v3/dataStoreContexts/{id}` |
| `GET/POST /v3/odsInstanceDerivatives` | `GET/POST /v3/dataStoreDerivatives` |
| `GET/PUT/DELETE /v3/odsInstanceDerivatives/{id}` | `GET/PUT/DELETE /v3/dataStoreDerivatives/{id}` |

### JSON Field Names

| Old field | New field | Appears in |
| --- | --- | --- |
| `instanceType` | `dataStoreType` | DataStore response body |
| `odsInstanceId` | `dataStoreId` | DataStoreContext and DataStoreDerivative bodies |
| `odsInstanceIds` | `dataStoreIds` | Application and ApiClient bodies |
| `odsInstanceContexts` | `dataStoreContexts` | DataStoreDetail response body |
| `odsInstanceDerivatives` | `dataStoreDerivatives` | DataStoreDetail response body |

### Feature Folders & Files

| Old | New |
| --- | --- |
| `Features/OdsInstances/` | `Features/DataStores/` |
| `Features/OdsInstanceContext/` | `Features/DataStoreContexts/` |
| `Features/OdsInstanceDerivative/` | `Features/DataStoreDerivatives/` |
| `AddOdsInstance.cs` | `AddDataStore.cs` |
| `ReadOdsInstance.cs` | `ReadDataStore.cs` |
| `EditOdsInstance.cs` | `EditDataStore.cs` |
| `DeleteOdsInstance.cs` | `DeleteDataStore.cs` |
| `OdsInstanceModel.cs` | `DataStoreModel.cs` |
| `OdsInstanceMapper.cs` | `DataStoreMapper.cs` |
| etc | |

### C# Classes

| Old | New |
| --- | --- |
| `OdsInstanceModel` | `DataStoreModel` |
| `OdsInstanceDetailModel` | `DataStoreDetailModel` |
| `OdsInstanceContextModel` | `DataStoreContextModel` |
| `OdsInstanceDerivativeModel` | `DataStoreDerivativeModel` |
| `AddOdsInstance` | `AddDataStore` |
| `ReadOdsInstance` | `ReadDataStore` |
| `EditOdsInstance` | `EditDataStore` |
| `DeleteOdsInstance` | `DeleteDataStore` |
| `OdsInstanceMapper` | `DataStoreMapper` |
| `OdsInstanceContextMapper` | `DataStoreContextMapper` |
| `OdsInstanceDerivativeMapper` | `DataStoreDerivativeMapper` |
| `AddOdsInstanceRequest` | `AddDataStoreRequest` |
| etc. | |

### Infrastructure Layer (Queries & Commands in V3)

All interfaces and implementations in `Infrastructure/Database/Queries` and `Infrastructure/Database/Commands` that reference `OdsInstance` in their names are renamed. For example:

* `IAddOdsInstanceCommand` → `IAddDataStoreCommand`
* `IGetOdsInstancesQuery` → `IGetDataStoresQuery`
* `IGetOdsInstanceQuery` → `IGetDataStoreQuery`
* `IGetOdsInstanceContextsQuery` → `IGetDataStoreContextsQuery`
* etc.

### SwaggerSchema Titles

| Old | New |
|---|---|
| `[SwaggerSchema(Title = "OdsInstance")]` | `[SwaggerSchema(Title = "DataStore")]` |
| `[SwaggerSchema(Title = "OdsInstanceDetail")]` | `[SwaggerSchema(Title = "DataStoreDetail")]` |
| `[SwaggerSchema(Title = "OdsInstanceContext")]` | `[SwaggerSchema(Title = "DataStoreContext")]` |
| `[SwaggerSchema(Title = "OdsInstanceDerivative")]` | `[SwaggerSchema(Title = "DataStoreDerivative")]` |
| `[SwaggerSchema(Title = "AddOdsInstanceRequest")]` | `[SwaggerSchema(Title = "AddDataStoreRequest")]` |
| (and equivalent Edit/Add request schemas) | |

### E2E Bruno Tests

Rename folders and `.bru` files under `E2E Tests/Bruno Admin API E2E 3.0/v3/`:

* `OdsInstances/` → `DataStores/`
* `OdsInstanceContexts/` → `DataStoreContexts/`
* `OdsInstanceDerivatives/` → `DataStoreDerivatives/`
* `Multitenant Isolation - OdsInstances/` → `Multitenant Isolation - DataStores/`
* Update all URL paths and JSON body field names inside `.bru` files

## Execution Order

1. **DataStores** — rename `OdsInstances` folder, files, classes, routes, mapper, and Infrastructure commands/queries
2. **DataStoreContexts** — rename `OdsInstanceContext` folder, files, classes, routes, mapper
3. **DataStoreDerivatives** — rename `OdsInstanceDerivative` folder, files, classes, routes, mapper
4. **Cross-cutting fields** — rename `OdsInstanceIds` → `DataStoreIds` in Applications and ApiClients (models, requests, mapper, validators, error messages)
5. **FeatureConstants** — update V3 `FeatureConstants.cs` constant names and description strings
6. **Unit Tests** — update test files to use new class/property names
7. **E2E Tests** — rename Bruno test folders/files and update URLs and field names in `.bru` files

## Validation

After all changes:

```
./build.ps1 -Command Build
./build.ps1 -Command UnitTest
```

Both must pass with zero errors and no test failures before the work is considered done.

## Notes

* The `FeatureConstants.cs` constant _identifiers_ in V3 will be updated (e.g., `OdsInstanceIdsDescription` → `DataStoreIdsDescription`), but the V1 and shared versions are left unchanged.
* `ReadApplicationsByOdsInstance.cs` is in `Features/Applications/` and stays there; only the class name and route string change.
* The `EducationOrganizationMapper.cs`, `EducationOrganizationModels.cs`, `ReadEducationOrganizations.cs`, and `RefreshEducationOrganizations.cs` files in the `OdsInstances` folder have no OdsInstance-specific naming and are moved to `DataStores/` but their class names are unchanged.
