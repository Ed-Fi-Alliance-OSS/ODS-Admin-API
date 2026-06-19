# Design: db-ods Dual Template Databases (Minimal + Populated)

**Date:** 2026-06-18  
**Scope:** `Docker/Settings/V2/DB-Ods/mssql/` and `Docker/V2/Compose/mssql/SingleTenant/compose-build-ods.yml`

---

## Summary

The `db-ods` container currently restores a single minimal ODS template database at startup. This design extends it to restore **two** template databases instead:

| Database Name | Source |
|---|---|
| `Ods_Minimal_Template` | Minimal template NuGet package (renamed from current `EdFi_Ods` restore) |
| `Ods_Populated_Template` | New populated template NuGet package |

Both databases respect `TPDM_ENABLED`: when `true`, the TPDM Core variant is used for each.

---

## Files Changed

| File | Change |
|---|---|
| `Docker/Settings/V2/DB-Ods/mssql/Dockerfile` | Add populated template ARGs + download steps |
| `Docker/Settings/V2/DB-Ods/mssql/setup-db.sh` | Restore both template databases with new names |
| `Docker/V2/Compose/mssql/SingleTenant/compose-build-ods.yml` | Point build context to `V2/` folder |

The `shared/` folder is left unchanged; V2-specific logic lives in `V2/`.

---

## Architecture

### Build Phase (Dockerfile)

Four `.bak` files are downloaded and extracted into `/app/backups/` at image build time — all four variants are always included so `TPDM_ENABLED` can remain a runtime variable:

| File | Source Package |
|---|---|
| `EdFi_Ods_Minimal_Template.bak` | `EdFi.Suite3.Ods.Minimal.Template.Standard.${STANDARD_VERSION}` |
| `EdFi_Ods_Minimal_Template_TPDM_Core.bak` | `EdFi.Suite3.Ods.Minimal.Template.TPDM.Core.${EXTENSION_VERSION}.Standard.${STANDARD_VERSION}` |
| `EdFi_Ods_Populated_Template.bak` | `EdFi.Suite3.Ods.Populated.Template.Standard.${STANDARD_VERSION}` |
| `EdFi_Ods_Populated_Template_TPDM_Core.bak` | `EdFi.Suite3.Ods.Populated.Template.TPDM.Core.${EXTENSION_VERSION}.Standard.${STANDARD_VERSION}` |

New `ARG` declarations (same `ODS_VERSION`, `TPDM_VERSION`, `STANDARD_VERSION`, `EXTENSION_VERSION`):

```dockerfile
ARG POPULATED_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Populated.Template.Standard.${STANDARD_VERSION}/versions/${ODS_VERSION}/content
ARG TPDM_POPULATED_URL=https://pkgs.dev.azure.com/ed-fi-alliance/Ed-Fi-Alliance-OSS/_apis/packaging/feeds/EdFi/nuget/packages/EdFi.Suite3.Ods.Populated.Template.TPDM.Core.${EXTENSION_VERSION}.Standard.${STANDARD_VERSION}/versions/${TPDM_VERSION}/content
```

The `.bak` filenames inside the populated ZIPs are assumed to follow the naming convention `EdFi.Ods.Populated.Template.bak` and `EdFi.Ods.Populated.Template.TPDM.Core.bak`. These must be verified against the actual NuGet packages during implementation.

### Runtime Phase (setup-db.sh)

The script selects the correct backup pair based on `TPDM_ENABLED`:

```
TPDM_ENABLED=false → MINIMAL_BACKUP=EdFi_Ods_Minimal_Template.bak
                      POPULATED_BACKUP=EdFi_Ods_Populated_Template.bak

TPDM_ENABLED=true  → MINIMAL_BACKUP=EdFi_Ods_Minimal_Template_TPDM_Core.bak
                      POPULATED_BACKUP=EdFi_Ods_Populated_Template_TPDM_Core.bak
```

A reusable `restore_database` shell function handles each restore:

```bash
restore_database <db_name> <backup_file> <mdf_path> <ldf_path>
```

Inside the function:
1. Run `RESTORE FILELISTONLY` against the backup file to extract logical file names dynamically (avoids hard-coding internal names that may vary across package versions).
2. Run `RESTORE DATABASE [<db_name>]` with the discovered `MOVE` clauses.

**Idempotency guard:** Each restore is skipped if the target `.mdf` file already exists (prevents duplicate restores on container restart):

- Skip `Ods_Minimal_Template` if `/var/opt/mssql/data/Ods_Minimal_Template.mdf` exists.
- Skip `Ods_Populated_Template` if `/var/opt/mssql/data/Ods_Populated_Template.mdf` exists.

The two restores run sequentially within the same script execution.

### Compose Change

```yaml
# Before
context: ../../../../Settings/shared/DB-Ods/mssql/

# After
context: ../../../../Settings/V2/DB-Ods/mssql/
```

No version args change — `ODS_VERSION`, `TPDM_VERSION`, `STANDARD_VERSION`, and `EXTENSION_VERSION` remain the same.

---

## Error Handling

- `set -e` is already active in `setup-db.sh`; any failed SQL command will abort the script.
- Failed downloads in the Dockerfile will cause the build to fail immediately (existing behavior via `wget` non-zero exit).
- The dynamic `RESTORE FILELISTONLY` parse must produce non-empty logical names; if parsing fails, the subsequent `RESTORE DATABASE` will fail with a clear SQL error.

---

## Testing

Manual verification steps after implementation:
1. Build the image: `docker compose -f compose-build-ods.yml build db-ods`
2. Start the container and wait for the healthcheck to pass.
3. Connect to SQL Server and verify both databases exist with the correct names:
   ```sql
   SELECT name FROM sys.databases WHERE name IN ('Ods_Minimal_Template', 'Ods_Populated_Template');
   ```
4. Restart the container and confirm both restores are skipped (idempotency).
5. Repeat with `TPDM_ENABLED=true` to verify the TPDM variants are used.
