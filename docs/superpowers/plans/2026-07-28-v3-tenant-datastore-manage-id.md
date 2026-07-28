# v3 Tenant DataStoreManageId Parity Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `DataStoreManageId` to v3's `TenantDataStoreModel` so the `/tenants/{tenantName}/dataStores/edOrgs` endpoint reaches parity with v2's `OdsInstanceManageId` on `/tenants/{tenantName}/odsInstances/edOrgs`.

**Architecture:** v3's `TenantService`, `TenantMapper`, and the underlying `IGetDataStoreManagesQuery`/DI wiring already exist and already populate `Status`/`DatabaseTemplate`/`DatabaseName` from the linked `OdsInstanceManage` record — they just never captured its `Id`. This plan mirrors the existing v2 code paths (`Application/EdFi.Ods.AdminApi/Infrastructure/Services/Tenants/TenantService.cs` and `Features/Tenants/TenantMapper.cs`) into their v3 counterparts, one property and two one-line assignments, then extends the existing unit tests and Bruno E2E schemas to assert on it.

**Tech Stack:** C# / .NET, NUnit + Shouldly + FakeItEasy (unit tests), Bruno (`.bru`) E2E collections with `ajv` JSON-schema assertions.

## Global Constraints

- Field name is `DataStoreManageId` (v3's naming counterpart to v2's `OdsInstanceManageId`), per `docs/superpowers/specs/2026-07-28-v3-tenant-datastore-manage-id-design.md`.
- No `[JsonPropertyName]` override needed — the app's default camelCase policy serializes `DataStoreManageId` as `dataStoreManageId`.
- No changes to `ReadTenants.cs`, queries, or DI registration in either version.
- No DBTests — neither v2 nor v3 has DBTests coverage for this endpoint today.
- v3's single-instance `/dataStores/{id}/edOrgs` endpoint (`DataStoreWithEducationOrganizationsModel`) already has `DataStoreManageId` correctly and must not be touched.
- While updating the Bruno E2E schema, also add `status`, `databaseTemplate`, `databaseName` (each `["string", "null"]`) to close a separate pre-existing gap where v3's schema never validated fields `TenantDataStoreModel` already returns and v2's schema already checks.

---

