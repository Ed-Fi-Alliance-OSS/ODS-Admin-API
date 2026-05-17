# Rename OdsInstance to DataStore (V3) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the V3 `odsInstance*` API resource family (endpoints, JSON fields, C# symbols) to `dataStore*` while leaving the database schema and V1/V2 untouched.

**Architecture:** Each task handles one cohesive feature area end-to-end (infrastructure → feature files → references) so the project compiles and tests pass after every task. **Task 1 (FeatureConstants) MUST be done first** — it adds the new constant names that all subsequent tasks reference. Tasks 2–4 cover the three renamed resource families; Task 5 covers cross-cutting field renames; Tasks 6–7 update tests and E2E files.

**Tech Stack:** .NET 8, ASP.NET Core Minimal APIs, FluentValidation, Swashbuckle, NUnit + Shouldly + FakeItEasy, Bruno (`.bru` files).

---

## File Map

### Task 1 – FeatureConstants V3 (do this FIRST)
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs`

### Task 2 – DataStores (was OdsInstances)
**Infrastructure – rename in place (keep filenames, rename classes/interfaces):**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/AddOdsInstanceCommand.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/EditOdsInstanceCommand.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Commands/DeleteOdsInstanceCommand.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetOdsInstancesQuery.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetOdsInstanceQuery.cs`
- Modify: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Database/Queries/GetApplicationsByOdsInstanceIdQuery.cs`

**Feature folder rename + file content update:**
- Rename folder: `Features/OdsInstances/` → `Features/DataStores/`
- Rename + modify: `AddOdsInstance.cs` → `AddDataStore.cs`
- Rename + modify: `ReadOdsInstance.cs` → `ReadDataStore.cs`
- Rename + modify: `EditOdsInstance.cs` → `EditDataStore.cs`
- Rename + modify: `DeleteOdsInstance.cs` → `DeleteDataStore.cs`
- Rename + modify: `OdsInstanceModel.cs` → `DataStoreModel.cs`
- Rename + modify: `OdsInstanceMapper.cs` → `DataStoreMapper.cs`
- Rename: `OdsInstanceWithEducationOrganizationsModel.cs` → `DataStoreWithEducationOrganizationsModel.cs`
- Modify (references only): `EducationOrganizationMapper.cs`, `EducationOrganizationModels.cs`, `ReadEducationOrganizations.cs`, `RefreshEducationOrganizations.cs`
- Modify: `Features/Applications/ReadApplicationsByOdsInstance.cs` (rename class + update route)

### Task 3 – DataStoreContexts (was OdsInstanceContexts)
**Infrastructure:**
- Modify: `Infrastructure/Database/Commands/AddOdsInstanceContextCommand.cs`
- Modify: `Infrastructure/Database/Commands/EditOdsInstanceContextCommand.cs`
- Modify: `Infrastructure/Database/Commands/DeleteOdsInstanceContextCommand.cs`
- Modify: `Infrastructure/Database/Queries/GetOdsInstanceContextsQuery.cs`
- Modify: `Infrastructure/Database/Queries/GetOdsInstanceContextByIdQuery.cs`

**Feature folder rename + file content update:**
- Rename folder: `Features/OdsInstanceContext/` → `Features/DataStoreContexts/`
- Rename + modify: `AddOdsInstanceContext.cs` → `AddDataStoreContext.cs`
- Rename + modify: `ReadOdsInstanceContext.cs` → `ReadDataStoreContext.cs`
- Rename + modify: `EditOdsInstanceContext.cs` → `EditDataStoreContext.cs`
- Rename + modify: `DeleteOdsInstanceContext.cs` → `DeleteDataStoreContext.cs`
- Rename + modify: `OdsInstanceContextModel.cs` → `DataStoreContextModel.cs`
- Rename + modify: `OdsInstanceContextMapper.cs` → `DataStoreContextMapper.cs`

### Task 4 – DataStoreDerivatives (was OdsInstanceDerivatives)
**Infrastructure:**
- Modify: `Infrastructure/Database/Commands/AddOdsInstanceDerivativeCommand.cs`
- Modify: `Infrastructure/Database/Commands/EditOdsInstanceDerivativeCommand.cs`
- Modify: `Infrastructure/Database/Commands/DeleteOdsInstanceDerivativeCommand.cs`
- Modify: `Infrastructure/Database/Queries/GetOdsInstanceDerivativesQuery.cs`
- Modify: `Infrastructure/Database/Queries/GetOdsInstanceDerivativeByIdQuery.cs`

**Feature folder rename + file content update:**
- Rename folder: `Features/OdsInstanceDerivative/` → `Features/DataStoreDerivatives/`
- Rename + modify: `AddOdsInstanceDerivative.cs` → `AddDataStoreDerivative.cs`
- Rename + modify: `ReadOdsInstanceDerivative.cs` → `ReadDataStoreDerivative.cs`
- Rename + modify: `EditOdsInstanceDerivative.cs` → `EditDataStoreDerivative.cs`
- Rename + modify: `DeleteOdsInstanceDerivative.cs` → `DeleteDataStoreDerivative.cs`
- Rename + modify: `OdsInstanceDerivativeModel.cs` → `DataStoreDerivativeModel.cs`
- Rename + modify: `OdsInstanceDerivativeMapper.cs` → `DataStoreDerivativeMapper.cs`

### Task 5 – Cross-cutting: `dataStoreIds` in Applications and ApiClients
- Modify: `Features/Applications/ApplicationModel.cs`
- Modify: `Features/Applications/AddApplication.cs`
- Modify: `Features/Applications/EditApplication.cs`
- Modify: `Features/Applications/ApplicationMapper.cs`
- Modify: `Features/ApiClients/ApiClientModel.cs`
- Modify: `Features/ApiClients/AddApiClient.cs`
- Modify: `Features/ApiClients/EditApiClient.cs`
- Modify: `Features/ApiClients/ApiClientMapper.cs`
- Modify: `Infrastructure/Database/Queries/GetOdsInstanceIdsByApplicationIdQuery.cs`
- Modify: `Infrastructure/Database/Queries/GetOdsInstanceIdsByApiClientIdQuery.cs`
- Modify: `Infrastructure/Database/Commands/AddApiClientOdsInstanceCommand.cs`
- Modify: `Infrastructure/Database/Queries/GetApiClientOdsInstanceQuery.cs`

### Task 6 – Unit Tests
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/Applications/ApplicationMapperTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/ApiClients/ApiClientModelTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/ApiClients/ReadApiClientTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/ApiClients/AddApiClientOdsInstanceIdsValidationTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/ApiClients/EditApiClientOdsInstanceIdsValidationTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/ApiClients/AddApiClientValidatorTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Features/ApiClients/EditApiClientValidatorTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetOdsInstanceQueryTests.cs`
- Modify: `EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Queries/GetOdsInstancesQueryTests.cs`

### Task 7 – E2E Bruno Tests
- Rename folder: `v3/OdsInstances/` → `v3/DataStores/`
- Rename folder: `v3/OdsInstanceContexts/` → `v3/DataStoreContexts/`
- Rename folder: `v3/OdsInstanceDerivatives/` → `v3/DataStoreDerivatives/`
- Rename folder: `v3/Multitenant Isolation - OdsInstances/` → `v3/Multitenant Isolation - DataStores/`
- Modify all `.bru` files in those folders (URL paths and JSON field names)

---

## Task 1: Update V3 FeatureConstants (do this FIRST)

**File:** `Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs`

- [ ] **Step 1.1: Rename OdsInstance-related constants in V3 FeatureConstants**

  Apply these substitutions in `Features/FeatureConstants.cs` (V3 only — do NOT touch V1 or shared):

  | Old constant identifier | New constant identifier | New value |
  |---|---|---|
  | `OdsInstanceIdsDescription` | `DataStoreIdsDescription` | `"List of data store id"` |
  | `OdsInstanceIdsValidationMessage` | `DataStoreIdsValidationMessage` | `"Please provide at least one data store id."` |
  | `OdsInstanceIdValidationMessage` | `DataStoreIdValidationMessage` | `"Please provide valid data store id. The id {DataStoreId} does not exist."` |
  | `OdsInstanceName` | `DataStoreName` | `"Data store name"` |
  | `OdsInstanceInstanceType` | `DataStoreTypeDescription` | `"Data store type"` |
  | `OdsInstanceConnectionString` | `DataStoreConnectionString` | `"Data store connection string"` |
  | `OdsInstanceAlreadyExistsMessage` | `DataStoreAlreadyExistsMessage` | `"A data store with this name already exists in the database. Please enter a unique name."` |
  | `OdsInstanceCantBeDeletedMessage` | `DataStoreCantBeDeletedMessage` | `"There are some {Table} associated to this DataStore. Can not be deleted."` |
  | `OdsInstanceDerivativeCombinedKeyMustBeUnique` | `DataStoreDerivativeCombinedKeyMustBeUnique` | `"The combined key data store id and derivative type must be unique."` |
  | `OdsInstanceContextCombinedKeyMustBeUnique` | `DataStoreContextCombinedKeyMustBeUnique` | `"The combined key data store id and context key must be unique."` |
  | `OdsInstanceConnectionStringInvalid` | `DataStoreConnectionStringInvalid` | `"The connection string is not valid."` |
  | `OdsInstanceDerivativeIdDescription` | `DataStoreDerivativeIdDescription` | `"Data store derivative id."` |
  | `OdsInstanceDerivativeOdsInstanceIdDescription` | `DataStoreDerivativeDataStoreIdDescription` | `"Data store derivative data store id."` |
  | `OdsInstanceDerivativeDerivativeTypeDescription` | `DataStoreDerivativeTypeDescription` | `"derivative type."` |
  | `OdsInstanceDerivativeConnectionStringDescription` | `DataStoreDerivativeConnectionStringDescription` | `"connection string."` |
  | `OdsInstanceDerivativeDerivativeTypeNotValid` | `DataStoreDerivativeTypeNotValid` | `"The value for the Derivative type is not allowed. The only accepted values are: 'ReadReplica' or 'Snapshot'."` |
  | `OdsInstanceContextIdDescription` | `DataStoreContextIdDescription` | `"Data store context id."` |
  | `OdsInstanceContextOdsInstanceIdDescription` | `DataStoreContextDataStoreIdDescription` | `"Data store context data store id."` |
  | `OdsInstanceContextContextKeyDescription` | (unchanged name) | (unchanged value) |
  | `OdsInstanceContextContextValueDescription` | (unchanged name) | (unchanged value) |

  Also add two new constants needed by the feature files above:
  ```csharp
  public const string DataStoreContextDataStoreIdDescription = "Data store context data store id.";
  public const string DataStoreDerivativeDataStoreIdDescription = "Data store derivative data store id.";
  ```

