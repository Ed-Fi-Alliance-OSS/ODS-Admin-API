
# EF Core Migration and Database Update Commands

This document provides step-by-step guidance for generating and applying migrations in EF Core. Commands for both Command Line (`cmd`) and Package Manager Console (`pmc`) are included.

### 1. Add a Migration

#### Command Line (`cmd`)

Use the following command to add a migration in the command line:

```bash
dotnet ef migrations add <MigrationName> --context <ContextName> --output-dir Infrastructure/DataAccess/Artifacts/<DbProvider> --project EdFi.Ods.AdminApi.AdminConsole
```

#### Package Manager Console (`pmc`)

In the Package Manager Console within Visual Studio, run:

```powershell
Add-Migration <MigrationName> -Context <ContextName> -Project EdFi.Ods.AdminApi.AdminConsole -OutputDir Infrastructure/DataAccess/Artifacts/<Database>/<DbProvider>
```

- `MigrationName`: Name of the migration (e.g., `InitialCreate`).
- `ContextName`: Name of the context (options: `AdminConsolePgSqlContext`,`AdminConsoleMsSqlContext`)
- `Database`: Name of the context (options: `Admin`,`Security`)
- `DbProvider`: The database provider, this will create a folder or add the migration in the specific db provider (options: `MsSql`,`PgSql` ).

---

### 2. Update the Database

#### Command Line (`cmd`)

To update the database using the command line, run:

```bash
dotnet ef database update --context <ContextName> --project EdFi.Ods.AdminApi.AdminConsole
```

#### Package Manager Console (`pmc`)

For updating the database from the Package Manager Console, use:

```powershell
Update-Database -Context <ContextName> -Project EdFi.Ods.AdminApi.AdminConsole
```
- `ContextName`: Name of the context (options: `AdminConsolePgSqlContext`,`AdminConsoleMsSqlContext`)
---

### Summary

| Action              | Command (cmd)                                                                                           | Command (pmc)                                                                                  |
|---------------------|--------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| **Add Migration**   | `dotnet ef migrations add <MigrationName> --context <ContextName> --output-dir Infrastructure/DataAccess/Artifacts/<Database>/<DbProvider> --project EdFi.Ods.AdminApi.AdminConsole` | `Add-Migration <MigrationName> -Context <ContextName> -Project EdFi.Ods.AdminApi.AdminConsole -OutputDir Infrastructure/DataAccess/Artifacts/<Database>/<DbProvider>` |
| **Update Database** | `dotnet ef database update --context <ContextName> --project EdFi.Ods.AdminApi.AdminConsole`                               | `Update-Database -Context <ContextName> -Project EdFi.Ods.AdminApi.AdminConsole`                                  |

---

>**Note:** Before running any commands, verify that the `appsettings.json` file has the right `DatabaseEngine` and the corresponding `connection string` correctly configured.

> **Note:** Before running commands in cmd, make sure to navigate to the root folder of your project by using:
> 
> ```bash
> cd path/to/your/project
> ```