### Task 1: Add `DataStoreManageId` property to `TenantDataStoreModel`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantDetailModel.cs`
- Test: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Tenants/TenantDetailModelTests.cs`

**Interfaces:**
- Produces: `TenantDataStoreModel.DataStoreManageId` (`int?`, settable property) — consumed by Task 2's `TenantMapper` and `TenantService` changes, and by Task 2's `TenantServiceTests` assertions.

- [ ] **Step 1: Write the failing test**

  In `Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Tenants/TenantDetailModelTests.cs`, update the `Properties_ShouldBeSettable` test (this will fail to compile until Task 1 Step 3 adds the property):

  Replace:
  ```csharp
        var odsInstance = new TenantDataStoreModel()
        {
            DataStoreId = 1,
            EducationOrganizations = [educationOrganization]
        };

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            DataStores = [odsInstance]
        };

        // Assert
        tenantDetailModel.TenantName.ShouldBe(tenantName);
        tenantDetailModel.DataStores.ShouldBe([odsInstance]);
        tenantDetailModel.DataStores[0].EducationOrganizations.ShouldBe([educationOrganization]);
    }
  ```

  With:
  ```csharp
        var odsInstance = new TenantDataStoreModel()
        {
            DataStoreId = 1,
            DataStoreManageId = 10,
            EducationOrganizations = [educationOrganization]
        };

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            DataStores = [odsInstance]
        };

        // Assert
        tenantDetailModel.TenantName.ShouldBe(tenantName);
        tenantDetailModel.DataStores.ShouldBe([odsInstance]);
        tenantDetailModel.DataStores[0].DataStoreManageId.ShouldBe(10);
        tenantDetailModel.DataStores[0].EducationOrganizations.ShouldBe([educationOrganization]);
    }
  ```

- [ ] **Step 2: Run test to verify it fails**

  Run: `dotnet test "Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj" --filter "FullyQualifiedName~TenantDetailModelTests" --nologo`

  Expected: Build FAILS with `CS0117: 'TenantDataStoreModel' does not contain a definition for 'DataStoreManageId'`.

- [ ] **Step 3: Add the property**

  In `Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantDetailModel.cs`, in `TenantDataStoreModel`:

  Replace:
  ```csharp
    [JsonPropertyName("id")]
    public int? DataStoreId { get; set; }
    public string Name { get; set; }
  ```

  With:
  ```csharp
    [JsonPropertyName("id")]
    public int? DataStoreId { get; set; }
    public int? DataStoreManageId { get; set; }
    public string Name { get; set; }
  ```

- [ ] **Step 4: Run test to verify it passes**

  Run: `dotnet test "Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj" --filter "FullyQualifiedName~TenantDetailModelTests" --nologo`

  Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

  ```bash
  git add "Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantDetailModel.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/Tenants/TenantDetailModelTests.cs"
  git commit -m "Add DataStoreManageId property to v3 TenantDataStoreModel"
  ```

---

### Task 2: Populate `DataStoreManageId` in `TenantMapper` and `TenantService`

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantMapper.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs`
- Test: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs`

**Interfaces:**
- Consumes: `TenantDataStoreModel.DataStoreManageId` (`int?`, from Task 1).
- Produces: `TenantMapper.ToUnlinkedDataStoreManageModel(OdsInstanceManage)` now sets `DataStoreManageId`; `TenantService.GetTenantEdOrgsByInstancesAsync(...)` now sets `DataStoreManageId` on linked data stores. No signature changes — later tasks consume the same method names/signatures as before.

- [ ] **Step 1: Write the failing tests**

  In `Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs`, apply these six edits (each adds a `DataStoreManageId` assertion mirroring the equivalent `OdsInstanceManageId` assertion already present in `Application/EdFi.Ods.AdminApi.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs`):

  **a) `GetTenantEdOrgsByInstancesAsync_SetsStatusCreated_WhenDataStoreHasNoLinkedDataStoreManage`**

  Replace:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].Status.ShouldBe(OdsInstanceManageStatus.Created.ToString());
        result.DataStores[0].DatabaseTemplate.ShouldBeNull();
        result.DataStores[0].DatabaseName.ShouldBeNull();
    }
  ```

  With:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].Status.ShouldBe(OdsInstanceManageStatus.Created.ToString());
        result.DataStores[0].DataStoreManageId.ShouldBeNull();
        result.DataStores[0].DatabaseTemplate.ShouldBeNull();
        result.DataStores[0].DatabaseName.ShouldBeNull();
    }
  ```

  **b) `GetTenantEdOrgsByInstancesAsync_EnrichesDataStore_WithLinkedDataStoreManageFields`**

  Replace:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        var dataStore = result.DataStores[0];
        dataStore.Status.ShouldBe(OdsInstanceManageStatus.CreateInProgress.ToString());
        dataStore.DatabaseTemplate.ShouldBe("Minimal");
        dataStore.DatabaseName.ShouldBe("EdFi_ODS_2");
    }
  ```

  With:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        var dataStore = result.DataStores[0];
        dataStore.DataStoreManageId.ShouldBe(10);
        dataStore.Status.ShouldBe(OdsInstanceManageStatus.CreateInProgress.ToString());
        dataStore.DatabaseTemplate.ShouldBe("Minimal");
        dataStore.DatabaseName.ShouldBe("EdFi_ODS_2");
    }
  ```

  **c) `GetTenantEdOrgsByInstancesAsync_AddsUnlinkedDataStoreManages_WithNullIds`**

  Replace:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(2);
        result.DataStores.ShouldContain(d => d.DataStoreId == null && d.Name == "Unlinked-A");
        result.DataStores.ShouldContain(d => d.DataStoreId == null && d.Name == "Unlinked-B");
    }
  ```

  With:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(2);
        result.DataStores.ShouldContain(d => d.DataStoreId == null && d.Name == "Unlinked-A" && d.DataStoreManageId == 20);
        result.DataStores.ShouldContain(d => d.DataStoreId == null && d.Name == "Unlinked-B" && d.DataStoreManageId == 21);
    }
  ```

  **d) `GetTenantEdOrgsByInstancesAsync_MixedScenario_LinkedAndUnlinked`**

  Replace:
  ```csharp
        var linkedDataStore = result.DataStores.Single(d => d.DataStoreId == 5);
        linkedDataStore.Status.ShouldBe(OdsInstanceManageStatus.Created.ToString());
        linkedDataStore.DatabaseTemplate.ShouldBe("Minimal");
        linkedDataStore.DatabaseName.ShouldBe("EdFi_ODS_5");

        var unlinkedDataStore = result.DataStores.Single(d => d.Name == "Unlinked-C");
        unlinkedDataStore.Name.ShouldBe("Unlinked-C");
        unlinkedDataStore.Status.ShouldBe(OdsInstanceManageStatus.PendingCreate.ToString());
        unlinkedDataStore.DataStoreId.ShouldBeNull();
    }
  ```

  With:
  ```csharp
        var linkedDataStore = result.DataStores.Single(d => d.DataStoreId == 5);
        linkedDataStore.DataStoreManageId.ShouldBe(30);
        linkedDataStore.Status.ShouldBe(OdsInstanceManageStatus.Created.ToString());
        linkedDataStore.DatabaseTemplate.ShouldBe("Minimal");
        linkedDataStore.DatabaseName.ShouldBe("EdFi_ODS_5");

        var unlinkedDataStore = result.DataStores.Single(d => d.Name == "Unlinked-C");
        unlinkedDataStore.DataStoreManageId.ShouldBe(31);
        unlinkedDataStore.Name.ShouldBe("Unlinked-C");
        unlinkedDataStore.Status.ShouldBe(OdsInstanceManageStatus.PendingCreate.ToString());
        unlinkedDataStore.DataStoreId.ShouldBeNull();
    }
  ```

  **e) `GetTenantEdOrgsByInstancesAsync_AddsDataStoreManage_WhenLinkedToMissingDataStore_ForAllStatuses`**

  Replace:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].DataStoreId.ShouldBeNull();
        result.DataStores[0].Status.ShouldBe(status);
    }
  ```

  With:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].DataStoreId.ShouldBeNull();
        result.DataStores[0].DataStoreManageId.ShouldBe(42);
        result.DataStores[0].Status.ShouldBe(status);
    }
  ```

  **f) `GetTenantEdOrgsByInstancesAsync_AppendsLatestDataStoreManagePerMissingDataStoreId`**

  Replace:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].Status.ShouldBe(OdsInstanceManageStatus.Deleted.ToString());
        result.DataStores[0].Name.ShouldBe("Orphan-Newer");
    }
  ```

  With:
  ```csharp
        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].DataStoreManageId.ShouldBe(51);
        result.DataStores[0].Status.ShouldBe(OdsInstanceManageStatus.Deleted.ToString());
        result.DataStores[0].Name.ShouldBe("Orphan-Newer");
    }
  ```