- [ ] **Step 1.2: Update all references to old constant names in V3 feature files**

  Search for the old constant names and replace them:
  ```powershell
  grep -rn "FeatureConstants\.OdsInstance" Application/EdFi.Ods.AdminApi.V3/Features/
  ```

  For each result, replace with the new constant name (e.g., `FeatureConstants.OdsInstanceName` → `FeatureConstants.DataStoreName`).

- [ ] **Step 1.3: Run build**

  ```powershell
  cd Application; dotnet build EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj
  ```

  Expected: **0 errors**.

- [ ] **Step 1.4: Commit Task 1**

  ```powershell
  git add Application/EdFi.Ods.AdminApi.V3/Features/FeatureConstants.cs
  git commit -m "feat(v3): update FeatureConstants identifiers and values for DataStore rename

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 2: DataStores feature area (Infrastructure + Features/OdsInstances)

**Files:**
- Rename folder: `Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/` → `Features/DataStores/`
- Modify (in place): all files listed in the Task 2 file map above

- [ ] **Step 2.1: Rename the Infrastructure command files for DataStore (OdsInstance)**

  Rename classes/interfaces inside each file. **Do not rename the files themselves yet** — that would cause merge conflicts. Rename classes and interfaces inside them.

  In `Infrastructure/Database/Commands/AddOdsInstanceCommand.cs`, replace all content with:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Admin.DataAccess.Models;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  public interface IAddDataStoreCommand
  {
      OdsInstance Execute(IAddDataStoreModel newDataStore);
  }

  public class AddDataStoreCommand : IAddDataStoreCommand
  {
      private readonly IUsersContext _context;

      public AddDataStoreCommand(IUsersContext context)
      {
          _context = context;
      }

      public OdsInstance Execute(IAddDataStoreModel newDataStore)
      {
          var odsInstance = new OdsInstance
          {
              Name = newDataStore.Name,
              InstanceType = newDataStore.DataStoreType,
              ConnectionString = newDataStore.ConnectionString
          };
          _context.OdsInstances.Add(odsInstance);
          _context.SaveChanges();
          return odsInstance;
      }
  }

  public interface IAddDataStoreModel
  {
      string? Name { get; }
      string? DataStoreType { get; }
      string? ConnectionString { get; }
  }
  ```

  In `Infrastructure/Database/Commands/EditOdsInstanceCommand.cs`, replace all content with:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  public interface IEditDataStoreCommand
  {
      OdsInstance Execute(IEditDataStoreModel changedDataStore);
  }

  public class EditDataStoreCommand : IEditDataStoreCommand
  {
      private readonly IUsersContext _context;

      public EditDataStoreCommand(IUsersContext context)
      {
          _context = context;
      }

      public OdsInstance Execute(IEditDataStoreModel changedDataStore)
      {
          var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == changedDataStore.Id) ??
              throw new NotFoundException<int>("dataStore", changedDataStore.Id);

          odsInstance.Name = changedDataStore.Name;
          odsInstance.InstanceType = changedDataStore.DataStoreType;
          if (!string.IsNullOrEmpty(changedDataStore.ConnectionString))
              odsInstance.ConnectionString = changedDataStore.ConnectionString;

          _context.SaveChanges();
          return odsInstance;
      }
  }

  public interface IEditDataStoreModel
  {
      public int Id { get; set; }
      string? Name { get; }
      string? DataStoreType { get; }
      string? ConnectionString { get; }
  }
  ```

  In `Infrastructure/Database/Commands/DeleteOdsInstanceCommand.cs`, replace all content with:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  public interface IDeleteDataStoreCommand
  {
      void Execute(int id);
  }

  public class DeleteDataStoreCommand : IDeleteDataStoreCommand
  {
      private readonly IUsersContext _context;

      public DeleteDataStoreCommand(IUsersContext context)
      {
          _context = context;
      }

      public void Execute(int id)
      {
          var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == id)
              ?? throw new NotFoundException<int>("dataStore", id);
          _context.OdsInstances.Remove(odsInstance);
          _context.SaveChanges();
      }
  }
  ```

