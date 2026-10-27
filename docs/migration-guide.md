# Migrate from previous versions

To migrate from a previous version (2.2.0) to a new version (2.2.1) you need to replace the old version binaries with the new ones.

The steps to migrate to the new version are shown for both an installation in a docker container and an installation in IIS.

## Upgrading to this release

This release adds the column `ApiClients.InheritsApplicationEducationOrganizations` to the `EdFi_Admin` database (migration `00010`, `00010-AddInheritsApplicationEducationOrganizationsToApiClients.sql`, for both SQL Server and PostgreSQL). Fresh installs and new Docker volumes get it automatically. Existing databases do not.

Before deploying the new binaries:

1. Apply `00010` to **every** `EdFi_Admin` database. In a multi-tenant installation, that means each tenant's `EdFi_Admin` database. For example, run `eng/run-dbup-migrations.ps1` against each one, or run the script under `Application/EdFi.Ods.AdminApi/Artifacts/<MsSql|PgSql>/Structure/Admin/` yourself.
2. Then deploy the new Admin API.

The published Docker database images run the Admin API scripts only the first time a volume is initialized, so existing Docker volumes do not pick up `00010` on their own. Admin API does not detect or tolerate a missing column. Until the column exists, every API client and Application endpoint fails.

## Building the new version

### Step 1: Download the latest version

First, download the latest version and extract the contents of the sources directory to a directory to build the new version binaries.

### Step 2: Build and publish the new version binaries

Go to the root directory and execute the build and publish command to generate the new binaries.

```bash
.\build.ps1 BuildAndPublish       
```

### Step 3: Update the binaries for your installation

You can find the steps below to update the binaries for a Docker Container or a IIS installation

- [Docker Container](./migration-guide-docker.md)
- [IIS Installation](./migration-guide-iis.md)