- [ ] **Step 2: Run tests to verify they fail**

  Run: `dotnet test "Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj" --filter "FullyQualifiedName~TenantServiceTests" --nologo`

  Expected: FAIL — the 6 edited tests fail with Shouldly mismatches (e.g. `dataStore.DataStoreManageId` is `null`, expected `10`).

- [ ] **Step 3: Populate `DataStoreManageId` in `TenantMapper`**

  In `Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantMapper.cs`:

  Replace:
  ```csharp
    public static TenantDataStoreModel ToUnlinkedDataStoreManageModel(OdsInstanceManage source)
    {
        return new TenantDataStoreModel
        {
            DataStoreId = null,
            Name = source.Name,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
        };
    }
  ```

  With:
  ```csharp
    public static TenantDataStoreModel ToUnlinkedDataStoreManageModel(OdsInstanceManage source)
    {
        return new TenantDataStoreModel
        {
            DataStoreId = null,
            DataStoreManageId = source.Id,
            Name = source.Name,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
        };
    }
  ```

- [ ] **Step 4: Populate `DataStoreManageId` in `TenantService`**

  In `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs`, in `GetTenantEdOrgsByInstancesAsync`:

  Replace:
  ```csharp
            foreach (var dataStore in tenantDetails.DataStores)
            {
                if (dataStore.DataStoreId is int dataStoreId && linkedDataStoreManagesByDataStoreId.TryGetValue(dataStoreId, out var dataStoreManage))
                {
                    dataStore.Status = dataStoreManage.Status;
                    dataStore.DatabaseTemplate = dataStoreManage.DatabaseTemplate;
                    dataStore.DatabaseName = dataStoreManage.DatabaseName;
                }
                else
                {
                    dataStore.Status = OdsInstanceManageStatus.Created.ToString();
                }
            }
  ```

  With:
  ```csharp
            foreach (var dataStore in tenantDetails.DataStores)
            {
                if (dataStore.DataStoreId is int dataStoreId && linkedDataStoreManagesByDataStoreId.TryGetValue(dataStoreId, out var dataStoreManage))
                {
                    dataStore.DataStoreManageId = dataStoreManage.Id;
                    dataStore.Status = dataStoreManage.Status;
                    dataStore.DatabaseTemplate = dataStoreManage.DatabaseTemplate;
                    dataStore.DatabaseName = dataStoreManage.DatabaseName;
                }
                else
                {
                    dataStore.Status = OdsInstanceManageStatus.Created.ToString();
                }
            }
  ```