- [ ] **Step 2.2: Rename the Infrastructure query files for DataStore (OdsInstances)**

  In `Infrastructure/Database/Queries/GetOdsInstancesQuery.cs`, rename the interface and class:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using System.Linq.Expressions;
  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
  using EdFi.Ods.AdminApi.Common.Settings;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.Extensions.Options;
  using IUsersContext = EdFi.Admin.DataAccess.Contexts.IUsersContext;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  public interface IGetDataStoresQuery
  {
      List<OdsInstance> Execute();
      List<OdsInstance> Execute(CommonQueryParams commonQueryParams, int? id, string? name, string? dataStoreType);
  }

  public class GetDataStoresQuery : IGetDataStoresQuery
  {
      private readonly IUsersContext _usersContext;
      private readonly IOptions<AppSettings> _options;
      private readonly Dictionary<string, Expression<Func<OdsInstance, object>>> _orderByColumnOds;

      public GetDataStoresQuery(IUsersContext userContext, IOptions<AppSettings> options)
      {
          _usersContext = userContext;
          _options = options;
          var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();
          _orderByColumnOds = new Dictionary<string, Expression<Func<OdsInstance, object>>>
                      (StringComparer.OrdinalIgnoreCase)
                  {
                      { SortingColumns.DefaultNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.Name, DatabaseEngineEnum.SqlServerCollation) : x.Name },
                      { SortingColumns.OdsInstanceInstanceTypeColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.InstanceType, DatabaseEngineEnum.SqlServerCollation) : x.InstanceType },
                      { SortingColumns.DefaultIdColumn, x => x.OdsInstanceId }
                  };
      }

      public List<OdsInstance> Execute()
      {
          return _usersContext.OdsInstances.OrderBy(o => o.Name).ToList();
      }

      public List<OdsInstance> Execute(CommonQueryParams commonQueryParams, int? id, string? name, string? dataStoreType)
      {
          Expression<Func<OdsInstance, object>> columnToOrderBy = _orderByColumnOds.GetColumnToOrderBy(commonQueryParams.OrderBy);

          return _usersContext.OdsInstances
              .Where(o => id == null || o.OdsInstanceId == id)
              .Where(o => name == null || o.Name == name)
              .Where(o => dataStoreType == null || o.InstanceType == dataStoreType)
              .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
              .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
              .ToList();
      }
  }
  ```

  In `Infrastructure/Database/Queries/GetOdsInstanceQuery.cs`, rename:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
  using Microsoft.EntityFrameworkCore;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  public interface IGetDataStoreQuery
  {
      OdsInstance Execute(int id);
  }

  public class GetDataStoreQuery(IUsersContext userContext) : IGetDataStoreQuery
  {
      private readonly IUsersContext _usersContext = userContext;

      public OdsInstance Execute(int id)
      {
          return _usersContext.OdsInstances
              .Include(p => p.OdsInstanceContexts)
              .Include(p => p.OdsInstanceDerivatives)
              .SingleOrDefault(o => o.OdsInstanceId == id)
              ?? throw new NotFoundException<int>("dataStore", id);
      }
  }
  ```

  In `Infrastructure/Database/Queries/GetApplicationsByOdsInstanceIdQuery.cs`, rename interface and class:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
  using Microsoft.EntityFrameworkCore;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  public interface IGetApplicationsByDataStoreIdQuery
  {
      List<Application> Execute(int dataStoreId);
  }

  public class GetApplicationsByDataStoreIdQuery : IGetApplicationsByDataStoreIdQuery
  {
      private readonly IUsersContext _context;

      public GetApplicationsByDataStoreIdQuery(IUsersContext context)
      {
          _context = context;
      }

      public List<Application> Execute(int dataStoreId)
      {
          var applications = _context.ApiClientOdsInstances
              .Where(aco => aco.OdsInstance.OdsInstanceId == dataStoreId)
              .Select(aco => aco.ApiClient.Application)
              .Distinct()
              .Include(app => app.ApplicationEducationOrganizations)
              .Include(app => app.Profiles)
              .Include(app => app.Vendor)
              .Include(app => app.ApiClients)
              .ToList();

          if (!applications.Any() && _context.OdsInstances.Find(dataStoreId) == null)
          {
              throw new NotFoundException<int>("dataStore", dataStoreId);
          }

          return applications;
      }
  }
  ```

- [ ] **Step 2.3: Create new feature folder `DataStores/` and create `DataStoreModel.cs`**

  Create `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DataStoreModel.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
  using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
  using Swashbuckle.AspNetCore.Annotations;
  using System.Text.Json.Serialization;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

  [SwaggerSchema(Title = "DataStore")]
  public class DataStoreModel
  {
      [JsonPropertyName("id")]
      public int DataStoreId { get; set; }
      public string Name { get; set; } = string.Empty;
      public string? DataStoreType { get; set; }
  }

  [SwaggerSchema(Title = "DataStoreDetail")]
  public class DataStoreDetailModel : DataStoreModel
  {
      public IEnumerable<DataStoreContextModel>? DataStoreContexts { get; set; }
      public IEnumerable<DataStoreDerivativeModel>? DataStoreDerivatives { get; set; }
  }
  ```

  > **Note:** `DataStoreContexts` and `DataStoreDerivatives` namespaces won't exist yet — they will be created in Tasks 2 and 3. To keep the build green, you can temporarily define stub classes or complete all three model files before building. The recommended approach is to complete steps 1.3–1.7 of this task, then immediately do Tasks 2 and 3 before running a build.

- [ ] **Step 2.4: Create `DataStoreMapper.cs`**

  Create `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DataStoreMapper.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
  using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

  public static class DataStoreMapper
  {
      public static DataStoreModel ToModel(OdsInstance source)
      {
          return new DataStoreModel
          {
              DataStoreId = source.OdsInstanceId,
              Name = source.Name,
              DataStoreType = source.InstanceType
          };
      }

      public static DataStoreDetailModel ToDetailModel(OdsInstance source)
      {
          return new DataStoreDetailModel
          {
              DataStoreId = source.OdsInstanceId,
              Name = source.Name,
              DataStoreType = source.InstanceType,
              DataStoreContexts = DataStoreContextMapper.ToModelList(source.OdsInstanceContexts),
              DataStoreDerivatives = DataStoreDerivativeMapper.ToModelList(source.OdsInstanceDerivatives)
          };
      }

      public static List<DataStoreModel> ToModelList(IEnumerable<OdsInstance> source)
      {
          return source.Select(ToModel).ToList();
      }
  }
  ```

- [ ] **Step 2.5: Create `AddDataStore.cs`**

  Create `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/AddDataStore.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Constants;
  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
  using EdFi.Ods.AdminApi.Common.Settings;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using FluentValidation;
  using Microsoft.Extensions.Options;
  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

  public class AddDataStore : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder
             .MapPost(endpoints, "/dataStores", Handle)
             .WithDefaultSummaryAndDescription()
             .WithRouteOptions(b => b.WithResponseCode(201))
             .BuildForVersions(AdminApiVersions.V3);
      }

      public static async Task<IResult> Handle(
          Validator validator,
          IAddDataStoreCommand addDataStoreCommand,
          ISymmetricStringEncryptionProvider encryptionProvider,
          IOptions<AppSettings> options,
          AddDataStoreRequest request,
          HttpContext httpContext)
      {
          await validator.GuardAsync(request);
          string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
          request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
          var added = addDataStoreCommand.Execute(request);
          var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStores/{added.OdsInstanceId}");
          return Results.Created(absoluteLocation, null);
      }

      [SwaggerSchema(Title = "AddDataStoreRequest")]
      public class AddDataStoreRequest : IAddDataStoreModel
      {
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceName, Nullable = false)]
          public string? Name { get; set; }
          [SwaggerSchema(Description = FeatureConstants.DataStoreTypeDescription, Nullable = true)]
          public string? DataStoreType { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceConnectionString, Nullable = false)]
          public string? ConnectionString { get; set; }
      }

      public class Validator : AbstractValidator<IAddDataStoreModel>
      {
          private readonly IGetDataStoresQuery _getDataStoresQuery;
          private readonly string _databaseEngine;

          public Validator(IGetDataStoresQuery getDataStoresQuery, IOptions<AppSettings> options)
          {
              _getDataStoresQuery = getDataStoresQuery;
              _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

              RuleFor(m => m.Name)
                  .NotEmpty()
                  .Must(BeAUniqueName)
                  .WithMessage(FeatureConstants.OdsInstanceAlreadyExistsMessage);

              RuleFor(m => m.DataStoreType)
                  .MaximumLength(100)
                  .When(m => !string.IsNullOrEmpty(m.DataStoreType));

              RuleFor(m => m.ConnectionString)
                  .NotEmpty();

              RuleFor(m => m.ConnectionString)
                  .Must(BeAValidConnectionString)
                  .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                  .When(m => !string.IsNullOrEmpty(m.ConnectionString));
          }

          private bool BeAUniqueName(string? name)
          {
              return _getDataStoresQuery.Execute().TrueForAll(x => x.Name != name);
          }

          private bool BeAValidConnectionString(string? connectionString)
          {
              return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
          }
      }
  }
  ```

- [ ] **Step 2.6: Create `ReadDataStore.cs`**

  Create `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/ReadDataStore.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

  public class ReadDataStore : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores", GetDataStores)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponse<DataStoreModel[]>(200))
              .BuildForVersions(AdminApiVersions.V3);

          AdminApiEndpointBuilder.MapGet(endpoints, "/dataStores/{id}", GetDataStore)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponse<DataStoreDetailModel>(200))
              .BuildForVersions(AdminApiVersions.V3);
      }

      internal static Task<IResult> GetDataStores(IGetDataStoresQuery getDataStoresQuery, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? name, string? dataStoreType)
      {
          var dataStores = DataStoreMapper.ToModelList(getDataStoresQuery.Execute(commonQueryParams, id, name, dataStoreType));
          return Task.FromResult(Results.Ok(dataStores));
      }

      internal static Task<IResult> GetDataStore(IGetDataStoreQuery getDataStoreQuery, int id)
      {
          var dataStore = getDataStoreQuery.Execute(id);
          var model = DataStoreMapper.ToDetailModel(dataStore);
          return Task.FromResult(Results.Ok(model));
      }
  }
  ```

- [ ] **Step 2.7: Create `EditDataStore.cs`**

  Create `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/EditDataStore.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
  using EdFi.Ods.AdminApi.Common.Settings;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;
  using FluentValidation;
  using Microsoft.Extensions.Options;
  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

  public class EditDataStore : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder
              .MapPut(endpoints, "/dataStores/{id}", Handle)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponseCode(204))
              .BuildForVersions(AdminApiVersions.V3);
      }

      public static async Task<IResult> Handle(
          Validator validator,
          IEditDataStoreCommand editDataStoreCommand,
          ISymmetricStringEncryptionProvider encryptionProvider,
          IOptions<AppSettings> options,
          EditDataStoreRequest request,
          int id)
      {
          request.Id = id;
          await validator.GuardAsync(request);

          string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
          if (!string.IsNullOrEmpty(request.ConnectionString))
              request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
          else
              request.ConnectionString = string.Empty;
          editDataStoreCommand.Execute(request);
          return Results.NoContent();
      }

      [SwaggerSchema(Title = "EditDataStoreRequest")]
      public class EditDataStoreRequest : IEditDataStoreModel
      {
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceName, Nullable = false)]
          public string? Name { get; set; }
          [SwaggerSchema(Description = FeatureConstants.DataStoreTypeDescription, Nullable = true)]
          public string? DataStoreType { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceConnectionString, Nullable = true)]
          public string? ConnectionString { get; set; }
          [SwaggerExclude]
          public int Id { get; set; }
      }

      public class Validator : AbstractValidator<IEditDataStoreModel>
      {
          private readonly IGetDataStoresQuery _getDataStoresQuery;
          private readonly IGetDataStoreQuery _getDataStoreQuery;
          private readonly string _databaseEngine;

          public Validator(IGetDataStoresQuery getDataStoresQuery, IGetDataStoreQuery getDataStoreQuery, IOptions<AppSettings> options)
          {
              _getDataStoresQuery = getDataStoresQuery;
              _getDataStoreQuery = getDataStoreQuery;
              _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

              RuleFor(m => m.Name)
                  .NotEmpty()
                  .Must(BeAUniqueName)
                  .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage)
                  .When(m => BeAnExistingDataStore(m.Id) && NameIsChanged(m));

              RuleFor(m => m.DataStoreType)
                  .MaximumLength(100)
                  .When(m => !string.IsNullOrEmpty(m.DataStoreType));

              RuleFor(m => m.ConnectionString)
                  .Must(BeAValidConnectionString)
                  .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                  .When(m => !string.IsNullOrEmpty(m.ConnectionString));
          }

          private bool BeAnExistingDataStore(int id)
          {
              _getDataStoreQuery.Execute(id);
              return true;
          }

          private bool NameIsChanged(IEditDataStoreModel model)
          {
              return _getDataStoreQuery.Execute(model.Id).Name != model.Name;
          }

          private bool BeAUniqueName(string? name)
          {
              return _getDataStoresQuery.Execute().TrueForAll(x => x.Name != name);
          }

          private bool BeAValidConnectionString(string? connectionString)
          {
              return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
          }
      }
  }
  ```

- [ ] **Step 2.8: Create `DeleteDataStore.cs`**

  Create `Application/EdFi.Ods.AdminApi.V3/Features/DataStores/DeleteDataStore.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using FluentValidation;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

  public class DeleteDataStore : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder.MapDelete(endpoints, "/dataStores/{id}", Handle)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponseCode(204))
              .BuildForVersions(AdminApiVersions.V3);
      }

      internal async Task<IResult> Handle(IDeleteDataStoreCommand deleteDataStoreCommand, Validator validator, int id)
      {
          var request = new Request { Id = id };
          await validator.GuardAsync(request);
          deleteDataStoreCommand.Execute(request.Id);
          return await Task.FromResult(Results.NoContent());
      }

      public class Validator : AbstractValidator<Request>
      {
          private readonly IGetDataStoreQuery _getDataStoreQuery;
          private readonly IGetApplicationsByDataStoreIdQuery _getApplicationsByDataStoreIdQuery;
          private OdsInstance? _dataStoreEntity = null;

          public Validator(IGetDataStoreQuery getDataStoreQuery, IGetApplicationsByDataStoreIdQuery getApplicationsByDataStoreIdQuery)
          {
              _getDataStoreQuery = getDataStoreQuery;
              _getApplicationsByDataStoreIdQuery = getApplicationsByDataStoreIdQuery;

              RuleFor(m => m.Id)
                  .Must(NotHaveApplicationsRelationships)
                  .WithMessage(FeatureConstants.OdsInstanceCantBeDeletedMessage)
                  .When(Exist);
              RuleFor(m => m.Id)
                  .Must(NotHaveDataStoreContextsRelationships)
                  .WithMessage(FeatureConstants.OdsInstanceCantBeDeletedMessage)
                  .When(Exist);
              RuleFor(m => m.Id)
                  .Must(NotHaveDataStoreDerivativesRelationships)
                  .WithMessage(FeatureConstants.OdsInstanceCantBeDeletedMessage)
                  .When(Exist);
          }

          private bool Exist(Request request)
          {
              _dataStoreEntity = _getDataStoreQuery.Execute(request.Id);
              return true;
          }

          private bool NotHaveApplicationsRelationships<T>(Request model, int id, ValidationContext<T> context)
          {
              context.MessageFormatter.AppendArgument("Table", "Applications");
              List<Application> appList = _getApplicationsByDataStoreIdQuery.Execute(id) ?? [];
              return appList.Count == 0;
          }

          private bool NotHaveDataStoreContextsRelationships<T>(Request model, int id, ValidationContext<T> context)
          {
              context.MessageFormatter.AppendArgument("Table", "DataStoreContexts");
              return _dataStoreEntity!.OdsInstanceContexts.Count == 0;
          }

          private bool NotHaveDataStoreDerivativesRelationships<T>(Request model, int id, ValidationContext<T> context)
          {
              context.MessageFormatter.AppendArgument("Table", "DataStoreDerivatives");
              return _dataStoreEntity!.OdsInstanceDerivatives.Count == 0;
          }
      }

      public class Request
      {
          public int Id { get; set; }
      }
  }
  ```

- [ ] **Step 2.9: Copy unchanged education-organization files into `DataStores/` folder and update namespace**

  The following files in `Features/OdsInstances/` have no OdsInstance-specific naming. Copy them to `Features/DataStores/` and update the `namespace` declaration from `EdFi.Ods.AdminApi.V3.Features.OdsInstances` to `EdFi.Ods.AdminApi.V3.Features.DataStores`:
  - `EducationOrganizationMapper.cs`
  - `EducationOrganizationModels.cs`
  - `ReadEducationOrganizations.cs`
  - `RefreshEducationOrganizations.cs`

  Also update any route strings in `ReadEducationOrganizations.cs` and `RefreshEducationOrganizations.cs` if they reference `odsInstances` (change to `dataStores`):
  ```
  // In ReadEducationOrganizations.cs — find and update these route strings:
  "odsInstances/{id}/educationOrganizations"  →  "dataStores/{id}/educationOrganizations"

  // In RefreshEducationOrganizations.cs:
  "odsInstances/{id}/educationOrganizations/refresh"  →  "dataStores/{id}/educationOrganizations/refresh"
  ```

- [ ] **Step 2.10: Update `Features/Applications/ReadApplicationsByOdsInstance.cs`**

  Rename the class, update the route string, update the namespace usages, and update error message references. The file stays in `Features/Applications/` — only the class name and URL change:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  namespace EdFi.Ods.AdminApi.V3.Features.Applications;

  public class ReadApplicationsByDataStore : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          var url = "dataStores/{id}/applications";

          AdminApiEndpointBuilder.MapGet(endpoints, url, GetDataStoreApplications)
              .WithSummary("Retrieves applications assigned to a specific data store based on the resource identifier.")
              .WithRouteOptions(b => b.WithResponse<ApplicationModel[]>(200))
              .BuildForVersions(AdminApiVersions.V3);
      }

      internal static Task<IResult> GetDataStoreApplications(
          IGetApplicationsByDataStoreIdQuery getApplicationsByDataStoreIdQuery,
          IGetDataStoreIdsByApplicationIdQuery getDataStoreIdsByApplicationIdQuery,
          int id)
      {
          var applicationEntities = getApplicationsByDataStoreIdQuery.Execute(id);
          var dataStoreIdsByApplicationId = getDataStoreIdsByApplicationIdQuery.Execute(applicationEntities.Select(a => a.ApplicationId));
          var applications = ApplicationMapper.ToModelList(applicationEntities, dataStoreIdsByApplicationId);
          return Task.FromResult(Results.Ok(applications));
      }
  }
  ```

  Also rename the file: `ReadApplicationsByOdsInstance.cs` → `ReadApplicationsByDataStore.cs`

