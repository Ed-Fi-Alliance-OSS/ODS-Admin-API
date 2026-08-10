# DataStoreDerivative Connection String Encryption — Design

**Ticket:** [ADMINAPI-1482](https://edfi.atlassian.net/browse/ADMINAPI-1482)
**Scope:** V3 only (`Application/EdFi.Ods.AdminApi.V3`). There is no `DataStoreDerivative` concept in V2.

## Problem

`DataStoreDerivative.ConnectionString` is stored unencrypted and returned in plaintext by the Admin API, including any embedded database password. The primary `DataStore` resource already handles this correctly — encrypting on write via `ISymmetricStringEncryptionProvider` and never exposing `ConnectionString` in any response model. `DataStoreDerivative` does neither, and the plaintext leaks through three surfaces that all share the same response model/mapper:

- `GET /v3/dataStoreDerivatives` (list)
- `GET /v3/dataStoreDerivatives/{id}`
- `GET /v3/dataStores/{id}` (detail — embeds derivatives via `DataStoreDetailModel.DataStoreDerivatives`)

## Goals (Acceptance Criteria, from the ticket)

1. The three GET surfaces above never include a `connectionString` field for any derivative, in any form (plaintext or encrypted).
2. POST/PUT continue to accept `connectionString` on write and now encrypt it before persisting.
3. Existing plaintext-stored derivative connection strings get encrypted (or a documented follow-up covers this) — no permanently-unencrypted legacy rows.
4. `ConnectionStringHelper.ValidateConnectionString` continues to run before encryption.

## Existing precedent (primary DataStore)

- `AddDataStore.Handle` / `EditDataStore.Handle` inject `ISymmetricStringEncryptionProvider` + `IOptions<AppSettings>` and call `encryptionProvider.Encrypt(request.ConnectionString, key)` before invoking the low-level command. The commands themselves never encrypt anything — persistence-only.
- `DataStoreModel`/`DataStoreDetailModel` have no `ConnectionString` property at all.
- `DataStoreEncryptionHelper.EncryptConnectionStringsIfNeededAsync` opportunistically re-encrypts any legacy plaintext `OdsInstance.ConnectionString` it encounters on read (skips if empty, already-encrypted, or fails `ConnectionStringHelper.ValidateConnectionString`), called from `GetDataStoreQuery`/`GetDataStoresQuery`.

This design replicates that exact pattern for `DataStoreDerivative`, reusing the same `ISymmetricStringEncryptionProvider` singleton — no new abstraction needed.

## Design

### A. Write path — encrypt on create/update

- **`AddDataStoreDerivative.Handle`**: inject `ISymmetricStringEncryptionProvider` and `IOptions<AppSettings>`. Encrypt `request.ConnectionString` before calling `addDataStoreDerivativeCommand.Execute(request)`. Throw `InvalidOperationException` if `EncryptionKey` is missing, matching `AddDataStore.Handle`.
- **`EditDataStoreDerivative.Handle`**: same injection and encrypt-before-`Execute` call. The derivative's `ConnectionString` is validator-required (`NotEmpty()`), unlike the primary `EditDataStore`'s optional one — so there is no "leave empty" branch to port; always encrypt.
- `AddDataStoreDerivativeCommand` / `EditDataStoreDerivativeCommand` are unchanged — they persist whatever string they're given.
- AC4 (validate-before-encrypt) falls out of the existing structure: `validator.GuardAsync(request)` (which runs `ConnectionStringHelper.ValidateConnectionString` via `BeAValidConnectionString`) already executes before the handler body that will do the encrypting.

### B. Read path — never return it, encrypt legacy rows lazily

**Never return (AC1):**
- Remove `ConnectionString` from `DataStoreDerivativeModel` entirely (not left as a nulled-out property) — matches how the primary `DataStoreModel` has no such property.
- Remove the corresponding line from `DataStoreDerivativeMapper.ToModel`.
- Because all three leak surfaces share this one model/mapper, this single change fixes all three at once.

**Encrypt legacy plaintext at rest (AC3), via lazy backfill on read:**
- Add `DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(List<OdsInstanceDerivative> derivatives, IUsersContext, ISymmetricStringEncryptionProvider, string encryptionKey, string databaseEngine, CancellationToken = default)` — identical logic to the existing method, typed for `OdsInstanceDerivative`.
- Call it from every query that loads `OdsInstanceDerivative` entities:
  - `GetDataStoreQuery.Execute` (detail) — already does `.Include(p => p.OdsInstanceDerivatives)` and already calls the sibling helper for the primary connection string; add the derivative call alongside it, under the same `EncryptionKey`/`DatabaseEngine` guard.
  - `GetDataStoreDerivativesQuery` — add `ISymmetricStringEncryptionProvider` to its constructor (currently takes only `IUsersContext`/`IOptions<AppSettings>`); call the helper from both `Execute()` overloads.
  - `GetDataStoreDerivativeByIdQuery` — add both `ISymmetricStringEncryptionProvider` and `IOptions<AppSettings>` to its constructor (currently takes neither); call the helper from `Execute`.
- Convergence matches the existing primary-DataStore behavior: a row stays plaintext at rest until next read through one of these three paths, then is transparently re-encrypted and saved. No separate migration script — this is the intentional trade-off per the "lazy backfill" decision below.

### C. Error handling

- Write path: missing `EncryptionKey` → `InvalidOperationException`, same as today's `DataStore` behavior.
- Read-path backfill: guarded by `!string.IsNullOrEmpty(EncryptionKey) && !string.IsNullOrEmpty(DatabaseEngine)`; silently skips backfill if either is absent (never throws for this reason) — same as `GetDataStoreQuery` today.
- Backfill never encrypts a value that fails `ConnectionStringHelper.ValidateConnectionString` — malformed legacy data is left untouched rather than corrupted.

## Decisions made during brainstorming

- **Scope: V3 only.** V2 has no `DataStoreDerivative` concept.
- **Migration strategy for AC3: lazy backfill on read**, reusing the existing `DataStoreEncryptionHelper` pattern rather than adding a one-time DB migration script. Chosen because DbUp migration scripts are plain SQL with no access to the encryption key/provider, and the codebase already has working precedent for this exact problem on the primary `DataStore`.
- **Response model: remove the property, not null it out.** `DataStoreDerivativeModel` drops `ConnectionString` entirely rather than keeping it and always mapping to `null`, to match the primary `DataStoreModel`'s shape exactly and avoid leaving a residual field in the OpenAPI schema.

## Testing

**Unit tests (new):**
- `AddDataStoreDerivative`/`EditDataStoreDerivative` handler tests — `A.Fake<ISymmetricStringEncryptionProvider>()`, assert `Encrypt` is invoked and its result is what's persisted (pattern: `AddDataStoreTests.cs` / `EditDataStoreHandlerTests.cs`).
- `DataStoreDerivativeMapper` test — assert the mapped model carries no `ConnectionString` value.
- `DataStoreEncryptionHelperTests` — extend with round-trip cases for `EncryptDerivativeConnectionStringsIfNeededAsync`, using the real `Aes256SymmetricStringEncryptionProvider` (existing pattern for this helper).
- `GetDataStoreDerivativesQueryTests` / `GetDataStoreDerivativeByIdQueryTests` (new or extended) — seed a plaintext row, assert it's re-encrypted and saved after `Execute()`.

**E2E (Bruno) updates required** — these assert response-body JSON schema and need their `"connectionString": {...}` schema-assertion block removed (request-body `connectionString` on POST/PUT stays unchanged):
- `DataStoreDerivatives/GET - DataStoreDerivatives.bru`
- `DataStoreDerivatives/GET - DataStoreDerivatives - Without Offset.bru`
- `DataStoreDerivatives/GET - DataStoreDerivatives - Without Limit.bru`
- `DataStoreDerivatives/GET - DataStoreDerivatives - Without Offset and Limit.bru`
- `DataStoreDerivatives/GET - DataStoreDerivatives by ID.bru`
- `DataStores/GET - DataStores by ID.bru`
- `Multitenant Isolation - DataStores/GET - DataStores by ID - Tenant1.bru`

## Out of scope

- The OpenAPI YAML doc (`docs/api-specifications/openapi-yaml/admin-api-2.3.0.yaml`) is generated by CI (`.github/workflows/openapi-md.yml`), not hand-edited.
- No changes needed to `OdsInstanceDerivative`'s underlying EF entity/table — it's defined in the external `EdFi.Admin.DataAccess` package, and its `ConnectionString` column already exists; this fix only changes what's read from/written to that column.
- No current consumer decrypts a derivative's connection string for actual use (no background job, health check, or refresh path touches it) — decryption support beyond the lazy-backfill helper is not needed.