- [ ] **Step 5: Run tests to verify they pass**

  Run: `dotnet test "Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj" --filter "FullyQualifiedName~TenantServiceTests" --nologo`

  Expected: PASS (all tests in the fixture, including the `[TestCaseSource(nameof(AllStatuses))]` parameterized cases).

- [ ] **Step 6: Commit**

  ```bash
  git add "Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantMapper.cs" "Application/EdFi.Ods.AdminApi.V3/Infrastructure/Services/Tenants/TenantService.cs" "Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Services/Tenants/TenantServiceTests.cs"
  git commit -m "Populate DataStoreManageId on v3 tenant data stores"
  ```

---

### Task 3: Update Bruno E2E schema assertions

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru`
- Modify: `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru`

**Interfaces:**
- Consumes: the JSON response shape produced by Task 2 (`dataStoreManageId`, `status`, `databaseTemplate`, `databaseName` now present on each `dataStores[]` entry).
- Produces: nothing consumed by later tasks — this is the last task in the plan.

- [ ] **Step 1: Update the Multitenant schema**

  In `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru`, inside `script:post-response`, in `GetTenantDataStoresEdOrgsSchema`:

  Replace:
  ```javascript
        "dataStores": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "id": {
                "type": ["integer", "null"]
              },
              "name": {
                "type": "string"
              },
              "dataStoreType": {
                "type": ["string", "null"]
              },
              "educationOrganizations": {
  ```

  With:
  ```javascript
        "dataStores": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "id": {
                "type": ["integer", "null"]
              },
              "dataStoreManageId": {
                "type": ["integer", "null"]
              },
              "name": {
                "type": "string"
              },
              "dataStoreType": {
                "type": ["string", "null"]
              },
              "status": {
                "type": ["string", "null"]
              },
              "databaseTemplate": {
                "type": ["string", "null"]
              },
              "databaseName": {
                "type": ["string", "null"]
              },
              "educationOrganizations": {
  ```

- [ ] **Step 2: Update the Singletenant schema**

  In `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru`, apply the identical replacement from Step 1 (same `GetTenantDataStoresEdOrgsSchema` block, same before/after text).

- [ ] **Step 3: Run the v3 multitenant Bruno E2E suite**

  Run: `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode multitenant -TearDown`

  Expected: PASS, including `GET Tenants DataStores EdOrgs: Response matches schema` in both the Multitenant and Singletenant collection runs (Singletenant coverage runs as part of the same suite in single-tenant mode — if your local setup only runs one mode at a time, also run `./eng/run-e2e-bruno.ps1 -ApiVersion 3 -TenantMode singletenant -TearDown`).

  Note: this requires local Docker/DB setup per `docs/developer.md`. If that environment isn't available, at minimum re-read both edited `.bru` files to confirm the JSON is well-formed (matching brace/bracket structure) before committing.

- [ ] **Step 4: Commit**

  ```bash
  git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/GET - Tenants EdOrgs by Tenant Name - Multitenant.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Tenants/GET - Tenants EdOrgs by Tenant Name - Singletenant.bru"
  git commit -m "Add dataStoreManageId and status fields to v3 tenant edOrgs E2E schema"
  ```

---

### Task 4: Full verification

**Files:** none (verification only)

- [ ] **Step 1: Run the full v3 unit test project**

  Run: `dotnet test "Application/EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj" --nologo`

  Expected: PASS, 0 failures.

- [ ] **Step 2: Run the full solution unit test suite**

  Run: `./build.ps1 -Command UnitTest`

  Expected: PASS, 0 failures across all `*.UnitTests` projects (confirms the v2 `TenantServiceTests`/`TenantDetailModelTests` — untouched by this plan — still pass, i.e. no accidental cross-version regression).

- [ ] **Step 3: Confirm no unrelated files changed**

  Run: `git status`

  Expected: working tree clean (all changes already committed across Tasks 1–3); no unexpected modified files outside the ones listed in this plan's tasks.