- [ ] **Step 2.11: Delete the old `OdsInstances/` feature folder**

  Now that all files have been recreated in `DataStores/`, delete the old folder and its files:
  ```powershell
  Remove-Item -Recurse -Force "Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances"
  ```

  > Git will track this as a rename (delete old + create new). Verify with `git status`.

- [ ] **Step 2.12: Verify build compiles (expected: errors from Tasks 3 and 4 namespace references in DataStoreMapper/DataStoreModel)**

  ```powershell
  cd Application; dotnet build EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj 2>&1 | Select-String "error|Error" | head -30
  ```

  Expected errors at this point: only missing `DataStoreContexts` and `DataStoreDerivatives` namespace references in `DataStoreMapper.cs` and `DataStoreModel.cs`. All other errors must be fixed before moving on.

---

## Task 3: DataStoreContexts feature area (Infrastructure + Features/OdsInstanceContext)

**Files:**
- Modify: all infrastructure command/query files listed in the Task 2 file map
- Rename folder: `Features/OdsInstanceContext/` → `Features/DataStoreContexts/`
- Create new files in `Features/DataStoreContexts/`

- [ ] **Step 3.1: Update Infrastructure commands for DataStoreContext**

  In `Infrastructure/Database/Commands/AddOdsInstanceContextCommand.cs`, replace all content:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  public interface IAddDataStoreContextCommand
  {
      OdsInstanceContext Execute(IAddDataStoreContextModel newDataStoreContext);
  }

  public class AddDataStoreContextCommand : IAddDataStoreContextCommand
  {
      private readonly IUsersContext _context;

      public AddDataStoreContextCommand(IUsersContext context)
      {
          _context = context;
      }

      public OdsInstanceContext Execute(IAddDataStoreContextModel newDataStoreContext)
      {
          var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == newDataStoreContext.DataStoreId) ??
              throw new NotFoundException<int>("dataStore", newDataStoreContext.DataStoreId);

          var context = new OdsInstanceContext
          {
              ContextKey = newDataStoreContext.ContextKey,
              ContextValue = newDataStoreContext.ContextValue,
              OdsInstance = odsInstance
          };
          _context.OdsInstanceContexts.Add(context);
          _context.SaveChanges();
          return context;
      }
  }

  public interface IAddDataStoreContextModel
  {
      public int DataStoreId { get; set; }
      public string? ContextKey { get; set; }
      public string? ContextValue { get; set; }
  }
  ```

  In `Infrastructure/Database/Commands/EditOdsInstanceContextCommand.cs`, rename classes/interfaces to `IEditDataStoreContextCommand`, `EditDataStoreContextCommand`, `IEditDataStoreContextModel` following the same pattern (replacing `OdsInstanceId` with `DataStoreId` in the model interface). The EF Core operations still use `_context.OdsInstanceContexts` and `OdsInstanceContextId` — do not change those.

  In `Infrastructure/Database/Commands/DeleteOdsInstanceContextCommand.cs`, rename to `IDeleteDataStoreContextCommand` and `DeleteDataStoreContextCommand`.

- [ ] **Step 3.2: Update Infrastructure queries for DataStoreContext**

  In `Infrastructure/Database/Queries/GetOdsInstanceContextsQuery.cs`, rename `IGetOdsInstanceContextsQuery` → `IGetDataStoreContextsQuery`, `GetOdsInstanceContextsQuery` → `GetDataStoreContextsQuery`.

  In `Infrastructure/Database/Queries/GetOdsInstanceContextByIdQuery.cs`, rename `IGetOdsInstanceContextByIdQuery` → `IGetDataStoreContextByIdQuery`, `GetOdsInstanceContextByIdQuery` → `GetDataStoreContextByIdQuery`. Update the `NotFoundException` message to say `"dataStoreContext"`.

- [ ] **Step 3.3: Create `Features/DataStoreContexts/DataStoreContextModel.cs`**

  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using Swashbuckle.AspNetCore.Annotations;
  using System.Text.Json.Serialization;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

  [SwaggerSchema(Title = "DataStoreContext")]
  public class DataStoreContextModel
  {
      [JsonPropertyName("id")]
      public int DataStoreContextId { get; set; }
      public int DataStoreId { get; set; }
      public string? ContextKey { get; set; }
      public string? ContextValue { get; set; }
  }
  ```

- [ ] **Step 3.4: Create `Features/DataStoreContexts/DataStoreContextMapper.cs`**

  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using DbOdsInstanceContext = EdFi.Admin.DataAccess.Models.OdsInstanceContext;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

  public static class DataStoreContextMapper
  {
      public static DataStoreContextModel ToModel(DbOdsInstanceContext source)
      {
          return new DataStoreContextModel
          {
              DataStoreContextId = source.OdsInstanceContextId,
              DataStoreId = source.OdsInstance?.OdsInstanceId ?? 0,
              ContextKey = source.ContextKey,
              ContextValue = source.ContextValue
          };
      }

      public static List<DataStoreContextModel> ToModelList(IEnumerable<DbOdsInstanceContext> source)
      {
          return source.Select(ToModel).ToList();
      }
  }
  ```

- [ ] **Step 3.5: Create `Features/DataStoreContexts/AddDataStoreContext.cs`**

  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Constants;
  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using FluentValidation;
  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

  public class AddDataStoreContext : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder
             .MapPost(endpoints, "/dataStoreContexts", Handle)
             .WithDefaultSummaryAndDescription()
             .WithRouteOptions(b => b.WithResponseCode(201))
             .BuildForVersions(AdminApiVersions.V3);
      }

      public static async Task<IResult> Handle(Validator validator, IAddDataStoreContextCommand addDataStoreContextCommand, AddDataStoreContextRequest request, HttpContext httpContext)
      {
          await validator.GuardAsync(request);
          var added = addDataStoreContextCommand.Execute(request);
          var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStoreContexts/{added.OdsInstanceContextId}");
          return Results.Created(absoluteLocation, null);
      }

      [SwaggerSchema(Title = "AddDataStoreContextRequest")]
      public class AddDataStoreContextRequest : IAddDataStoreContextModel
      {
          [SwaggerSchema(Description = FeatureConstants.DataStoreContextDataStoreIdDescription, Nullable = false)]
          public int DataStoreId { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextKeyDescription, Nullable = false)]
          public string? ContextKey { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextValueDescription, Nullable = false)]
          public string? ContextValue { get; set; }
      }

      public class Validator : AbstractValidator<AddDataStoreContextRequest>
      {
          private readonly IGetDataStoreQuery _getDataStoreQuery;
          private readonly IGetDataStoreContextsQuery _getDataStoreContextsQuery;

          public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreContextsQuery getDataStoreContextsQuery)
          {
              _getDataStoreQuery = getDataStoreQuery;
              _getDataStoreContextsQuery = getDataStoreContextsQuery;

              RuleFor(m => m.ContextKey).NotEmpty();
              RuleFor(m => m.ContextValue).NotEmpty();

              RuleFor(m => m.DataStoreId)
                  .NotEqual(0)
                  .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

              RuleFor(m => m.DataStoreId)
                  .Must(BeAnExistingDataStore)
                  .When(m => !m.DataStoreId.Equals(0));

              RuleFor(ctx => ctx)
                  .Must(BeUniqueCombinedKey)
                  .WithMessage(FeatureConstants.OdsInstanceContextCombinedKeyMustBeUnique);
          }

          private bool BeAnExistingDataStore(int id)
          {
              _getDataStoreQuery.Execute(id);
              return true;
          }

          private bool BeUniqueCombinedKey(AddDataStoreContextRequest request)
          {
              return !_getDataStoreContextsQuery.Execute().Exists(
                  x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                  x.ContextKey.Equals(request.ContextKey, StringComparison.OrdinalIgnoreCase));
          }
      }
  }
  ```

