# V2/V3 Common-Library Dedup Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate the remaining confirmed-safe duplication between the V2 (`EdFi.Ods.AdminApi`) and V3 (`EdFi.Ods.AdminApi.V3`) projects by moving byte-identical (namespace-only-diff) files into `EdFi.Ods.AdminApi.Common` / `EdFi.Ods.AdminApi.DBTests.Common`, without touching anything that carries a real per-version behavioral difference.

**Architecture:** Two phases. Phase 1 is purely mechanical file moves (plain DTOs, mappers, extension/helper classes) with zero production-behavior change. Phase 2 unifies the production `AdminApiDbContext` (discovered mid-planning to be a namespace-only duplicate itself, previously assumed genuinely version-specific), which unblocks moving `AdminApiAuditLogWriter` and de-static-ing `PlatformUsersContextTestBase`/`AdminApiDbContextTestBase` into the shared DBTests project.

**Tech Stack:** .NET 10, C#, NUnit, FluentValidation, EF Core, NuGet Central Package Management (`Directory.Packages.props`).

**Spec:** `docs/design/2026-08-13-v2-v3-common-dedup-phase2.md`

## Global Constraints

- Every moved file must first be diffed (v2 copy vs v3 copy) to confirm it really is namespace-only — do not trust this plan's classification blindly, `diff` is one command and catches drift since the spec was written.
- No behavior change anywhere in this plan except the two explicitly-called-out simplifications in Task 11 (collapsing now-identical V2/V3 branches in `WebApplicationBuilderExtensions.cs`) — those preserve existing behavior for every mode, they just stop duplicating it.
- After every task: `dotnet build` the full solution from `Application/` (`dotnet build Ed-Fi-ODS-AdminApi.sln`) must show `0 Warning(s) 0 Error(s)` before moving to the next task. `TreatWarningsAsErrors` is `true` in every project, so a missed `using` is a build failure, not a silent bug.
- Commit after each task (see each task's final step) — small, logically-scoped commits, matching how the Phase-1-of-this-cleanup work already on this branch was committed.
- V1 (`EdFi.Ods.AdminApi.V1`) is out of scope everywhere in this plan — it uses its own vendored data-access types and does not share any of the moved code.

---

## Task 1: Move Profiles (Model + Mapper + Validator) to Common

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Features/Profiles/ProfileModel.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Features/Profiles/ProfileMapper.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Features/Profiles/ProfileValidator.cs`
- Delete: `Application/EdFi.Ods.AdminApi/Features/Profiles/ProfileModel.cs`, `ProfileMapper.cs`, `ProfileValidator.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileModel.cs`, `ProfileMapper.cs`, `ProfileValidator.cs`
- Modify (usings only, via build-error-driven fix in Step 4): consumers in both projects' `Features/Profiles/*.cs` and both `UnitTests`' `Features/Profiles/*.cs` / `Infrastructure/Database/Commands/{Add,Edit}ProfileCommandTests.cs`

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.Common.Features.Profiles.ProfileModel`, `.ProfileDetailsModel`, `.ProfileMapper` (static, `ToModel`/`ToDetailsModel`/`ToModelList`), `.ProfileValidator` (instance, `Validate<T>(string name, string definition, ValidationContext<T> context)`)

- [ ] **Step 1: Diff v2 and v3 copies to confirm still namespace-only**

Run from `Application/`:
```bash
diff EdFi.Ods.AdminApi/Features/Profiles/ProfileModel.cs EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileModel.cs
diff EdFi.Ods.AdminApi/Features/Profiles/ProfileMapper.cs EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileMapper.cs
diff EdFi.Ods.AdminApi/Features/Profiles/ProfileValidator.cs EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileValidator.cs
```
Expected: only `namespace EdFi.Ods.AdminApi.Features.Profiles;` vs `namespace EdFi.Ods.AdminApi.V3.Features.Profiles;` (plus harmless trailing blank lines in the v3 copies). If anything else differs, stop — this file has diverged since the spec was written and needs re-investigation, not a mechanical move.

- [ ] **Step 2: Create the three files in Common**

`Application/EdFi.Ods.AdminApi.Common/Features/Profiles/ProfileModel.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Common.Features.Profiles;

[SwaggerSchema(Title = "Profile")]
public class ProfileModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

[SwaggerSchema(Title = "ProfileDetails")]
public class ProfileDetailsModel : ProfileModel
{
    public string? Definition { get; set; }
}
```

`Application/EdFi.Ods.AdminApi.Common/Features/Profiles/ProfileMapper.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AdminProfile = EdFi.Admin.DataAccess.Models.Profile;

namespace EdFi.Ods.AdminApi.Common.Features.Profiles;

public static class ProfileMapper
{
    public static ProfileModel ToModel(AdminProfile source)
    {
        return new ProfileModel
        {
            Id = source.ProfileId,
            Name = source.ProfileName
        };
    }

    public static ProfileDetailsModel ToDetailsModel(AdminProfile source)
    {
        return new ProfileDetailsModel
        {
            Id = source.ProfileId,
            Name = source.ProfileName,
            Definition = source.ProfileDefinition
        };
    }

    public static List<ProfileModel> ToModelList(IEnumerable<AdminProfile> source)
    {
        return source.Select(ToModel).ToList();
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Features/Profiles/ProfileValidator.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using FluentValidation;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace EdFi.Ods.AdminApi.Common.Features.Profiles
{
    public class ProfileValidator
    {
        public void Validate<T>(string name, string definition, ValidationContext<T> context)
        {
            var schema = new XmlSchemaSet();
            var path = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!).LocalPath;
            schema.Add("", Path.Combine(path, "Schema", "Ed-Fi-ODS-API-Profile.xsd"));
            var propertyName = "Definition";

            void EventHandler(object? sender, ValidationEventArgs e)
            {
                if (e.Severity == XmlSeverityType.Error)
                {
                    context.AddFailure(propertyName, e.Message);
                }
            }
            try
            {
                var document = new XmlDocument();
                document.LoadXml(definition);
                document.Schemas.Add(schema);
                document.Validate(EventHandler);

                var profile = document.DocumentElement;
                if (profile != null && !string.IsNullOrEmpty(name))
                {
                    var profileName = profile.GetAttribute("name");
                    if(!profileName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        context.AddFailure(propertyName, $"Profile name attribute value should match with {name}." );
                    }
                }          
            }
            catch (Exception ex)
            {
                context.AddFailure(propertyName, ex.Message.ToString());
            }
        }
    }
}
```

Note: `ProfileValidator.Validate` reads a `Schema/Ed-Fi-ODS-API-Profile.xsd` file relative to the executing assembly's location. Confirmed: `EdFi.Ods.AdminApi/EdFi.Ods.AdminApi.csproj` has
`<Content Include="Schema\Ed-Fi-ODS-API-Profile.xsd" CopyToPublishDirectory="Always" CopyToOutputDirectory="Always" />`
(so V2 currently works). `EdFi.Ods.AdminApi.V3/EdFi.Ods.AdminApi.V3.csproj` has `EnableDefaultContentItems=false` and no equivalent entry — the file exists on disk at `EdFi.Ods.AdminApi.V3/Schema/Ed-Fi-ODS-API-Profile.xsd` but is not copied to V3's build output, a pre-existing bug unrelated to this plan (do not fix it here — out of scope; V3's profile validation likely already fails at runtime today, before this move). Once `Assembly.GetExecutingAssembly()` resolves to `EdFi.Ods.AdminApi.Common.dll`, V2's copy mechanism no longer applies either. Copy the schema file to `Application/EdFi.Ods.AdminApi.Common/Schema/Ed-Fi-ODS-API-Profile.xsd` and add to `Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj`:
```xml
<ItemGroup>
  <Content Include="Schema\Ed-Fi-ODS-API-Profile.xsd" CopyToPublishDirectory="Always" CopyToOutputDirectory="Always" />
</ItemGroup>
```
This preserves V2's current working behavior (schema resolves from wherever `ProfileValidator` executes) and does not attempt to fix V3's separate, pre-existing gap.

- [ ] **Step 3: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi/Features/Profiles/ProfileModel.cs
rm Application/EdFi.Ods.AdminApi/Features/Profiles/ProfileMapper.cs
rm Application/EdFi.Ods.AdminApi/Features/Profiles/ProfileValidator.cs
rm Application/EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileModel.cs
rm Application/EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileMapper.cs
rm Application/EdFi.Ods.AdminApi.V3/Features/Profiles/ProfileValidator.cs
```

- [ ] **Step 4: Fix consumers via the build-error-driven loop**

Run `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`. It will fail with `CS0246`-style errors ("The type or namespace name 'ProfileModel' could not be found") in every file that referenced these types via same-namespace implicit access. For each file the compiler names, add the missing using and rebuild:

```csharp
using EdFi.Ods.AdminApi.Common.Features.Profiles;
```

Expected files (from `grep -rl "ProfileModel\|ProfileMapper\|ProfileValidator" Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests --include=*.cs`), confirmed this plan's investigation already found these to be the consumers:
- `EdFi.Ods.AdminApi/Features/Profiles/{ReadProfile,AddProfile,EditProfile}.cs`
- `EdFi.Ods.AdminApi.V3/Features/Profiles/{ReadProfile,AddProfile,EditProfile}.cs`
- `EdFi.Ods.AdminApi.UnitTests/Infrastructure/Database/Commands/{Add,Edit}ProfileCommandTests.cs`
- `EdFi.Ods.AdminApi.UnitTests/Features/Profiles/{AddProfileValidatorTests,EditProfileValidatorTests,ProfileMapperTests,ProfileValidatorTests,ReadProfileTests}.cs`
- `EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/Database/Commands/{Add,Edit}ProfileCommandTests.cs`
- `EdFi.Ods.AdminApi.V3.UnitTests/Features/Profiles/{ReadProfileTests,ProfileValidatorTests,ProfileMapperTests,EditProfileValidatorTests,AddProfileValidatorTests}.cs`

Repeat build-and-fix until `dotnet build Application/Ed-Fi-ODS-AdminApi.sln` reports `0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: all four unit test projects (`EdFi.Ods.AdminApi.Common.UnitTests`, `EdFi.Ods.AdminApi.UnitTests`, `EdFi.Ods.AdminApi.V3.UnitTests`, `EdFi.Ods.AdminApi.InstanceManagement.UnitTests`) show `Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Features/Profiles Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj Application/EdFi.Ods.AdminApi.Common/Schema Application/EdFi.Ods.AdminApi/Features/Profiles Application/EdFi.Ods.AdminApi.V3/Features/Profiles Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests
git commit -m "Move Profiles Model/Mapper/Validator to Common"
```

---

## Task 2: Move ResourceClaimActionAuthStrategyModel, ResourceClaimActionModel, TenantModel to Common

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Features/ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Features/ResourceClaimActions/ResourceClaimActionModel.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Features/Tenants/TenantModel.cs`
- Delete the matching v2 and v3 copies (6 files total)

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.Common.Features.ResourceClaimActionAuthStrategies.{ResourceClaimActionAuthStrategyModel, ActionWithAuthorizationStrategy, AuthorizationStrategyModelForAction}`, `EdFi.Ods.AdminApi.Common.Features.ResourceClaimActions.{ResourceClaimActionModel, ActionForResourceClaimModel}`, `EdFi.Ods.AdminApi.Common.Features.Tenants.{TenantModel, TenantModelConnectionStrings}`

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi/Features/ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs Application/EdFi.Ods.AdminApi.V3/Features/ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs
diff Application/EdFi.Ods.AdminApi/Features/ResourceClaimActions/ResourceClaimActionModel.cs Application/EdFi.Ods.AdminApi.V3/Features/ResourceClaimActions/ResourceClaimActionModel.cs
diff Application/EdFi.Ods.AdminApi/Features/Tenants/TenantModel.cs Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantModel.cs
```
Expected: namespace line only (plus a trailing blank line in the v3 copies).

- [ ] **Step 2: Create the three files in Common**

`Application/EdFi.Ods.AdminApi.Common/Features/ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Features.ResourceClaimActionAuthStrategies
{
    public class ResourceClaimActionAuthStrategyModel
    {
        public int ResourceClaimId { get; set; }
        public string ResourceName { get; set; } = string.Empty;

        public string ClaimName { get; set; } = string.Empty;

        public IReadOnlyList<ActionWithAuthorizationStrategy> AuthorizationStrategiesForActions { get; set; } = new List<ActionWithAuthorizationStrategy>();
    }

    public class ActionWithAuthorizationStrategy
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public IReadOnlyList<AuthorizationStrategyModelForAction> AuthorizationStrategies { get; set; } = new List<AuthorizationStrategyModelForAction>();

    }

    public class AuthorizationStrategyModelForAction
    {
        public int AuthStrategyId { get; set; }
        public string AuthStrategyName { get; set; } = string.Empty;
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Features/ResourceClaimActions/ResourceClaimActionModel.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Features.ResourceClaimActions
{
    public class ResourceClaimActionModel
    {
        public int ResourceClaimId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ClaimName { get; set; } = string.Empty;
        public List<ActionForResourceClaimModel> Actions { get; set; } = new List<ActionForResourceClaimModel>();
    }

    public class ActionForResourceClaimModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Features/Tenants/TenantModel.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Features.Tenants;

public class TenantModel
{
    public required string TenantName { get; set; }

    public TenantModelConnectionStrings ConnectionStrings { get; set; } = new();
}

public class TenantModelConnectionStrings
{
    public string EdFiSecurityConnectionString { get; set; }
    public string EdFiAdminConnectionString { get; set; }

    public TenantModelConnectionStrings()
    {
        EdFiAdminConnectionString = string.Empty;
        EdFiSecurityConnectionString = string.Empty;
    }

    public TenantModelConnectionStrings(string edFiAdminConnectionString, string edFiSecurityConnectionString)
    {
        EdFiAdminConnectionString = edFiAdminConnectionString;
        EdFiSecurityConnectionString = edFiSecurityConnectionString;
    }
}
```

- [ ] **Step 3: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi/Features/ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs
rm Application/EdFi.Ods.AdminApi.V3/Features/ResourceClaimActionAuthStrategies/ResourceClaimActionAuthStrategyModel.cs
rm Application/EdFi.Ods.AdminApi/Features/ResourceClaimActions/ResourceClaimActionModel.cs
rm Application/EdFi.Ods.AdminApi.V3/Features/ResourceClaimActions/ResourceClaimActionModel.cs
rm Application/EdFi.Ods.AdminApi/Features/Tenants/TenantModel.cs
rm Application/EdFi.Ods.AdminApi.V3/Features/Tenants/TenantModel.cs
```

- [ ] **Step 4: Fix consumers via the build-error-driven loop**

Run `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`, add
`using EdFi.Ods.AdminApi.Common.Features.ResourceClaimActionAuthStrategies;` /
`using EdFi.Ods.AdminApi.Common.Features.ResourceClaimActions;` /
`using EdFi.Ods.AdminApi.Common.Features.Tenants;` to every file the compiler
names, rebuild, repeat until clean. Use `grep -rl "ResourceClaimActionAuthStrategyModel\|ResourceClaimActionModel\|TenantModel" Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests --include=*.cs` beforehand to know roughly which files to expect (Features/ResourceClaimActionAuthStrategies, Features/ResourceClaimActions, Features/Tenants, Infrastructure/Database/Queries for the auth-strategies/actions queries, and their UnitTests counterparts).

- [ ] **Step 5: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Features/ResourceClaimActionAuthStrategies Application/EdFi.Ods.AdminApi.Common/Features/ResourceClaimActions Application/EdFi.Ods.AdminApi.Common/Features/Tenants Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests
git commit -m "Move ResourceClaimActionAuthStrategyModel, ResourceClaimActionModel, TenantModel to Common"
```

---

## Task 3: Move Infrastructure Helpers and Documentation filters to Common

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/HealthCheckServiceExtensions.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/ConstantsHelper.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation/ProfileRequestExampleFilter.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs`
- Delete the matching v2 and v3 copies (12 files total)

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.Common.Infrastructure.Helpers.{HealthCheckServiceExtensions, IAppSettingsFileProvider, FileSystemAppSettingsFileProvider, ConstantsHelpers}`, `EdFi.Ods.AdminApi.Common.Infrastructure.Documentation.{ProfileRequestExampleAttribute, ProfileRequestExampleFilter, SwaggerOptionalAttribute, SwaggerOptionalSchemaFilter, SwaggerSchemaRemoveRequiredFilter, SwaggerExcludeAttribute, SwaggerExcludeSchemaFilter}`

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi/Infrastructure/Helpers/HealthCheckServiceExtensions.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/HealthCheckServiceExtensions.cs
diff Application/EdFi.Ods.AdminApi/Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs
diff Application/EdFi.Ods.AdminApi/Infrastructure/Helpers/ConstantsHelper.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/ConstantsHelper.cs
diff Application/EdFi.Ods.AdminApi/Infrastructure/Documentation/ProfileRequestExampleFilter.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Documentation/ProfileRequestExampleFilter.cs
diff Application/EdFi.Ods.AdminApi/Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs
diff Application/EdFi.Ods.AdminApi/Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs
```
Expected: namespace line only (plus trailing blank lines in v3 copies).

- [ ] **Step 2: Create the six files in Common**

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/HealthCheckServiceExtensions.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Settings;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddHealthCheck(
        this IServiceCollection services,
        IConfigurationRoot configuration
    )
    {
        var databaseEngine = configuration.Get("AppSettings:DatabaseEngine", "SqlServer");
        var multiTenancyEnabled = configuration.Get("AppSettings:MultiTenancy", false);

        if (!string.IsNullOrEmpty(databaseEngine))
        {
            var isSqlServer = DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer);
            var hcBuilder = services.AddHealthChecks();

            // Add health checks for both EdFi_Admin and EdFi_Security databases
            AddDatabaseHealthChecks(hcBuilder, configuration, "EdFi_Admin", multiTenancyEnabled, isSqlServer);
            AddDatabaseHealthChecks(hcBuilder, configuration, "EdFi_Security", multiTenancyEnabled, isSqlServer);
        }

        return services;
    }

    private static void AddDatabaseHealthChecks(
        IHealthChecksBuilder hcBuilder,
        IConfigurationRoot configuration,
        string connectionStringName,
        bool multiTenancyEnabled,
        bool isSqlServer
    )
    {
        Dictionary<string, string> connectionStrings;

        if (multiTenancyEnabled)
        {
            var tenantSettings =
                configuration.Get<TenantsSection>()
                ?? throw new AdminApiException("Unable to load tenant configuration from appSettings");

            connectionStrings = tenantSettings.Tenants.ToDictionary(
                x => x.Key,
                x => x.Value.ConnectionStrings[connectionStringName]
            );
        }
        else
        {
            connectionStrings = new()
            {
                { "SingleTenant", configuration.GetConnectionStringByName(connectionStringName) }
            };
        }

        foreach (var connectionString in connectionStrings)
        {
            var healthCheckName = multiTenancyEnabled
                ? $"{connectionString.Key}_{connectionStringName}"
                : connectionStringName;

            if (isSqlServer)
            {
                hcBuilder.AddSqlServer(connectionString.Value, name: healthCheckName, tags: ["Databases"]);
            }
            else
            {
                hcBuilder.AddNpgSql(connectionString.Value, name: healthCheckName, tags: ["Databases"]);
            }
        }
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public interface IAppSettingsFileProvider
{
    string ReadAllText();
    void WriteAllText(string content);
}

public class FileSystemAppSettingsFileProvider(string filePath) : IAppSettingsFileProvider
{
    public string ReadAllText()
    {
        return File.ReadAllText(filePath);
    }

    public void WriteAllText(string content)
    {
        File.WriteAllText(filePath, content);
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers/ConstantsHelper.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

public static class ConstantsHelpers
{
    /// <summary>
    /// Semantic version of the admin api.
    /// </summary>
    public const string Version = "2.0";

    /// <summary>
    /// Assembly version of the admin api.
    /// </summary>
    public static readonly string Build = Assembly.GetExecutingAssembly()
        .GetName()
        .Version?.ToString() ?? Version;
}
```

Note: `ConstantsHelpers.Build` reads `Assembly.GetExecutingAssembly().GetName().Version` — once moved, this resolves to `EdFi.Ods.AdminApi.Common.dll`'s assembly version instead of the v2/v3 app assembly's version. Check whatever consumes `ConstantsHelpers.Build` (`grep -rn "ConstantsHelpers.Build" Application --include=*.cs`) — if it's surfaced anywhere user-visible (e.g. a `/version` endpoint or Swagger info), confirm the version reported doesn't need to be the *application* assembly's version specifically. If it does, this file cannot move as a plain relocation — flag it and stop rather than silently changing what version string ships.

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation/ProfileRequestExampleFilter.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;

[AttributeUsage(AttributeTargets.Method)]
public class ProfileRequestExampleAttribute : Attribute
{
}

public class ProfileRequestExampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attribute = context.MethodInfo.GetCustomAttributes(typeof(ProfileRequestExampleAttribute), false).FirstOrDefault();
        if (attribute == null)
        {
            return;
        }

        var profileDefinition = @"<Profile name=""Test-Profile""><Resource name=""Resource1""><ReadContentType memberSelection=""IncludeOnly""><Collection name=""Collection1"" memberSelection=""IncludeOnly"">" +
                    @"<Property name=""Property1"" /><Property name=""Property2"" /></Collection></ReadContentType><WriteContentType memberSelection=""IncludeOnly"">" +
                    @"<Collection name=""Collection2"" memberSelection=""IncludeOnly""><Property name=""Property1"" /><Property name=""Property2"" />" +
                    @"</Collection></WriteContentType></Resource></Profile>";

        var profileRequest = new
        {
            name = "Test-Profile",
            definition = profileDefinition
        };

        foreach (var schema in context.SchemaRepository.Schemas)
        {
            if (schema.Key.ToLower().Contains("addprofilerequest") || schema.Key.ToLower().Contains("editprofilerequest"))
            {
                schema.Value.Example = new OpenApiString(JsonConvert.SerializeObject(profileRequest, Formatting.Indented), true);
            }
        }
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using EdFi.Common.Extensions;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;

[AttributeUsage(AttributeTargets.Property)]
public class SwaggerOptionalAttribute : Attribute
{
}

public class SwaggerOptionalSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var properties = context.Type.GetProperties();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(SwaggerOptionalAttribute));
            var propertyNameInCamelCasing = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            if (attribute != null)
            {
                schema.Required?.Remove(propertyNameInCamelCasing);
            }
            else
            {
                if (schema.Required == null)
                {
                    schema.Required = new HashSet<string>() { propertyNameInCamelCasing };
                }
                else
                {
                    schema.Required.Add(propertyNameInCamelCasing);
                }
            }
        }
    }
}

public class SwaggerSchemaRemoveRequiredFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var properties = context.Type.GetProperties();
        foreach (var property in properties)
        {
            var propertyNameInCamelCasing = property.Name.ToCamelCase();
            schema.Required?.Remove(propertyNameInCamelCasing);
        }
    }
}
```

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;

[AttributeUsage(AttributeTargets.Property)]
public class SwaggerExcludeAttribute : Attribute
{
}

public class SwaggerExcludeSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var properties = context.Type.GetProperties();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute(typeof(SwaggerExcludeAttribute));
            var propertyNameInCamelCasing = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            if (attribute != null)
            {
                schema.Properties.Remove(propertyNameInCamelCasing);
            }
        }
    }
}
```

- [ ] **Step 3: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi/Infrastructure/Helpers/HealthCheckServiceExtensions.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/HealthCheckServiceExtensions.cs
rm Application/EdFi.Ods.AdminApi/Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/FileSystemAppSettingsFileProvider.cs
rm Application/EdFi.Ods.AdminApi/Infrastructure/Helpers/ConstantsHelper.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Helpers/ConstantsHelper.cs
rm Application/EdFi.Ods.AdminApi/Infrastructure/Documentation/ProfileRequestExampleFilter.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Documentation/ProfileRequestExampleFilter.cs
rm Application/EdFi.Ods.AdminApi/Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Documentation/SwaggerOptionalSchemaFilter.cs
rm Application/EdFi.Ods.AdminApi/Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Documentation/SwaggerExcludeSchemaFilter.cs
```

- [ ] **Step 4: Fix consumers via the build-error-driven loop**

Run `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`, add
`using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;` and/or
`using EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;` to every file
the compiler names (expect `WebApplicationBuilderExtensions.cs` in both
projects — it uses `AddHealthCheck`, `FileSystemAppSettingsFileProvider`,
`SwaggerOptionalSchemaFilter`, `SwaggerSchemaRemoveRequiredFilter`,
`SwaggerExcludeSchemaFilter`, `ProfileRequestExampleFilter` — plus any
Program.cs/Startup-equivalent files, and unit tests for these classes).
Rebuild, repeat until clean. Note `WebApplicationBuilderExtensions.cs`
already has `using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;` at line
6 and needs `using EdFi.Ods.AdminApi.Common.Infrastructure.Documentation;`
added (it currently gets these from its own project's `Infrastructure.Helpers`/`Infrastructure.Documentation`
namespaces via a separate using each, which should be removed once nothing
else in the file needs them).

- [ ] **Step 5: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/Helpers Application/EdFi.Ods.AdminApi.Common/Infrastructure/Documentation Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests
git commit -m "Move Infrastructure Helpers and Documentation filters to Common"
```

---

## Task 4: Move SecurityModels to Common

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Security/SecurityModels.cs`
- Delete: `Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityModels.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Security/SecurityModels.cs`

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.Common.Infrastructure.Security.{ApiApplication, ApiScope, ApiAuthorization, ApiToken}`
- **This is a prerequisite for Task 5 (`AdminApiDbContext` unification) in Phase 2** — `AdminApiDbContext.cs` in both v2 and v3 is otherwise identical except for this one dependency.

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityModels.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Security/SecurityModels.cs
```
Expected: namespace line only (plus trailing blank line in v3 copy).

- [ ] **Step 2: Create the file in Common**

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Security/SecurityModels.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using OpenIddict.EntityFrameworkCore.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Security;

public class ApiApplication : OpenIddictEntityFrameworkCoreApplication<int, ApiAuthorization, ApiToken>
{
}

public class ApiScope : OpenIddictEntityFrameworkCoreScope<int>
{
}

public class ApiAuthorization : OpenIddictEntityFrameworkCoreAuthorization<int, ApiApplication, ApiToken>
{
}

public class ApiToken : OpenIddictEntityFrameworkCoreToken<int, ApiApplication, ApiAuthorization>
{
}
```

Check `Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj` already references the `OpenIddict.EntityFrameworkCore` package (`grep -n "OpenIddict.EntityFrameworkCore" Application/EdFi.Ods.AdminApi.Common/EdFi.Ods.AdminApi.Common.csproj`). If it doesn't, add `<PackageReference Include="OpenIddict.EntityFrameworkCore" />` and rebuild — if that reintroduces the `NU1608`/`EFCore.NamingConventions` conflict seen earlier in this cleanup (adding `EdFi.Suite3.Security.DataAccess` triggered it), apply the same fix: add `<PackageReference Include="EFCore.NamingConventions" />` to pin the centrally-managed version (already present in Common's csproj from the earlier Action/AuthorizationStrategy mapper move — check before adding a duplicate entry).

- [ ] **Step 3: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi/Infrastructure/Security/SecurityModels.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Security/SecurityModels.cs
```

- [ ] **Step 4: Fix consumers via the build-error-driven loop**

Run `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`, add
`using EdFi.Ods.AdminApi.Common.Infrastructure.Security;` to every file the
compiler names. Expect `AdminApiDbContext.cs` (both v2 and v3 — it still
exists as separate files until Task 5), `WebApplicationBuilderExtensions.cs`,
`SecurityExtensions.cs`, and their v3 equivalents/unit tests. Rebuild, repeat
until clean.

- [ ] **Step 5: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects.

- [ ] **Step 6: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests
git commit -m "Move SecurityModels to Common"
```

---

## Task 5: Move DBTests/AssertionExtensions.cs and UnitTests/Api/OdsApiValidatorTests.cs

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.DBTests.Common/AssertionExtensions.cs`
- Delete: `Application/EdFi.Ods.AdminApi.DBTests/AssertionExtensions.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3.DBTests/AssertionExtensions.cs`
- Delete: `Application/EdFi.Ods.AdminApi.UnitTests/Api/OdsApiValidatorTests.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3.UnitTests/Api/OdsApiValidatorTests.cs`
- Create: `Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Services/OdsApiValidatorTests.cs`

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.DBTestsShared.AssertionExtensions` (extension methods `ShouldValidate`, `ShouldNotValidate`, `ShouldSatisfy`)

- [ ] **Step 1: Diff to confirm AssertionExtensions.cs is still namespace-only, and that both OdsApiValidatorTests.cs copies are still identical to each other**

```bash
diff Application/EdFi.Ods.AdminApi.DBTests/AssertionExtensions.cs Application/EdFi.Ods.AdminApi.V3.DBTests/AssertionExtensions.cs
diff Application/EdFi.Ods.AdminApi.UnitTests/Api/OdsApiValidatorTests.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Api/OdsApiValidatorTests.cs
```
Expected: namespace line only for both.

- [ ] **Step 2: Create AssertionExtensions.cs in the shared DBTests project**

`Application/EdFi.Ods.AdminApi.DBTests.Common/AssertionExtensions.cs` (note: disk folder is `EdFi.Ods.AdminApi.DBTests.Common`, C# namespace is `EdFi.Ods.AdminApi.DBTestsShared` — this mismatch is deliberate, established in the earlier `PlatformSecurityContextTestBase` move in this cleanup to avoid a namespace collision with `EdFi.Ods.AdminApi.Common`):
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Shouldly;
using static System.Environment;

namespace EdFi.Ods.AdminApi.DBTestsShared;

public static class AssertionExtensions
{
    public static void ShouldValidate<TModel>(this AbstractValidator<TModel> validator, TModel model)
        => validator.Validate(model).ShouldBeSuccessful();

    public static void ShouldNotValidate<TModel>(this AbstractValidator<TModel> validator, TModel model, params string[] expectedErrors)
        => validator.Validate(model).ShouldBeFailure(expectedErrors);

    private static void ShouldBeSuccessful(this ValidationResult result)
    {
        var indentedErrorMessages = result
            .Errors
            .OrderBy(x => x.ErrorMessage)
            .Select(x => "    " + x.ErrorMessage)
            .ToArray();

        var actual = string.Join(NewLine, indentedErrorMessages);

        result.IsValid.ShouldBeTrue($"Expected no validation errors, but found {result.Errors.Count}:{NewLine}{actual}");
    }

    private static void ShouldBeFailure(this ValidationResult result, params string[] expectedErrors)
    {
        result.IsValid.ShouldBeFalse("Expected validation errors, but the message passed validation.");

        result.Errors
            .OrderBy(x => x.ErrorMessage)
            .Select(x => x.ErrorMessage)
            .ToArray()
            .ShouldBe(expectedErrors.OrderBy(x => x).ToArray());
    }

    public static void ShouldSatisfy<T>(this IEnumerable<T> actual, params Action<T>[] itemExpectations)
    {
        var actualItems = actual.ToArray();

        if (actualItems.Length != itemExpectations.Length)
            throw new Exception(
                $"Expected the collection to have {itemExpectations.Length} " +
                $"items, but there were {actualItems.Length} items.");

        for (var i = 0; i < actualItems.Length; i++)
        {
            try
            {
                itemExpectations[i](actualItems[i]);
            }
            catch (Exception failure)
            {
                throw new Exception($"Assertion failed for item at position [{i}].", failure);
            }
        }
    }
}
```

Check `Application/EdFi.Ods.AdminApi.DBTests.Common/EdFi.Ods.AdminApi.DBTests.Common.csproj` references `FluentValidation` and `Shouldly` (`grep -n "PackageReference" Application/EdFi.Ods.AdminApi.DBTests.Common/EdFi.Ods.AdminApi.DBTests.Common.csproj`) — it currently only has `NUnit`, `Respawn`, `EdFi.Suite3.Admin.DataAccess`, `EdFi.Suite3.Security.DataAccess`. Add:
```xml
<PackageReference Include="FluentValidation.AspNetCore" />
<PackageReference Include="Shouldly" />
```

- [ ] **Step 3: Move OdsApiValidatorTests.cs into Common.UnitTests**

Read `Application/EdFi.Ods.AdminApi.UnitTests/Api/OdsApiValidatorTests.cs` in full and create it at
`Application/EdFi.Ods.AdminApi.Common.UnitTests/Infrastructure/Services/OdsApiValidatorTests.cs`
with only the namespace line changed, from:
```csharp
namespace EdFi.Ods.AdminApi.UnitTests.Api;
```
to:
```csharp
namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Services;
```
Everything else in the file is unchanged — it already references
`EdFi.Ods.AdminApi.Common.Infrastructure.Services` (`OdsApiValidator`,
`ISimpleGetRequest`, `OdsApiValidatorResult`), which is why this test
belongs in `Common.UnitTests`, not per-version.

- [ ] **Step 4: Delete the old copies**

```bash
rm Application/EdFi.Ods.AdminApi.DBTests/AssertionExtensions.cs
rm Application/EdFi.Ods.AdminApi.V3.DBTests/AssertionExtensions.cs
rm Application/EdFi.Ods.AdminApi.UnitTests/Api/OdsApiValidatorTests.cs
rm Application/EdFi.Ods.AdminApi.V3.UnitTests/Api/OdsApiValidatorTests.cs
```

- [ ] **Step 5: Fix consumers via the build-error-driven loop**

`AssertionExtensions` is an extension-method class used via `.ShouldValidate(...)`/`.ShouldNotValidate(...)`/`.ShouldSatisfy(...)` syntax, which requires the namespace to be in scope via `using`, not just referenced by type name — the compiler error here will be a missing-method error on the call site, not a missing-type error. Run `dotnet build Application/Ed-Fi-ODS-AdminApi.sln`, and for every DBTests file using these extension methods (`grep -rl "ShouldValidate\|ShouldNotValidate\|ShouldSatisfy" Application/EdFi.Ods.AdminApi.DBTests Application/EdFi.Ods.AdminApi.V3.DBTests --include=*.cs`), add:
```csharp
using EdFi.Ods.AdminApi.DBTestsShared;
```
Rebuild, repeat until clean.

- [ ] **Step 6: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects (the relocated `OdsApiValidatorTests` now run as part of `EdFi.Ods.AdminApi.Common.UnitTests`).

- [ ] **Step 7: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.DBTests.Common Application/EdFi.Ods.AdminApi.DBTests Application/EdFi.Ods.AdminApi.V3.DBTests Application/EdFi.Ods.AdminApi.Common.UnitTests Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests
git commit -m "Move AssertionExtensions to shared DBTests project; relocate OdsApiValidatorTests to Common.UnitTests"
```

---

## Task 6: Phase 1 verification — full build, unit tests, DB tests, Bruno E2E matrix

**Files:** none (verification only)

- [ ] **Step 1: Full solution build**

```bash
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 2: Full unit test suite**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` across `EdFi.Ods.AdminApi.Common.UnitTests`, `EdFi.Ods.AdminApi.InstanceManagement.UnitTests`, `EdFi.Ods.AdminApi.UnitTests`, `EdFi.Ods.AdminApi.V3.UnitTests`.

- [ ] **Step 3: DB tests against SQL Server**

```bash
./eng/run-db-tests.ps1 -Project All -DbEngine mssql -TearDown
```
Expected: all tests pass for both `EdFi.Ods.AdminApi.DBTests` and `EdFi.Ods.AdminApi.V3.DBTests`.

- [ ] **Step 4: Bruno E2E matrix — 8 runs**

```bash
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine pgsql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine pgsql -TenantMode multitenant  -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine mssql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine mssql -TenantMode multitenant  -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine pgsql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine pgsql -TenantMode multitenant  -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine mssql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine mssql -TenantMode multitenant  -TearDown
```
Expected: `✅ All Bruno tests passed!` for each of the 8 runs. If any run fails, treat it as a Phase 1 regression signal — do not proceed to Phase 2 until resolved.

No commit for this task (verification only, no file changes expected). If any step reveals a missed file, fix it as part of whichever Task 1-5 the missed file belongs to and re-run this task's verification from Step 1.

---

## Task 7: Move AdminApiDbContext to Common and collapse the V2/V3 branches in WebApplicationBuilderExtensions.cs

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/AdminApiDbContext.cs`
- Delete: `Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs`
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`
- Modify (usings only, via build-error-driven fix): the ~13 remaining v2 production consumers and ~12 v3 production consumers listed in Step 4, plus their UnitTests counterparts

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext` (constructor `(DbContextOptions<AdminApiDbContext> options, IConfiguration configuration)`, `DbSet<JobStatus> JobStatuses`, `DbSet<EducationOrganization> EducationOrganizations`, `DbSet<OdsInstanceManage> OdsInstanceManages`, `DbSet<AuditLog> AuditLogs`)
- **This is the widest-blast-radius task in this plan.** `AdminApiDbContext` is the live production EF Core context, not test-only. Take the diff-first step seriously.

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs
```
Expected (as of this plan's writing): only the `using EdFi.Ods.AdminApi.Infrastructure.Security;` / `using EdFi.Ods.AdminApi.V3.Infrastructure.Security;` line and the `namespace` line differ (plus trailing blank lines). If it also references anything else version-specific, **stop** — this task assumed Task 4 (SecurityModels move) removed the only real dependency; if that's no longer true, this move is not safe as planned.

- [ ] **Step 2: Create the file in Common**

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/AdminApiDbContext.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

public class AdminApiDbContext(DbContextOptions<AdminApiDbContext> options, IConfiguration configuration) : DbContext(options)
{
    private readonly IConfiguration _configuration = configuration;

    public DbSet<JobStatus> JobStatuses { get; set; }

    public DbSet<EducationOrganization> EducationOrganizations { get; set; }

    public DbSet<OdsInstanceManage> OdsInstanceManages { get; set; }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("adminapi");

        modelBuilder.Entity<ApiApplication>().ToTable("Applications").HasKey(a => a.Id);
        modelBuilder.Entity<ApiScope>().ToTable("Scopes").HasKey(s => s.Id);
        modelBuilder.Entity<ApiAuthorization>().ToTable("Authorizations").HasKey(a => a.Id);
        modelBuilder.Entity<ApiToken>().ToTable("Tokens").HasKey(t => t.Id);
        modelBuilder.Entity<EducationOrganization>().ToTable("EducationOrganizations").HasKey(t => t.Id);
        modelBuilder.Entity<JobStatus>().ToTable("JobStatuses").HasKey(t => t.Id);
        modelBuilder.Entity<OdsInstanceManage>().ToTable("OdsInstanceManages").HasKey(t => t.Id);
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs").HasKey(t => t.Id);
        modelBuilder.Entity<AuditLog>().Property(t => t.EventType).HasConversion<string>();

        var engine = _configuration.Get("AppSettings:DatabaseEngine", "SqlServer");
        modelBuilder.ApplyDatabaseServerSpecificConventions(engine);
    }
}
```

- [ ] **Step 3: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi/Infrastructure/AdminApiDbContext.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/AdminApiDbContext.cs
```

- [ ] **Step 4: Fix production and test consumers via the build-error-driven loop**

Run `dotnet build Application/Ed-Fi-ODS-AdminApi.sln` and add
`using EdFi.Ods.AdminApi.Common.Infrastructure;` to every file the compiler
names (it may already be present in some — check before adding a duplicate).
Confirmed consumers as of this plan's writing (re-verify with
`grep -rl "AdminApiDbContext" Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 --include=*.cs | grep -v obj`
before starting, since files may have changed since):

V2 (excluding `AdminApiDbContext.cs` itself, `AdminApiAuditLogWriter.cs` and `WebApplicationBuilderExtensions.cs` — handled separately below):
- `Features/OdsInstances/Manage/AddOdsInstanceManage.cs`
- `Infrastructure/Database/Commands/AddOdsInstanceManageCommand.cs`
- `Infrastructure/Database/Commands/DeleteOdsInstanceManageCommand.cs`
- `Infrastructure/Database/Queries/GetEducationOrganizationQuery.cs`
- `Infrastructure/Database/Queries/GetOdsInstanceManageByIdQuery.cs`
- `Infrastructure/Database/Queries/GetOdsInstanceManagesQuery.cs`
- `Infrastructure/Security/SecurityExtensions.cs`
- `Infrastructure/Services/EducationOrganizationService/EducationOrganizationService.cs`
- `Infrastructure/Services/Jobs/CreateInstanceJob.cs`
- `Infrastructure/Services/Jobs/CreatePendingOdsInstanceManagesDispatcherJob.cs`
- `Infrastructure/Services/Jobs/DeleteInstanceJob.cs`
- `Infrastructure/Services/Jobs/DeletePendingOdsInstanceManagesDispatcherJob.cs`
- `Infrastructure/Services/Jobs/JobStatusService.cs`
- `Infrastructure/Services/Tenants/TenantSpecificDbContextProvider.cs`

V3 (excluding the same three):
- `Features/DataStores/Manage/AddDataStoreManage.cs`
- `Infrastructure/Database/Commands/AddDataStoreManageCommand.cs`
- `Infrastructure/Database/Commands/DeleteDataStoreManageCommand.cs`
- `Infrastructure/Database/Queries/GetDataStoreManageByIdQuery.cs`
- `Infrastructure/Database/Queries/GetDataStoreManagesQuery.cs`
- `Infrastructure/Database/Queries/GetEducationOrganizationQuery.cs`
- `Infrastructure/Services/EducationOrganizationService/EducationOrganizationService.cs`
- `Infrastructure/Services/Jobs/CreateInstanceJob.cs`
- `Infrastructure/Services/Jobs/CreatePendingDataStoreManagesDispatcherJob.cs`
- `Infrastructure/Services/Jobs/DeleteInstanceJob.cs`
- `Infrastructure/Services/Jobs/DeletePendingDataStoreManagesDispatcherJob.cs`
- `Infrastructure/Services/Jobs/JobStatusService.cs`
- `Infrastructure/Services/Tenants/TenantSpecificDbContextProvider.cs`

UnitTests (both projects, mirroring the production file names under
`Features/OdsInstances/Manage`, `Features/DataStores/Manage`,
`Infrastructure/Database/Commands`, `Infrastructure/Database/Queries`,
`Infrastructure/Services/EducationOrganizationService`,
`Infrastructure/Services/Jobs`, `Infrastructure/Services/Tenants` —
re-run `grep -rl "AdminApiDbContext" Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests --include=*.cs` to get the exact list).

Do NOT fix `WebApplicationBuilderExtensions.cs` or `AdminApiAuditLogWriter.cs` in this loop — they need the specific edits in Steps 5 and 6 below, not just a using statement.

- [ ] **Step 5: Collapse the V2/V3 `AddDbContext<AdminApiDbContext>` branches in `WebApplicationBuilderExtensions.cs`**

Open `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`.

Remove the now-unneeded alias (it existed only to distinguish v2's and v3's previously-separate `AdminApiDbContext` types):
```csharp
using V3AdminApiDbContext = EdFi.Ods.AdminApi.V3.Infrastructure.AdminApiDbContext;
```

In the `case AdminApiMode.V3:` block inside `AddDatabases`, replace both occurrences of `V3AdminApiDbContext` with `AdminApiDbContext` (it now resolves to `EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext` via the `using EdFi.Ods.AdminApi.Common.Infrastructure;` already present at line 12):

Before:
```csharp
case AdminApiMode.V3:
    if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
    {
        webApplicationBuilder.Services.AddDbContext<V3AdminApiDbContext>(
```
After:
```csharp
case AdminApiMode.V3:
    if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
    {
        webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
```
And the same substitution in the `else if (... SqlServer)` branch immediately below it, which currently reads
`webApplicationBuilder.Services.AddDbContext<V3AdminApiDbContext>(` — change to
`webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(`.

Do not otherwise restructure the `switch` statement — the `V2` and `V3` case
blocks now register the identical `AdminApiDbContext` type with identical
`ISecurityContext`/`IUsersContext` registration code (that code was already
using shared `EdFi.Admin.DataAccess`/`EdFi.Security.DataAccess` types, not
version-specific ones), so the two case blocks are now byte-for-byte
identical bodies — but merging `case AdminApiMode.V2: case AdminApiMode.V3:`
into one shared block is an optional further simplification, not required
by this plan. Leave the switch structure as-is to keep this change minimal
and easy to review; a future cleanup can consolidate the cases if desired.

- [ ] **Step 6: Collapse the V2/V3 `AdminApiAuditLogWriter` registration in `WebApplicationBuilderExtensions.cs`**

In the same file, this task also unblocks moving `AdminApiAuditLogWriter` —
but do that move in Task 8, not here. For now, just prepare this file: the
current registration reads
```csharp
if (adminApiMode == AdminApiMode.V3)
{
    webApplicationBuilder.Services.AddSingleton<
        IAuditLogWriter,
        EdFi.Ods.AdminApi.V3.Infrastructure.Audit.AdminApiAuditLogWriter
    >();
}
else
{
    webApplicationBuilder.Services.AddSingleton<IAuditLogWriter, Audit.AdminApiAuditLogWriter>();
}
```
Leave this block untouched in this task — Task 8 replaces it once
`AdminApiAuditLogWriter` itself has moved to Common. Making that edit now,
before the class exists in Common, would not compile.

- [ ] **Step 7: Build and fix any remaining errors**

```bash
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
```
Fix any remaining missing-using errors the same way as Step 4. Repeat until `0 Warning(s) 0 Error(s)`.

- [ ] **Step 8: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects. Pay particular
attention to `EdFi.Ods.AdminApi.UnitTests/Infrastructure/WebApplicationBuilderExtensionsTests.cs`
— it directly tests this file's DI wiring.

- [ ] **Step 9: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/AdminApiDbContext.cs Application/EdFi.Ods.AdminApi Application/EdFi.Ods.AdminApi.V3 Application/EdFi.Ods.AdminApi.UnitTests Application/EdFi.Ods.AdminApi.V3.UnitTests
git commit -m "Move AdminApiDbContext to Common; collapse now-identical V2/V3 DbContext registration"
```

---

## Task 8: Move AdminApiAuditLogWriter to Common

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AdminApiAuditLogWriter.cs`
- Delete: `Application/EdFi.Ods.AdminApi/Infrastructure/Audit/AdminApiAuditLogWriter.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3/Infrastructure/Audit/AdminApiAuditLogWriter.cs`
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs`

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.Common.Infrastructure.Audit.AdminApiAuditLogWriter` (implements `IAuditLogWriter`, constructor `(IConfiguration configuration)`)
- Consumes: `EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext` (from Task 7)

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi/Infrastructure/Audit/AdminApiAuditLogWriter.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/Audit/AdminApiAuditLogWriter.cs
```
Expected: namespace line only.

- [ ] **Step 2: Create the file in Common**

`Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit/AdminApiAuditLogWriter.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AdminApiAuditLogWriter(IConfiguration configuration) : IAuditLogWriter
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var engine = DatabaseEngineEnum.Parse(configuration.Get("AppSettings:DatabaseEngine", "SqlServer"));
        var optionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
        if (engine == DatabaseEngineEnum.PostgreSql)
        {
            optionsBuilder.UseNpgsql(auditEvent.AdminConnectionString);
            optionsBuilder.UseLowerCaseNamingConvention();
        }
        else
        {
            optionsBuilder.UseSqlServer(auditEvent.AdminConnectionString);
        }

        await using var context = new AdminApiDbContext(optionsBuilder.Options, configuration);
        context.AuditLogs.Add(new AuditLog
        {
            EventType = auditEvent.EventType,
            Timestamp = auditEvent.Timestamp,
            ClientId = auditEvent.ClientId,
            SourceIpAddress = auditEvent.SourceIpAddress,
            HttpVerb = auditEvent.HttpVerb,
            HttpUrl = auditEvent.HttpUrl,
            StatusCode = auditEvent.StatusCode
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}
```
Note this file's original `using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;` (for `IAuditLogWriter`/`AuditEvent`) is dropped since the file now lives in that exact namespace — same-namespace types no longer need the using. `DatabaseEngineEnum` and `AdminApiDbContext` are both in `EdFi.Ods.AdminApi.Common.Infrastructure`, already imported.

- [ ] **Step 3: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi/Infrastructure/Audit/AdminApiAuditLogWriter.cs
rm Application/EdFi.Ods.AdminApi.V3/Infrastructure/Audit/AdminApiAuditLogWriter.cs
```

- [ ] **Step 4: Simplify the registration in `WebApplicationBuilderExtensions.cs`**

Replace:
```csharp
if (adminApiMode == AdminApiMode.V3)
{
    webApplicationBuilder.Services.AddSingleton<
        IAuditLogWriter,
        EdFi.Ods.AdminApi.V3.Infrastructure.Audit.AdminApiAuditLogWriter
    >();
}
else
{
    webApplicationBuilder.Services.AddSingleton<IAuditLogWriter, Audit.AdminApiAuditLogWriter>();
}
```
with:
```csharp
webApplicationBuilder.Services.AddSingleton<IAuditLogWriter, AdminApiAuditLogWriter>();
```
This preserves existing behavior: previously V1 and V2 used `Audit.AdminApiAuditLogWriter` (v2's own copy) and V3 used its own byte-identical copy — now all three modes use the single shared class, which is functionally the same code. Remove the `using EdFi.Ods.AdminApi.Infrastructure.Audit;` line from this file if `Audit.AdminApiAuditLogWriter` was its only use (check with `grep -n "Audit\." Application/EdFi.Ods.AdminApi/Infrastructure/WebApplicationBuilderExtensions.cs` after the edit) — `using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;` at line 7 already provides `AdminApiAuditLogWriter` and `IAuditLogWriter`.

- [ ] **Step 5: Build and fix any remaining errors**

```bash
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
```
Fix any remaining missing-using errors. Repeat until `0 Warning(s) 0 Error(s)`.

- [ ] **Step 6: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects.

- [ ] **Step 7: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/Audit Application/EdFi.Ods.AdminApi/Infrastructure Application/EdFi.Ods.AdminApi.V3/Infrastructure
git commit -m "Move AdminApiAuditLogWriter to Common; simplify its DI registration"
```

---

## Task 9: De-static PlatformUsersContextTestBase and move to shared DBTests project

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.DBTests.Common/PlatformUsersContextTestBase.cs`
- Delete: `Application/EdFi.Ods.AdminApi.DBTests/PlatformUsersContextTestBase.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3.DBTests/PlatformUsersContextTestBase.cs`
- Modify: 43 files in `Application/EdFi.Ods.AdminApi.DBTests/Database/{QueryTests,CommandTests}/*.cs` and `Database/EducationOrganizationServiceTests.cs` (one added line each — see Step 4 for the exact list)
- Modify: 43 files in `Application/EdFi.Ods.AdminApi.V3.DBTests/Database/{QueryTests,CommandTests}/*.cs` and `Database/EducationOrganizationServiceTests.cs` (same)
- Leave untouched: `Application/EdFi.Ods.AdminApi.V1.DBTests/PlatformUsersContextTestBase.cs` and its 16 direct subclasses — V1 keeps its own separate copy, not part of this move

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.DBTestsShared.PlatformUsersContextTestBase` (abstract, `[TestFixture]`) with `protected abstract string AdminConnectionString { get; }` and instance methods `Save(params object[])`, `Transaction(Action<IUsersContext>)`, `Transaction<TResult>(Func<IUsersContext, TResult>)`, `GetDbContextOptions()`

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi.DBTests/PlatformUsersContextTestBase.cs Application/EdFi.Ods.AdminApi.V3.DBTests/PlatformUsersContextTestBase.cs
```
Expected: namespace line only (plus trailing blank line in v3 copy).

- [ ] **Step 2: Count direct subclasses to confirm the blast radius hasn't changed**

```bash
grep -rl ": PlatformUsersContextTestBase" Application/EdFi.Ods.AdminApi.DBTests --include=*.cs | grep -v obj | wc -l
grep -rl ": PlatformUsersContextTestBase" Application/EdFi.Ods.AdminApi.V3.DBTests --include=*.cs | grep -v obj | wc -l
```
Expected: `43` for each. If the count differs from this plan's expectation, the file list in Step 4 needs regenerating before proceeding — use
`grep -rl ": PlatformUsersContextTestBase" Application/EdFi.Ods.AdminApi.DBTests Application/EdFi.Ods.AdminApi.V3.DBTests --include=*.cs | grep -v obj`
to get the current authoritative list.

- [ ] **Step 3: Create the de-static'd base class in the shared project**

`Application/EdFi.Ods.AdminApi.DBTests.Common/PlatformUsersContextTestBase.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Respawn;

namespace EdFi.Ods.AdminApi.DBTestsShared;

[TestFixture]
public abstract class PlatformUsersContextTestBase
{
    private readonly Checkpoint _checkpoint = new()
    {
        TablesToIgnore =
        [
            "__MigrationHistory", "DeployJournal", "AdminApiDeployJournal"
        ],
        SchemasToExclude = []
    };

    protected abstract string AdminConnectionString { get; }

    protected string ConnectionString => AdminConnectionString;

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        await _checkpoint.Reset(ConnectionString);
    }

    [SetUp]
    public async Task SetUp()
    {
        await _checkpoint.Reset(ConnectionString);
    }

    protected void Save(params object[] entities)
    {
        Transaction(usersContext =>
        {
            foreach (var entity in entities)
            {
                ((SqlServerUsersContext)usersContext).Add(entity);
            }
        });
    }

    protected void Transaction(Action<IUsersContext> action)
    {
        using var usersContext = new SqlServerUsersContext(GetDbContextOptions());
        using var transaction = (usersContext).Database.BeginTransaction();
        action(usersContext);
        usersContext.SaveChanges();
        transaction.Commit();
    }

    protected TResult Transaction<TResult>(Func<IUsersContext, TResult> query)
    {
        var result = default(TResult);

        Transaction(database =>
        {
            result = query(database);
        });

        return result;
    }

    protected DbContextOptions GetDbContextOptions()
    {
        var builder = new DbContextOptionsBuilder();
        builder.UseSqlServer(ConnectionString);
        return builder.Options;
    }
}
```
Every member has `static` removed compared to the v2/v3 originals — this is required because `AdminConnectionString` is an abstract instance member, and C# does not allow a static member to reference (or be overridden alongside) an instance abstract member in the same inheritance chain in the way this class needs.

- [ ] **Step 4: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi.DBTests/PlatformUsersContextTestBase.cs
rm Application/EdFi.Ods.AdminApi.V3.DBTests/PlatformUsersContextTestBase.cs
```

- [ ] **Step 5: Add the override to every direct-subclass test-fixture file**

For every file found in Step 2 (43 in `EdFi.Ods.AdminApi.DBTests`, 43 in `EdFi.Ods.AdminApi.V3.DBTests`):
1. Add `using EdFi.Ods.AdminApi.DBTestsShared;` to the using block.
2. Immediately inside the class body (right after the opening `{` of the `class ... : PlatformUsersContextTestBase` declaration), add:
```csharp
    protected override string AdminConnectionString => Testing.AdminConnectionString;
```

For example, `Application/EdFi.Ods.AdminApi.DBTests/Database/QueryTests/GetVendorByIdQueryTests.cs` goes from:
```csharp
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetVendorByIdQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetVendorById()
```
to:
```csharp
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.DBTestsShared;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetVendorByIdQueryTests : PlatformUsersContextTestBase
{
    protected override string AdminConnectionString => Testing.AdminConnectionString;

    [Test]
    public void ShouldGetVendorById()
```
This pattern is identical for all 86 files — only the class name and the rest of the file's body differ. `Testing` resolves without an extra `using` because it's a sibling type in the same project namespace (`EdFi.Ods.AdminApi.DBTests`/`EdFi.Ods.AdminApi.V3.DBTests`), same as it already does in `SecurityDataTestBase.cs` from the earlier `PlatformSecurityContextTestBase` move in this cleanup.

Apply this to all 43 files in `EdFi.Ods.AdminApi.DBTests` (get the exact list from Step 2's grep) and all 43 in `EdFi.Ods.AdminApi.V3.DBTests`.

- [ ] **Step 6: Build and fix any remaining errors**

```bash
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
```
A file missed in Step 5 fails with `CS0534` ("does not implement inherited abstract member 'PlatformUsersContextTestBase.AdminConnectionString.get'") naming the exact class — add the override line to that file and rebuild. Repeat until `0 Warning(s) 0 Error(s)`.

- [ ] **Step 7: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects (this class isn't used by any of them directly, but confirms nothing else broke).

- [ ] **Step 8: Run DB tests against SQL Server**

```bash
./eng/run-db-tests.ps1 -Project All -DbEngine mssql -TearDown
```
Expected: all tests pass — this is the step that actually proves `Save`/`Transaction` still checkpoint and commit correctly as instance methods against a real database.

- [ ] **Step 9: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.DBTests.Common Application/EdFi.Ods.AdminApi.DBTests Application/EdFi.Ods.AdminApi.V3.DBTests
git commit -m "De-static PlatformUsersContextTestBase and move to shared DBTests project"
```

---

## Task 10: De-static AdminApiDbContextTestBase and move to shared DBTests project

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.DBTests.Common/AdminApiDbContextTestBase.cs`
- Delete: `Application/EdFi.Ods.AdminApi.DBTests/AdminApiDbContextTestBase.cs`
- Delete: `Application/EdFi.Ods.AdminApi.V3.DBTests/AdminApiDbContextTestBase.cs`
- Modify: 7 files in `Application/EdFi.Ods.AdminApi.DBTests/**/*.cs` (see Step 4)
- Modify: 7 files in `Application/EdFi.Ods.AdminApi.V3.DBTests/**/*.cs` (same)

**Interfaces:**
- Produces: `EdFi.Ods.AdminApi.DBTestsShared.AdminApiDbContextTestBase` (abstract, `[TestFixture]`) with `protected abstract string AdminConnectionString { get; }`, `protected abstract IConfiguration Configuration { get; }`, and instance methods `Save`, four `Transaction` overloads, `GetAdminApiDbContextOptions`
- Consumes: `EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext` (from Task 7)

- [ ] **Step 1: Diff to confirm still namespace-only**

```bash
diff Application/EdFi.Ods.AdminApi.DBTests/AdminApiDbContextTestBase.cs Application/EdFi.Ods.AdminApi.V3.DBTests/AdminApiDbContextTestBase.cs
```
Expected: the `using EdFi.Ods.AdminApi.Infrastructure;` / `using EdFi.Ods.AdminApi.V3.Infrastructure;` line and the `namespace` line differ (plus trailing blank lines) — this dependency is exactly what Task 7 removed by moving `AdminApiDbContext` to Common. If it also differs elsewhere, stop and re-investigate before proceeding.

- [ ] **Step 2: Count direct subclasses to confirm the blast radius hasn't changed**

```bash
grep -rl ": AdminApiDbContextTestBase" Application/EdFi.Ods.AdminApi.DBTests --include=*.cs | grep -v obj
grep -rl ": AdminApiDbContextTestBase" Application/EdFi.Ods.AdminApi.V3.DBTests --include=*.cs | grep -v obj
```
Expected: 7 files each. As of this plan's writing:

V2:
- `Database/CommandTests/AddOdsInstanceManageCommandTests.cs`
- `Database/CommandTests/DeleteOdsInstanceManageCommandTests.cs`
- `Database/QueryTests/GetOdsInstanceManageByIdQueryTests.cs`
- `Database/QueryTests/GetOdsInstanceManagesQueryTests.cs`
- `Database/QueryTests/GetTenantEdOrgsByInstancesTests.cs`
- `Infrastructure/Audit/AdminApiAuditLogWriterTests.cs`
- `Services/Jobs/JobStatusServiceTests.cs`

V3:
- `Database/CommandTests/AddDataStoreManageCommandTests.cs`
- `Database/CommandTests/DeleteDataStoreManageCommandTests.cs`
- `Database/QueryTests/GetDataStoreManageByIdQueryTests.cs`
- `Database/QueryTests/GetDataStoreManagesQueryTests.cs`
- `Database/QueryTests/GetTenantEdOrgsByDataStoresTests.cs`
- `Infrastructure/Audit/AdminApiAuditLogWriterTests.cs`
- `Services/Jobs/JobStatusServiceTests.cs`

None of these 14 files also extend `PlatformUsersContextTestBase` — confirmed no overlap with Task 9's file list, so there's no dual-base-class conflict to resolve. `Infrastructure/Audit/AdminApiAuditLogWriterTests.cs` here is a DBTests file exercising `AdminApiAuditLogWriter` against a real database (distinct from the `EdFi.Ods.AdminApi.UnitTests`/`.V3.UnitTests` `Api/OdsApiValidatorTests.cs`-style mocked unit tests) — expect it to keep passing since Task 8 didn't change `AdminApiAuditLogWriter`'s behavior, only its location.

- [ ] **Step 3: Create the de-static'd base class in the shared project**

`Application/EdFi.Ods.AdminApi.DBTests.Common/AdminApiDbContextTestBase.cs`:
```csharp
// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Respawn;

namespace EdFi.Ods.AdminApi.DBTestsShared;

[TestFixture]
public abstract class AdminApiDbContextTestBase
{
    private readonly Checkpoint _checkpoint = new()
    {
        TablesToIgnore =
        [
            "__MigrationHistory", "DeployJournal", "AdminApiDeployJournal"
        ],
        SchemasToExclude = []
    };

    protected abstract string AdminConnectionString { get; }

    protected abstract IConfiguration Configuration { get; }

    protected string ConnectionString => AdminConnectionString;

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        await _checkpoint.Reset(ConnectionString);
    }

    [SetUp]
    public async Task SetUp()
    {
        await _checkpoint.Reset(ConnectionString);
    }

    protected void Save(params object[] entities)
    {
        Transaction(context =>
        {
            foreach (var entity in entities)
            {
                context.Add(entity);
            }
        });
    }

    protected void Transaction(System.Action<AdminApiDbContext> action)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Configuration);
        using var transaction = context.Database.BeginTransaction();
        action(context);
        context.SaveChanges();
        transaction.Commit();
    }

    protected TResult Transaction<TResult>(System.Func<AdminApiDbContext, TResult> query)
    {
        var result = default(TResult);
        Transaction(database =>
        {
            result = query(database);
        });
        return result;
    }

    protected async Task Transaction(System.Func<AdminApiDbContext, Task> action)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Configuration);
        using var transaction = await context.Database.BeginTransactionAsync();
        await action(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    protected async Task<TResult> Transaction<TResult>(System.Func<AdminApiDbContext, Task<TResult>> query)
    {
        using var context = new AdminApiDbContext(
            GetAdminApiDbContextOptions(ConnectionString),
            Configuration);
        using var transaction = await context.Database.BeginTransactionAsync();
        var result = await query(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    protected DbContextOptions<AdminApiDbContext> GetAdminApiDbContextOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AdminApiDbContext>();
        builder.UseSqlServer(connectionString);
        return builder.Options;
    }
}
```
Note `GetAdminApiDbContextOptions` changed from `public static` to `protected` (instance) — check Step 2's file list plus a solution-wide search
(`grep -rn "AdminApiDbContextTestBase.GetAdminApiDbContextOptions\|\.GetAdminApiDbContextOptions(" Application --include=*.cs`)
for any external caller invoking it as a static method (`AdminApiDbContextTestBase.GetAdminApiDbContextOptions(...)`) rather than as an inherited instance member — if one exists, it needs updating to call it on an instance instead, since the static form no longer exists.

- [ ] **Step 4: Delete the v2 and v3 copies**

```bash
rm Application/EdFi.Ods.AdminApi.DBTests/AdminApiDbContextTestBase.cs
rm Application/EdFi.Ods.AdminApi.V3.DBTests/AdminApiDbContextTestBase.cs
```

- [ ] **Step 5: Add the two-hook override to every direct-subclass file**

For each of the 7+7 files from Step 2:
1. Add `using EdFi.Ods.AdminApi.DBTestsShared;` to the using block.
2. Immediately inside the class body, add both overrides:
```csharp
    protected override string AdminConnectionString => Testing.AdminConnectionString;

    protected override IConfiguration Configuration => Testing.Configuration();
```

- [ ] **Step 6: Build and fix any remaining errors**

```bash
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
```
A missed file fails with `CS0534` naming both unimplemented abstract members if both overrides are missing, or just one if only one was added — add whichever is missing and rebuild. Repeat until `0 Warning(s) 0 Error(s)`.

- [ ] **Step 7: Run unit tests**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` in all four unit test projects.

- [ ] **Step 8: Run DB tests against SQL Server**

```bash
./eng/run-db-tests.ps1 -Project All -DbEngine mssql -TearDown
```
Expected: all tests pass.

- [ ] **Step 9: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.DBTests.Common Application/EdFi.Ods.AdminApi.DBTests Application/EdFi.Ods.AdminApi.V3.DBTests
git commit -m "De-static AdminApiDbContextTestBase and move to shared DBTests project"
```

---

## Task 11: Phase 2 verification — full build, unit tests, DB tests, Bruno E2E matrix

**Files:** none (verification only)

- [ ] **Step 1: Full solution build**

```bash
cd Application
dotnet build Ed-Fi-ODS-AdminApi.sln
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 2: Full unit test suite**

```bash
./build.ps1 -Command UnitTest -NoBuild
```
Expected: `Failed: 0` across all four unit test projects.

- [ ] **Step 3: DB tests against SQL Server**

```bash
./eng/run-db-tests.ps1 -Project All -DbEngine mssql -TearDown
```
Expected: all tests pass for both `EdFi.Ods.AdminApi.DBTests` and `EdFi.Ods.AdminApi.V3.DBTests`.

- [ ] **Step 4: Bruno E2E matrix — 8 runs**

This phase moved production code (`AdminApiDbContext`, `AdminApiAuditLogWriter`, and the DI registration in `WebApplicationBuilderExtensions.cs`) — re-run the full matrix, not a subset:
```bash
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine pgsql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine pgsql -TenantMode multitenant  -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine mssql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 2 -DbEngine mssql -TenantMode multitenant  -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine pgsql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine pgsql -TenantMode multitenant  -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine mssql -TenantMode singletenant -TearDown
./eng/run-bruno-e2e.ps1 -ApiVersion 3 -DbEngine mssql -TenantMode multitenant  -TearDown
```
Expected: `✅ All Bruno tests passed!` for each of the 8 runs. Pay particular
attention to any test that exercises audit logging, health checks, job
status, or education-organization sync endpoints — those are the
Bruno-covered surfaces of the code moved in Tasks 7 and 8.

No commit for this task (verification only). If any step fails, treat it as this phase's regression signal — trace it back to whichever of Tasks 7-10 the failing area belongs to before considering the plan complete.
