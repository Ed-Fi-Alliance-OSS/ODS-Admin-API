# Docker files details

## Development environment

1. `dev.pgsql.Dockerfile`

    The purpose of this file is to facilitate the setup of Admin API docker image in
    the development environment, allowing for local testing with latest changes using Postgresql.
    It utilizes the assets and dlls from "Docker\Application\EdFi.Ods.AdminApi"
    folder.

2. `dev.pgsql.Dockerfile`

    The purpose of this file is to facilitate the setup of Admin API docker image in
    the development environment, allowing for local testing with latest changes using MSSQL.
    It utilizes the assets and dlls from "Docker\Application\EdFi.Ods.AdminApi"
    folder.

3. `db.pgsql.admin.Dockerfile`

    Purpose of this file to setup the EdFi_Admin database image which includes Admin
    API specific tables. It utilizes database artifacts located at
    "Docker\Application\EdFi.Ods.AdminApi\Artifacts\PgSql\Structure\Admin".

## Non-development environments

1. `api.pgsql.Dockerfile`

    File for setting up Admin API and Postgres docker image with assets and dlls sourced from
    "EdFi.Suite3.ODS.AdminApi" nuget package(from
    <https://pkgs.dev.azure.com/ed-fi-alliance>)

2. `api.mssql.Dockerfile`

    File for setting up Admin API and MSSQL docker image with assets and dlls sourced from
    "EdFi.Suite3.ODS.AdminApi" nuget package(from
    <https://pkgs.dev.azure.com/ed-fi-alliance>)

3. `\Docker\Settings\DB-Admin\pgsql\Dockerfile`

    This file to setup the EdFi_Admin database image which includes Admin API
    specific tables. The database artifacts will be downloaded from
    "EdFi.Suite3.ODS.AdminApi" nuget package(from
    <https://pkgs.dev.azure.com/ed-fi-alliance>).

For detailed instructions on setting up docker containers, please refer
[docker.md](../docs/docker.md).