- [ ] **Step 3.6: Create `ReadDataStoreContext.cs`, `EditDataStoreContext.cs`, `DeleteDataStoreContext.cs`**

  `ReadDataStoreContext.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

  public class ReadDataStoreContext : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreContexts", GetDataStoreContexts)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponse<DataStoreContextModel[]>(200))
              .BuildForVersions(AdminApiVersions.V3);

          AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreContexts/{id}", GetDataStoreContext)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponse<DataStoreContextModel>(200))
              .BuildForVersions(AdminApiVersions.V3);
      }

      internal static Task<IResult> GetDataStoreContexts(IGetDataStoreContextsQuery getDataStoreContextsQuery, [AsParameters] CommonQueryParams commonQueryParams)
      {
          var list = DataStoreContextMapper.ToModelList(getDataStoreContextsQuery.Execute(commonQueryParams));
          return Task.FromResult(Results.Ok(list));
      }

      internal static Task<IResult> GetDataStoreContext(IGetDataStoreContextByIdQuery getDataStoreContextByIdQuery, int id)
      {
          var item = getDataStoreContextByIdQuery.Execute(id);
          var model = DataStoreContextMapper.ToModel(item);
          return Task.FromResult(Results.Ok(model));
      }
  }
  ```

  `EditDataStoreContext.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;
  using FluentValidation;
  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

  public class EditDataStoreContext : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder
              .MapPut(endpoints, "/dataStoreContexts/{id}", Handle)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponseCode(204))
              .BuildForVersions(AdminApiVersions.V3);
      }

      public static async Task<IResult> Handle(Validator validator, IEditDataStoreContextCommand editDataStoreContextCommand, EditDataStoreContextRequest request, int id)
      {
          request.Id = id;
          await validator.GuardAsync(request);
          editDataStoreContextCommand.Execute(request);
          return Results.NoContent();
      }

      [SwaggerSchema(Title = "EditDataStoreContextRequest")]
      public class EditDataStoreContextRequest : IEditDataStoreContextModel
      {
          [SwaggerExclude]
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextIdDescription, Nullable = false)]
          public int Id { get; set; }
          [SwaggerSchema(Description = FeatureConstants.DataStoreContextDataStoreIdDescription, Nullable = false)]
          public int DataStoreId { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextKeyDescription, Nullable = false)]
          public string? ContextKey { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextValueDescription, Nullable = false)]
          public string? ContextValue { get; set; }
      }

      public class Validator : AbstractValidator<EditDataStoreContextRequest>
      {
          private readonly IGetDataStoreQuery _getDataStoreQuery;
          private readonly IGetDataStoreContextsQuery _getDataStoreContextsQuery;

          public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreContextsQuery getDataStoreContextsQuery)
          {
              _getDataStoreQuery = getDataStoreQuery;
              _getDataStoreContextsQuery = getDataStoreContextsQuery;

              RuleFor(m => m.ContextKey).NotEmpty();
              RuleFor(m => m.ContextValue).NotEmpty();

              RuleFor(m => m.DataStoreId)
                  .NotEqual(0)
                  .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

              RuleFor(m => m.DataStoreId)
                  .Must(BeAnExistingDataStore)
                  .When(m => !m.DataStoreId.Equals(0));

              RuleFor(ctx => ctx)
                  .Must(BeUniqueCombinedKey)
                  .WithMessage(FeatureConstants.OdsInstanceContextCombinedKeyMustBeUnique);
          }

          private bool BeAnExistingDataStore(int id)
          {
              _getDataStoreQuery.Execute(id);
              return true;
          }

          private bool BeUniqueCombinedKey(EditDataStoreContextRequest request)
          {
              return !_getDataStoreContextsQuery.Execute().Exists(
                  x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                  x.ContextKey.Equals(request.ContextKey, StringComparison.OrdinalIgnoreCase) &&
                  x.OdsInstanceContextId != request.Id);
          }
      }
  }
  ```

  `DeleteDataStoreContext.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

  public class DeleteDataStoreContext : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder.MapDelete(endpoints, "/dataStoreContexts/{id}", Handle)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponseCode(204))
              .BuildForVersions(AdminApiVersions.V3);
      }

      public static Task<IResult> Handle(IDeleteDataStoreContextCommand deleteDataStoreContextCommand, int id)
      {
          deleteDataStoreContextCommand.Execute(id);
          return Task.FromResult(Results.NoContent());
      }
  }
  ```

- [ ] **Step 3.7: Delete old `Features/OdsInstanceContext/` folder**

  ```powershell
  Remove-Item -Recurse -Force "Application/EdFi.Ods.AdminApi.V3/Features/OdsInstanceContext"
  ```

---

## Task 4: DataStoreDerivatives feature area (Infrastructure + Features/OdsInstanceDerivative)

Follow the exact same pattern as Task 2.

- [ ] **Step 4.1: Update Infrastructure commands for DataStoreDerivative**

  In `Infrastructure/Database/Commands/AddOdsInstanceDerivativeCommand.cs`, replace all content:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Admin.DataAccess.Models;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

  namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  public interface IAddDataStoreDerivativeCommand
  {
      OdsInstanceDerivative Execute(IAddDataStoreDerivativeModel newDataStoreDerivative);
  }

  public class AddDataStoreDerivativeCommand : IAddDataStoreDerivativeCommand
  {
      private readonly IUsersContext _context;

      public AddDataStoreDerivativeCommand(IUsersContext context)
      {
          _context = context;
      }

      public OdsInstanceDerivative Execute(IAddDataStoreDerivativeModel newDataStoreDerivative)
      {
          var odsInstance = _context.OdsInstances.SingleOrDefault(v => v.OdsInstanceId == newDataStoreDerivative.DataStoreId) ??
              throw new NotFoundException<int>("dataStore", newDataStoreDerivative.DataStoreId);

          var derivative = new OdsInstanceDerivative
          {
              ConnectionString = newDataStoreDerivative.ConnectionString,
              DerivativeType = newDataStoreDerivative.DerivativeType,
              OdsInstance = odsInstance
          };
          _context.OdsInstanceDerivatives.Add(derivative);
          _context.SaveChanges();
          return derivative;
      }
  }

  public interface IAddDataStoreDerivativeModel
  {
      public int DataStoreId { get; set; }
      public string? DerivativeType { get; set; }
      public string? ConnectionString { get; set; }
  }
  ```

  In `Infrastructure/Database/Commands/EditOdsInstanceDerivativeCommand.cs`, rename to `IEditDataStoreDerivativeCommand`, `EditDataStoreDerivativeCommand`, `IEditDataStoreDerivativeModel`. Replace `OdsInstanceId` with `DataStoreId` in the model interface.

  In `Infrastructure/Database/Commands/DeleteOdsInstanceDerivativeCommand.cs`, rename to `IDeleteDataStoreDerivativeCommand` and `DeleteDataStoreDerivativeCommand`.

- [ ] **Step 4.2: Update Infrastructure queries for DataStoreDerivative**

  In `Infrastructure/Database/Queries/GetOdsInstanceDerivativesQuery.cs`, rename `IGetOdsInstanceDerivativesQuery` → `IGetDataStoreDerivativesQuery`, `GetOdsInstanceDerivativesQuery` → `GetDataStoreDerivativesQuery`.

  In `Infrastructure/Database/Queries/GetOdsInstanceDerivativeByIdQuery.cs`, rename `IGetOdsInstanceDerivativeByIdQuery` → `IGetDataStoreDerivativeByIdQuery`, `GetOdsInstanceDerivativeByIdQuery` → `GetDataStoreDerivativeByIdQuery`. Update `NotFoundException` message to `"dataStoreDerivative"`.

- [ ] **Step 4.3: Create `Features/DataStoreDerivatives/DataStoreDerivativeModel.cs`**

  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  [SwaggerSchema(Title = "DataStoreDerivative")]
  public class DataStoreDerivativeModel
  {
      public int Id { get; set; }
      public int? DataStoreId { get; set; }
      public string? DerivativeType { get; set; }
  }
  ```

