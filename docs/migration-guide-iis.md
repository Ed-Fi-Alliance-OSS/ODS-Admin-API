# Migrate from previous versions (IIS version)

To update the IIS site you must need the latest version of the binaries published.

You can find the preparation steps to publish the binaries in the following [Migration Guide](./migration-guide.md)

## Upgrading to this release

This release adds the column `ApiClients.InheritsApplicationEducationOrganizations` to the `EdFi_Admin` database (migration `00010`, `00010-AddInheritsApplicationEducationOrganizationsToApiClients.sql`, for both SQL Server and PostgreSQL). Fresh installs and new Docker volumes get it automatically. Existing databases do not.

Before deploying the new binaries:

1. Apply `00010` to **every** `EdFi_Admin` database. In a multi-tenant installation, that means each tenant's `EdFi_Admin` database. For example, run `eng/run-dbup-migrations.ps1` against each one, or run the script under `Application/EdFi.Ods.AdminApi/Artifacts/<MsSql|PgSql>/Structure/Admin/` yourself.
2. Then deploy the new Admin API.

Admin API does not detect or tolerate a missing column. Until the column exists, every API client and Application endpoint fails.

## Update AdminApi (IIS)

Open Powershell as an Admin to update the files.
Replace the source and destination vars to use your structure.

Declare the variables

```bash
$publishFolderPath = "C:\PublishFolder"
$virtualFolderPath = "C:\inetpub\wwwroot\YourVirtualFolder"
```

### Step 1: Remove dll files from the virtual folder

Go to the iis directory for the AdminApi site

Create a backup of the folder.

Remove all the dll files from the virtual folder

```bash
Get-ChildItem -Path $virtualFolderPath -File -Recurse | Where-Object { $_.Name -notmatch '\.sh$|\.config$|appsettings.*\.json$' } | Remove-Item
```

### Step 2: Copy binaries to virtual directory

```bash


Get-ChildItem -Path $publishFolderPath -File -Recurse | Where-Object { $_.Name -notmatch 'appsettings.*\.json$|\.config$' } | ForEach-Object { $destPath = $_.FullName.Replace($publishFolderPath, $virtualFolderPath); $destDir = [System.IO.Path]::GetDirectoryName($destPath); if (-not (Test-Path -Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force }; Copy-Item -Path $_.FullName -Destination $destPath }
```

### Step 3: Edit appsettings.json file

Using a text editor add the following lines.

For the AppSettings section add the parameter

```
"PreventDuplicateApplications": "false"
```

After the AllowedHosts parameter, add the following  section

```
"IpRateLimiting": {
        "EnableEndpointRateLimiting": true,
        "StackBlockedRequests": false,
        "RealIpHeader": "X-Real-IP",
        "ClientIdHeader": "X-ClientId",
        "HttpStatusCode": 429,
        "IpWhitelist": [],
        "EndpointWhitelist": [],
        "GeneralRules": [
            {
                "Endpoint": "POST:/Connect/Register",
                "Period": "1m",
                "Limit": 3
            }
        ]
    }
```

### Step 4: Update permissions

In some cases it is necessary to update the permissions of the binaries to be executed by the IIS.

```bash showLineNumbers
$userName = "IIS AppPool\DefaultAppPool"  # Change this to your application pool identity

# Set File System Permissions
$acl = Get-Acl $virtualFolderPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($userName, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl $virtualFolderPath $acl
```

### Step 5: Restart IIS

To apply the changes you should restart the IIS service or the service Pool.

You can reset the IIS service. This process will affect the rest io applications.

```
iisreset
```

Or you can reset the IIS AppPool related to the site.

```
Restart-WebAppPool -Name "AdminApiAppPool"
```