- [ ] **Step 4.4: Create `Features/DataStoreDerivatives/DataStoreDerivativeMapper.cs`**

  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  public static class DataStoreDerivativeMapper
  {
      public static DataStoreDerivativeModel ToModel(DbOdsInstanceDerivative source)
      {
          return new DataStoreDerivativeModel
          {
              Id = source.OdsInstanceDerivativeId,
              DataStoreId = source.OdsInstance?.OdsInstanceId,
              DerivativeType = source.DerivativeType
          };
      }

      public static List<DataStoreDerivativeModel> ToModelList(IEnumerable<DbOdsInstanceDerivative> source)
      {
          return source.Select(ToModel).ToList();
      }
  }
  ```

- [ ] **Step 4.5: Create `AddDataStoreDerivative.cs`, `ReadDataStoreDerivative.cs`, `EditDataStoreDerivative.cs`, `DeleteDataStoreDerivative.cs`**

  Follow the same pattern as Tasks 1 and 2. Key changes from original:
  - Routes: `/odsInstanceDerivatives` → `/dataStoreDerivatives`
  - Request class `AddDataStoreDerivativeRequest` implements `IAddDataStoreDerivativeModel`, has `DataStoreId` instead of `OdsInstanceId`
  - Request class `EditDataStoreDerivativeRequest` implements `IEditDataStoreDerivativeModel`, has `DataStoreId` instead of `OdsInstanceId`
  - All injected query/command references use the new `IGetDataStore*` / `IAddDataStore*` names
  - `SwaggerSchema(Title = "AddDataStoreDerivativeRequest")` / `"EditDataStoreDerivativeRequest"`
  - Validation error messages still reference `FeatureConstants.OdsInstanceIdValidationMessage` → replace with `FeatureConstants.DataStoreIdValidationMessage`

  `AddDataStoreDerivative.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Constants;
  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
  using EdFi.Ods.AdminApi.Common.Settings;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using FluentValidation;
  using Microsoft.Extensions.Options;
  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  public class AddDataStoreDerivative : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder
             .MapPost(endpoints, "/dataStoreDerivatives", Handle)
             .WithDefaultSummaryAndDescription()
             .WithRouteOptions(b => b.WithResponseCode(201))
             .BuildForVersions(AdminApiVersions.V3);
      }

      public static async Task<IResult> Handle(Validator validator, IAddDataStoreDerivativeCommand addDataStoreDerivativeCommand, AddDataStoreDerivativeRequest request, HttpContext httpContext)
      {
          await validator.GuardAsync(request);
          var added = addDataStoreDerivativeCommand.Execute(request);
          var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStoreDerivatives/{added.OdsInstanceDerivativeId}");
          return Results.Created(absoluteLocation, null);
      }

      [SwaggerSchema(Title = "AddDataStoreDerivativeRequest")]
      public class AddDataStoreDerivativeRequest : IAddDataStoreDerivativeModel
      {
          [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeDataStoreIdDescription, Nullable = false)]
          public int DataStoreId { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeDerivativeTypeDescription, Nullable = false)]
          public string? DerivativeType { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeConnectionStringDescription, Nullable = false)]
          public string? ConnectionString { get; set; }
      }

      public class Validator : AbstractValidator<AddDataStoreDerivativeRequest>
      {
          private readonly IGetDataStoreQuery _getDataStoreQuery;
          private readonly IGetDataStoreDerivativesQuery _getDataStoreDerivativesQuery;
          private readonly string _databaseEngine;

          public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreDerivativesQuery getDataStoreDerivativesQuery, IOptions<AppSettings> options)
          {
              _getDataStoreQuery = getDataStoreQuery;
              _getDataStoreDerivativesQuery = getDataStoreDerivativesQuery;
              _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

              RuleFor(m => m.DerivativeType).NotEmpty();

              RuleFor(m => m.DerivativeType)
                  .Matches("^(?i)(readreplica|snapshot)$")
                  .WithMessage(FeatureConstants.OdsInstanceDerivativeDerivativeTypeNotValid)
                  .When(m => !string.IsNullOrEmpty(m.DerivativeType));

              RuleFor(m => m.DataStoreId)
                  .NotEqual(0)
                  .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

              RuleFor(m => m.DataStoreId)
                  .Must(BeAnExistingDataStore)
                  .When(m => !m.DataStoreId.Equals(0));

              RuleFor(m => m.ConnectionString).NotEmpty();

              RuleFor(m => m.ConnectionString)
                  .Must(BeAValidConnectionString)
                  .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                  .When(m => !string.IsNullOrEmpty(m.ConnectionString));

              RuleFor(d => d)
                  .Must(BeUniqueCombinedKey)
                  .WithMessage(FeatureConstants.OdsInstanceDerivativeCombinedKeyMustBeUnique);
          }

          private bool BeAnExistingDataStore(int id)
          {
              _getDataStoreQuery.Execute(id);
              return true;
          }

          private bool BeAValidConnectionString(string? connectionString)
          {
              return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
          }

          private bool BeUniqueCombinedKey(AddDataStoreDerivativeRequest request)
          {
              return !_getDataStoreDerivativesQuery.Execute().Exists(
                  x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                  x.DerivativeType.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase));
          }
      }
  }
  ```

  `ReadDataStoreDerivative.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  public class ReadDataStoreDerivative : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreDerivatives", GetDataStoreDerivatives)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponse<DataStoreDerivativeModel[]>(200))
              .BuildForVersions(AdminApiVersions.V3);

          AdminApiEndpointBuilder.MapGet(endpoints, "/dataStoreDerivatives/{id}", GetDataStoreDerivative)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponse<DataStoreDerivativeModel>(200))
              .BuildForVersions(AdminApiVersions.V3);
      }

      internal static Task<IResult> GetDataStoreDerivatives(IGetDataStoreDerivativesQuery getDataStoreDerivativesQuery, [AsParameters] CommonQueryParams commonQueryParams)
      {
          var list = DataStoreDerivativeMapper.ToModelList(getDataStoreDerivativesQuery.Execute(commonQueryParams));
          return Task.FromResult(Results.Ok(list));
      }

      internal static Task<IResult> GetDataStoreDerivative(IGetDataStoreDerivativeByIdQuery getDataStoreDerivativeByIdQuery, int id)
      {
          var item = getDataStoreDerivativeByIdQuery.Execute(id);
          var model = DataStoreDerivativeMapper.ToModel(item);
          return Task.FromResult(Results.Ok(model));
      }
  }
  ```

  `EditDataStoreDerivative.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Admin.DataAccess.Contexts;
  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
  using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
  using EdFi.Ods.AdminApi.Common.Settings;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;
  using FluentValidation;
  using Microsoft.Extensions.Options;
  using Swashbuckle.AspNetCore.Annotations;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  public class EditDataStoreDerivative : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder
              .MapPut(endpoints, "/dataStoreDerivatives/{id}", Handle)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponseCode(204))
              .BuildForVersions(AdminApiVersions.V3);
      }

      public static async Task<IResult> Handle(Validator validator, IEditDataStoreDerivativeCommand editDataStoreDerivativeCommand, IUsersContext db, EditDataStoreDerivativeRequest request, int id)
      {
          request.Id = id;
          SetCurrentConnectionString(db, request, id);
          await validator.GuardAsync(request);
          editDataStoreDerivativeCommand.Execute(request);
          return Results.NoContent();
      }

      private static void SetCurrentConnectionString(IUsersContext db, EditDataStoreDerivativeRequest request, int id)
      {
          if (string.IsNullOrEmpty(request.ConnectionString))
              request.ConnectionString = db.OdsInstanceDerivatives.Find(id)?.ConnectionString;
      }

      [SwaggerSchema(Title = "EditDataStoreDerivativeRequest")]
      public class EditDataStoreDerivativeRequest : IEditDataStoreDerivativeModel
      {
          [SwaggerExclude]
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeIdDescription, Nullable = false)]
          public int Id { get; set; }
          [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeDataStoreIdDescription, Nullable = false)]
          public int DataStoreId { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeDerivativeTypeDescription, Nullable = false)]
          public string? DerivativeType { get; set; }
          [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeConnectionStringDescription, Nullable = false)]
          public string? ConnectionString { get; set; }
      }

      public class Validator : AbstractValidator<EditDataStoreDerivativeRequest>
      {
          private readonly IGetDataStoreQuery _getDataStoreQuery;
          private readonly IGetDataStoreDerivativesQuery _getDataStoreDerivativesQuery;
          private readonly string _databaseEngine;

          public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreDerivativesQuery getDataStoreDerivativesQuery, IOptions<AppSettings> options)
          {
              _getDataStoreQuery = getDataStoreQuery;
              _getDataStoreDerivativesQuery = getDataStoreDerivativesQuery;
              _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

              RuleFor(m => m.DerivativeType).NotEmpty();

              RuleFor(m => m.DerivativeType)
                  .Matches("^(?i)(readreplica|snapshot)$")
                  .WithMessage(FeatureConstants.OdsInstanceDerivativeDerivativeTypeNotValid)
                  .When(m => !string.IsNullOrEmpty(m.DerivativeType));

              RuleFor(m => m.DataStoreId)
                  .NotEqual(0)
                  .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

              RuleFor(m => m.DataStoreId)
                  .Must(BeAnExistingDataStore)
                  .When(m => !m.DataStoreId.Equals(0));

              RuleFor(m => m.ConnectionString)
                  .Must(BeAValidConnectionString)
                  .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                  .When(m => !string.IsNullOrWhiteSpace(m.ConnectionString));

              RuleFor(d => d)
                  .Must(BeUniqueCombinedKey)
                  .WithMessage(FeatureConstants.OdsInstanceDerivativeCombinedKeyMustBeUnique);
          }

          private bool BeAnExistingDataStore(int id)
          {
              _getDataStoreQuery.Execute(id);
              return true;
          }

          private bool BeAValidConnectionString(string? connectionString)
          {
              return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
          }

          private bool BeUniqueCombinedKey(EditDataStoreDerivativeRequest request)
          {
              return !_getDataStoreDerivativesQuery.Execute().Exists(
                  x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                  x.DerivativeType.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase) &&
                  x.OdsInstanceDerivativeId != request.Id);
          }
      }
  }
  ```

  `DeleteDataStoreDerivative.cs`:
  ```csharp
  // SPDX-License-Identifier: Apache-2.0
  // Licensed to the Ed-Fi Alliance under one or more agreements.
  // The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
  // See the LICENSE and NOTICES files in the project root for more information.

  using EdFi.Ods.AdminApi.Common.Features;
  using EdFi.Ods.AdminApi.Common.Infrastructure;
  using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

  namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

  public class DeleteDataStoreDerivative : IFeature
  {
      public void MapEndpoints(IEndpointRouteBuilder endpoints)
      {
          AdminApiEndpointBuilder.MapDelete(endpoints, "/dataStoreDerivatives/{id}", Handle)
              .WithDefaultSummaryAndDescription()
              .WithRouteOptions(b => b.WithResponseCode(204))
              .BuildForVersions(AdminApiVersions.V3);
      }

      public static Task<IResult> Handle(IDeleteDataStoreDerivativeCommand deleteDataStoreDerivativeCommand, int id)
      {
          deleteDataStoreDerivativeCommand.Execute(id);
          return Task.FromResult(Results.NoContent());
      }
  }
  ```

- [ ] **Step 4.6: Delete old `Features/OdsInstanceDerivative/` folder**

  ```powershell
  Remove-Item -Recurse -Force "Application/EdFi.Ods.AdminApi.V3/Features/OdsInstanceDerivative"
  ```

- [ ] **Step 4.7: Run build — should compile clean**

  ```powershell
  cd Application; dotnet build EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj
  ```

  Expected: **0 errors**. Fix any remaining compile errors before proceeding.

- [ ] **Step 4.8: Commit Tasks 2-4**

  ```powershell
  git add Application/EdFi.Ods.AdminApi.V3/
  git commit -m "feat(v3): rename OdsInstance/Context/Derivative endpoints and classes to DataStore

  - /odsInstances -> /dataStores, /odsInstanceContexts -> /dataStoreContexts
  - /odsInstanceDerivatives -> /dataStoreDerivatives
  - Rename feature folders, models, mappers, feature classes
  - Rename infrastructure commands and queries
  - JSON: instanceType->dataStoreType, odsInstanceId->dataStoreId in context/derivative bodies

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 5: Cross-cutting — `dataStoreIds` in Applications and ApiClients

**Files:** `Features/Applications/ApplicationModel.cs`, `AddApplication.cs`, `EditApplication.cs`, `ApplicationMapper.cs`, `Features/ApiClients/ApiClientModel.cs`, `AddApiClient.cs`, `EditApiClient.cs`, `ApiClientMapper.cs`, and four Infrastructure files.

- [ ] **Step 5.1: Update `Features/Applications/ApplicationModel.cs`**

  Change the `OdsInstanceIds` property to `DataStoreIds`:
  ```csharp
  // Replace in ApplicationModel class:
  public IList<int>? OdsInstanceIds { get; set; }
  // becomes:
  public IList<int>? DataStoreIds { get; set; }
  ```

- [ ] **Step 5.2: Update `Features/ApiClients/ApiClientModel.cs`**

  ```csharp
  // Replace in ApiClientModel class:
  public IList<int>? OdsInstanceIds { get; set; }
  // becomes:
  public IList<int>? DataStoreIds { get; set; }
  ```

- [ ] **Step 5.3: Update `Features/Applications/ApplicationMapper.cs`**

  Change `OdsInstanceIds = odsInstanceIds` → `DataStoreIds = dataStoreIds`, and rename the parameter from `odsInstanceIds` to `dataStoreIds` throughout:
  ```csharp
  public static ApplicationModel ToModel(Application source, IList<int> dataStoreIds)
  {
      return new ApplicationModel
      {
          Id = source.ApplicationId,
          ApplicationName = source.ApplicationName,
          ClaimSetName = source.ClaimSetName,
          EducationOrganizationIds = source.EducationOrganizationIds(),
          VendorId = source.VendorId(),
          ProfileIds = source.Profiles(),
          Enabled = source.ApiClients.All(a => a.IsApproved),
          DataStoreIds = dataStoreIds
      };
  }

  public static List<ApplicationModel> ToModelList(
      IEnumerable<Application> source,
      IReadOnlyDictionary<int, IList<int>> dataStoreIdsByApplicationId)
  {
      return source
          .Select(application =>
          {
              dataStoreIdsByApplicationId.TryGetValue(application.ApplicationId, out var dataStoreIds);
              return ToModel(application, dataStoreIds ?? new List<int>());
          })
          .ToList();
  }
  ```

- [ ] **Step 5.4: Update `Features/ApiClients/ApiClientMapper.cs`**

  ```csharp
  public static ApiClientModel ToModel(ApiClient source, IList<int> dataStoreIds)
  {
      return new ApiClientModel
      {
          Id = source.ApiClientId,
          Name = source.Name,
          ClientId = source.Key,
          ApplicationId = source.Application?.ApplicationId ?? 0,
          KeyStatus = source.KeyStatus,
          IsApproved = source.IsApproved,
          EducationOrganizationIds = source.ApplicationEducationOrganizations
              .Select(eu => eu.EducationOrganizationId)
              .ToList(),
          DataStoreIds = dataStoreIds
      };
  }

  public static List<ApiClientModel> ToModelList(
      IEnumerable<ApiClient> source,
      IReadOnlyDictionary<int, IList<int>> dataStoreIdsByApiClientId)
  {
      return source.Select(apiClient =>
      {
          dataStoreIdsByApiClientId.TryGetValue(apiClient.ApiClientId, out var dataStoreIds);
          return ToModel(apiClient, dataStoreIds ?? new List<int>());
      }).ToList();
  }
  ```

- [ ] **Step 5.5: Update `Features/Applications/AddApplication.cs`**

  In `AddApplicationRequest`, rename property `OdsInstanceIds` → `DataStoreIds`.
  In `ValidateOdsInstanceIds(...)`, rename the method to `ValidateDataStoreIds(...)` and update the `nameof(request.OdsInstanceIds)` → `nameof(request.DataStoreIds)` and the error message references. Also update the call in `GuardAgainstInvalidEntityReferences`.
  In the `Validator` class, update `RuleFor(m => m.OdsInstanceIds)` → `RuleFor(m => m.DataStoreIds)` and update the `WithMessage(FeatureConstants.OdsInstanceIdsValidationMessage)` → `WithMessage(FeatureConstants.DataStoreIdsValidationMessage)`.

- [ ] **Step 5.6: Update `Features/Applications/EditApplication.cs`**

  Apply the same changes as Step 4.5 but for `EditApplicationRequest` and its `ValidateOdsInstanceIds` method.

- [ ] **Step 5.7: Update `Features/ApiClients/AddApiClient.cs`**

  Rename `OdsInstanceIds` → `DataStoreIds` in `AddApiClientRequest`. Update `ValidateOdsInstanceIds` → `ValidateDataStoreIds`, update `nameof` and error message references. Update `RuleFor(m => m.OdsInstanceIds)` → `RuleFor(m => m.DataStoreIds)` in the validator.

- [ ] **Step 5.8: Update `Features/ApiClients/EditApiClient.cs`**

  Apply the same changes as Step 4.7 but for `EditApiClientRequest`.

- [ ] **Step 5.9: Update Infrastructure queries for DataStore IDs by Application/ApiClient ID**

  In `Infrastructure/Database/Queries/GetOdsInstanceIdsByApplicationIdQuery.cs`, rename:
  - Interface: `IGetOdsInstanceIdsByApplicationIdQuery` → `IGetDataStoreIdsByApplicationIdQuery`
  - Class: `GetOdsInstanceIdsByApplicationIdQuery` → `GetDataStoreIdsByApplicationIdQuery`
  - The method body references `p.OdsInstance.OdsInstanceId` — keep those (DB column names don't change).

  In `Infrastructure/Database/Queries/GetOdsInstanceIdsByApiClientIdQuery.cs`, rename:
  - Interface: `IGetOdsInstanceIdsByApiClientIdQuery` → `IGetDataStoreIdsByApiClientIdQuery`
  - Class: `GetOdsInstanceIdsByApiClientIdQuery` → `GetDataStoreIdsByApiClientIdQuery`

  In `Infrastructure/Database/Commands/AddApiClientOdsInstanceCommand.cs` — this works with the DB entity `ApiClientOdsInstance` directly and is not API-surface-facing. Rename for consistency:
  - Interface: `IAddApiClientOdsInstanceCommand` → `IAddApiClientDataStoreCommand`
  - Class: `AddApiClientOdsInstanceCommand` → `AddApiClientDataStoreCommand`

  In `Infrastructure/Database/Queries/GetApiClientOdsInstanceQuery.cs`:
  - Interface: `IGetApiClientOdsInstanceQuery` → `IGetApiClientDataStoreQuery`
  - Class: `GetApiClientOdsInstanceQuery` → `GetApiClientDataStoreQuery`

- [ ] **Step 5.10: Update all callers of the renamed query interfaces**

  Search for usages of the old interface names across V3 features and update them:
  ```powershell
  grep -rn "IGetOdsInstanceIdsByApplicationIdQuery\|IGetOdsInstanceIdsByApiClientIdQuery\|IAddApiClientOdsInstanceCommand\|IGetApiClientOdsInstanceQuery" Application/EdFi.Ods.AdminApi.V3/
  ```

  For each result, replace the old interface name with the new one (e.g., in `ReadApplication.cs`, `ReadApiClient.cs`, commands that add/edit Applications and ApiClients).

- [ ] **Step 5.11: Run build — should compile clean**

  ```powershell
  cd Application; dotnet build EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj
  ```

  Expected: **0 errors**.

- [ ] **Step 5.12: Commit Task 5**

  ```powershell
  git add Application/EdFi.Ods.AdminApi.V3/
  git commit -m "feat(v3): rename odsInstanceIds to dataStoreIds in Application and ApiClient

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 6: Update Unit Tests

**Project:** `Application/EdFi.Ods.AdminApi.V3.UnitTests/`

- [ ] **Step 6.1: Update `Features/Applications/ApplicationMapperTests.cs`**

  Replace `OdsInstanceIds` with `DataStoreIds` in all assertions and variable names. For example:
  ```csharp
  // Replace:
  model.OdsInstanceIds.ShouldBe(odsInstanceIds);
  // With:
  model.DataStoreIds.ShouldBe(dataStoreIds);

  // Replace variable names:
  var odsInstanceIds = new List<int> { 1, 2 };
  // With:
  var dataStoreIds = new List<int> { 1, 2 };

  // Test method names that reference OdsInstanceIds:
  public void ToModel_OdsInstanceIds_AreMappedDirectly()
  // becomes:
  public void ToModel_DataStoreIds_AreMappedDirectly()
  ```

  Also update `ToModelList` test assertions:
  ```csharp
  // Replace:
  models[0].OdsInstanceIds.ShouldBe(new List<int> { 10 });
  models[1].OdsInstanceIds.ShouldBe(new List<int> { 20, 30 });
  models[0].OdsInstanceIds.ShouldNotBeNull();
  models[0].OdsInstanceIds!.Count.ShouldBe(0);
  // With:
  models[0].DataStoreIds.ShouldBe(new List<int> { 10 });
  models[1].DataStoreIds.ShouldBe(new List<int> { 20, 30 });
  models[0].DataStoreIds.ShouldNotBeNull();
  models[0].DataStoreIds!.Count.ShouldBe(0);
  ```

- [ ] **Step 6.2: Update `Features/ApiClients/ApiClientModelTests.cs`**

  ```csharp
  // Replace all occurrences of OdsInstanceIds with DataStoreIds:
  model.OdsInstanceIds.ShouldBeNull();       →  model.DataStoreIds.ShouldBeNull();
  OdsInstanceIds = new List<int> { 1, 2 }   →  DataStoreIds = new List<int> { 1, 2 }
  model.OdsInstanceIds.ShouldBe(...)        →  model.DataStoreIds.ShouldBe(...)
  ```

- [ ] **Step 6.3: Update `Features/ApiClients/ReadApiClientTests.cs`**

  ```csharp
  // Replace:
  okResult.Value.OdsInstanceIds.ShouldBe(new List<int> { 10, 20 });
  // With:
  okResult.Value.DataStoreIds.ShouldBe(new List<int> { 10, 20 });
  ```

  Also update injected query types if they reference the old names:
  ```csharp
  // Replace:
  A.Fake<IGetOdsInstanceIdsByApiClientIdQuery>()
  // With:
  A.Fake<IGetDataStoreIdsByApiClientIdQuery>()
  ```

- [ ] **Step 6.4: Update `Features/ApiClients/AddApiClientOdsInstanceIdsValidationTests.cs`**

  Update class name references:
  ```csharp
  // The test fixture class name can stay or be renamed for clarity. Update field name in request:
  OdsInstanceIds = ids   →   DataStoreIds = ids
  OdsInstanceIds = null  →   DataStoreIds = null
  ```

  Update method name `ValidateOdsInstanceIds` → `ValidateDataStoreIds` in the reflection lookup:
  ```csharp
  _validateMethod = typeof(AddApiClient)
      .GetMethod("ValidateDataStoreIds", BindingFlags.NonPublic | BindingFlags.Static);
  ```

- [ ] **Step 6.5: Update `Features/ApiClients/EditApiClientOdsInstanceIdsValidationTests.cs`**

  Apply the same changes as Step 6.4 but referencing `EditApiClient` instead of `AddApiClient`.

- [ ] **Step 6.6: Update `Features/ApiClients/AddApiClientValidatorTests.cs` and `EditApiClientValidatorTests.cs`**

  Replace any references to `OdsInstanceIds` → `DataStoreIds` in test setup and assertions. Replace `OdsInstanceIdsValidationMessage` → `DataStoreIdsValidationMessage` in any expected error message checks.

- [ ] **Step 6.7: Update `Infrastructure/Database/Queries/GetOdsInstanceQueryTests.cs` and `GetOdsInstancesQueryTests.cs`**

  These test the query classes. Update:
  - Type references from `IGetOdsInstanceQuery` → `IGetDataStoreQuery`, `GetOdsInstanceQuery` → `GetDataStoreQuery`
  - `IGetOdsInstancesQuery` → `IGetDataStoresQuery`, `GetOdsInstancesQuery` → `GetDataStoresQuery`
  - Any references to `instanceType` filter parameter → `dataStoreType`

- [ ] **Step 6.8: Run unit tests**

  ```powershell
  cd Application; dotnet test EdFi.Ods.AdminApi.V3.UnitTests/EdFi.Ods.AdminApi.V3.UnitTests.csproj --no-build -v minimal 2>&1 | tail -20
  ```

  Expected: **All tests pass, 0 failures**.

- [ ] **Step 6.9: Commit Task 6**

  ```powershell
  git add Application/EdFi.Ods.AdminApi.V3.UnitTests/
  git commit -m "test(v3): update unit tests for DataStore rename

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Task 7: Update E2E Bruno Tests

**Directory:** `Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/`

- [ ] **Step 7.1: Rename Bruno test folders**

  ```powershell
  $e2e = "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3"
  Rename-Item "$e2e/OdsInstances"                      "DataStores"
  Rename-Item "$e2e/OdsInstanceContexts"               "DataStoreContexts"
  Rename-Item "$e2e/OdsInstanceDerivatives"            "DataStoreDerivatives"
  Rename-Item "$e2e/Multitenant Isolation - OdsInstances" "Multitenant Isolation - DataStores"
  ```

- [ ] **Step 7.2: Rename `.bru` files that include "OdsInstance" in their name**

  In each of the four renamed folders, rename any `.bru` files that contain "OdsInstance" or "OdsInstances" in their filenames to use "DataStore" / "DataStores". For example:
  ```
  GET - OdsInstances.bru             →  GET - DataStores.bru
  POST - OdsInstances.bru            →  POST - DataStores.bru
  GET - OdsInstances by ID.bru       →  GET - DataStores by ID.bru
  GET - OdsInstances by ID Application.bru  →  GET - DataStores by ID Application.bru
  DELETE - OdsInstances.bru          →  DELETE - DataStores.bru
  PUT - OdsInstances.bru             →  PUT - DataStores.bru
  DELETE - OdsInstanceContexts.bru   →  DELETE - DataStoreContexts.bru
  ... (all files in all four folders)
  ```

  Use PowerShell to do this in bulk:
  ```powershell
  Get-ChildItem -Recurse "$e2e" -Filter "*.bru" | Where-Object { $_.Name -like "*OdsInstance*" } | ForEach-Object {
      $newName = $_.Name -replace "OdsInstances", "DataStores" -replace "OdsInstanceContexts", "DataStoreContexts" -replace "OdsInstanceDerivatives", "DataStoreDerivatives" -replace "OdsInstanceContext", "DataStoreContext" -replace "OdsInstanceDerivative", "DataStoreDerivative" -replace "OdsInstance", "DataStore"
      Rename-Item $_.FullName $newName
  }
  ```

- [ ] **Step 7.3: Update URLs inside `.bru` files**

  In each renamed folder, update the URL strings inside `.bru` files:
  ```powershell
  Get-ChildItem -Recurse "$e2e" -Filter "*.bru" | ForEach-Object {
      $content = Get-Content $_.FullName -Raw
      $updated = $content `
          -replace "/v3/odsInstances", "/v3/dataStores" `
          -replace "/v3/odsInstanceContexts", "/v3/dataStoreContexts" `
          -replace "/v3/odsInstanceDerivatives", "/v3/dataStoreDerivatives"
      if ($content -ne $updated) {
          Set-Content $_.FullName $updated
      }
  }
  ```

- [ ] **Step 7.4: Update JSON field names inside `.bru` files**

  ```powershell
  Get-ChildItem -Recurse "$e2e" -Filter "*.bru" | ForEach-Object {
      $content = Get-Content $_.FullName -Raw
      $updated = $content `
          -replace '"instanceType"', '"dataStoreType"' `
          -replace '"odsInstanceId"', '"dataStoreId"' `
          -replace '"odsInstanceIds"', '"dataStoreIds"' `
          -replace '"odsInstanceContexts"', '"dataStoreContexts"' `
          -replace '"odsInstanceDerivatives"', '"dataStoreDerivatives"' `
          -replace 'instanceType', 'dataStoreType' `
          -replace 'OdsInstanceGUID', 'DataStoreGUID' `
          -replace 'CreatedOdsInstanceId', 'CreatedDataStoreId'
      if ($content -ne $updated) {
          Set-Content $_.FullName $updated
      }
  }
  ```

  > **Note:** After running this, manually review a few `.bru` files to verify replacements are correct and no unintended substitutions occurred (e.g., variable names, test names). Pay particular attention to the `GET - DataStores.bru` schema validation block which checks for `instanceType` — update the schema to check for `dataStoreType` instead.

- [ ] **Step 7.5: Commit Task 7**

  ```powershell
  git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/"
  git commit -m "test(v3): rename E2E Bruno test folders/files and update URLs and fields for DataStore

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```

---

## Final Validation

- [ ] **Step F.1: Full build**

  ```powershell
  cd C:\GAP\EdFi\ODS-Admin-API\ODS-Admin-API; .\build.ps1 -Command Build
  ```

  Expected: Build succeeds with **0 errors**.

- [ ] **Step F.2: Unit tests**

  ```powershell
  cd C:\GAP\EdFi\ODS-Admin-API\ODS-Admin-API; .\build.ps1 -Command UnitTest
  ```

  Expected: All tests pass with **0 failures**.

- [ ] **Step F.3: Verify no lingering old names remain in V3**

  ```powershell
  grep -rn "odsInstances\|odsInstanceContexts\|odsInstanceDerivatives\|OdsInstanceIds\|odsInstanceIds\|instanceType\|OdsInstanceContext\b\|OdsInstanceDerivative\b" Application/EdFi.Ods.AdminApi.V3/ --include="*.cs" --include="*.bru" | grep -v "/obj/" | grep -v "OdsInstanceContext\." | grep -v "OdsInstanceDerivative\."
  ```

  Expected: No results (except legitimate references to EF Core entity property names like `.OdsInstanceContexts`, `.OdsInstanceDerivatives`, `.OdsInstanceId` which are DB-layer names that intentionally remain unchanged).

- [ ] **Step F.4: Final commit if any fixes were needed**

  ```powershell
  git add .
  git commit -m "fix(v3): address remaining rename issues found during final validation

  Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
  ```
