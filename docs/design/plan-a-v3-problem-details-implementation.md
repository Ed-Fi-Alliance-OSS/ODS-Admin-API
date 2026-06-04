# Admin API v3 Problem Details + Admin App Compatibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Standardize Admin API **v3-only** error payloads on RFC 9457 Problem Details and make Admin App accept both compact and Problem Details envelopes without UI regressions.

**Architecture:** Add a v3-scoped Problem Details mapping path in the API pipeline, keeping v1/v2 middleware behavior untouched. Route all v3 exceptions through a single serializer that emits `application/problem+json` with extension members (`errors`, `validationErrors`, `correlationId`) when needed. In Admin App, add one normalization adapter in the HTTP error pipeline so all existing page-level handlers continue using current `StatusResponse` contracts.

**Tech Stack:** .NET 10, ASP.NET Core middleware/minimal APIs, FluentValidation, NUnit/Shouldly, Bruno E2E, React + Axios + TanStack Query (Admin App), Jest/Vitest.

---

## File Structure

### Admin API repository (`C:\dev\ed-fi\ODS-Admin-API`)

- Create: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\ErrorHandling\V3ProblemDetailsFactory.cs`  
  v3-specific helper to build RFC 9457 payloads consistently.
- Create: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\ErrorHandling\V3RequestErrorMiddleware.cs`  
  v3-only exception-to-ProblemDetails middleware.
- Modify: `Application\EdFi.Ods.AdminApi\Program.cs`  
  wire v3 middleware for v3 mode only; preserve v1/v2 pipeline.
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\AdminApiModeValidationMiddleware.cs`  
  replace compact `{ message }` response with Problem Details.
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\Jobs\GetJobStatus.cs`  
  ensure extension members shape for 400 tenant errors.
- Create: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\ErrorHandling\V3ProblemDetailsFactoryTests.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\ErrorHandling\V3RequestErrorMiddlewareTests.cs`
- Create: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\AdminApiModeValidationMiddlewareTests.cs`
- Modify: `Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0\v3\Validate Exception Content Type\Vendors - Invalid Copy.bru`
- Create: `Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0\v3\Validate Exception Content Type\AdminApi Mode Mismatch Returns ProblemDetails.bru`

### Admin App repository (`C:\dev\ed-fi\Ed-Fi-AdminApp`)

- Create: `packages\fe\src\app\api\errorEnvelope.ts`  
  shape guards + normalization for compact and Problem Details envelopes.
- Modify: `packages\fe\src\app\api\methods.ts`  
  normalize thrown errors before bubbling to mutations.
- Modify: `packages\fe\src\app\api-v2\apiClient.ts`  
  same normalization to keep v2 UI behavior consistent.
- Modify: `packages\fe\src\app\helpers\mutationErrCallback.ts`  
  consume normalized payload without page-level branching changes.
- Create: `packages\fe\src\app\api\errorEnvelope.spec.ts`
- Modify: `packages\fe\src\app\helpers\mutationErrCallback.spec.ts` (create if absent)

---

### Task 1: Add v3 Problem Details factory

**Files:**
- Create: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\ErrorHandling\V3ProblemDetailsFactory.cs`
- Test: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\ErrorHandling\V3ProblemDetailsFactoryTests.cs`

- [ ] **Step 1: Write the failing tests for RFC 9457 base fields and extensions**

```csharp
[Test]
public void CreateValidation_ShouldIncludeBaseMembersAndValidationErrors()
{
    var pd = V3ProblemDetailsFactory.CreateValidation(
        detail: "Validation failed",
        validationErrors: new Dictionary<string, string[]> { ["company"] = ["Company is required"] },
        correlationId: "trace-123");

    pd.Title.ShouldBe("Validation failed");
    pd.Status.ShouldBe(400);
    pd.Extensions.ShouldContainKey("validationErrors");
    pd.Extensions.ShouldContainKey("correlationId");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj --filter "FullyQualifiedName~V3ProblemDetailsFactoryTests" -v minimal`  
Expected: FAIL with missing type/file errors.

- [ ] **Step 3: Implement minimal factory**

```csharp
public static class V3ProblemDetailsFactory
{
    public static ProblemDetails Create(int status, string title, string detail, string? correlationId = null,
        IDictionary<string, object?>? extensions = null)
    {
        var pd = new ProblemDetails { Status = status, Title = title, Detail = detail, Type = "about:blank" };
        if (!string.IsNullOrWhiteSpace(correlationId)) pd.Extensions["correlationId"] = correlationId;
        if (extensions is not null)
            foreach (var (k, v) in extensions) pd.Extensions[k] = v;
        return pd;
    }
}
```

- [ ] **Step 4: Run tests to verify pass**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj --filter "FullyQualifiedName~V3ProblemDetailsFactoryTests" -v minimal`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Infrastructure/ErrorHandling/V3ProblemDetailsFactory.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/ErrorHandling/V3ProblemDetailsFactoryTests.cs
git commit -m "feat(v3): add RFC9457 problem details factory"
```

### Task 2: Replace v3 compact mode-mismatch payload with Problem Details

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3\Features\AdminApiModeValidationMiddleware.cs`
- Test: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Features\AdminApiModeValidationMiddlewareTests.cs`

- [ ] **Step 1: Write failing middleware test**

```csharp
[Test]
public async Task InvokeAsync_WhenVersionMismatch_ReturnsProblemDetails()
{
    var ctx = new DefaultHttpContext();
    ctx.Request.Path = "/v3/vendors";
    // configured AdminApiMode = V2
    await middleware.InvokeAsync(ctx);
    ctx.Response.StatusCode.ShouldBe(400);
    ctx.Response.ContentType.ShouldContain("application/problem+json");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj --filter "FullyQualifiedName~AdminApiModeValidationMiddlewareTests" -v minimal`  
Expected: FAIL (current response is compact JSON).

- [ ] **Step 3: Implement Problem Details response**

```csharp
if (requestedVersion != _adminApiMode && !path.Contains("/swagger/"))
{
    var pd = V3ProblemDetailsFactory.Create(
        status: StatusCodes.Status400BadRequest,
        title: "Bad Request",
        detail: "Wrong API version for this instance mode.",
        correlationId: context.TraceIdentifier);

    response.StatusCode = 400;
    response.ContentType = "application/problem+json";
    await response.WriteAsync(JsonSerializer.Serialize(pd));
    return;
}
```

- [ ] **Step 4: Re-run test**

Run: same command as Step 2  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/AdminApiModeValidationMiddleware.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Features/AdminApiModeValidationMiddlewareTests.cs
git commit -m "feat(v3): return problem details for mode mismatch"
```

### Task 3: Add v3-only request exception middleware and wire it

**Files:**
- Create: `Application\EdFi.Ods.AdminApi.V3\Infrastructure\ErrorHandling\V3RequestErrorMiddleware.cs`
- Modify: `Application\EdFi.Ods.AdminApi\Program.cs`
- Test: `Application\EdFi.Ods.AdminApi.V3.UnitTests\Infrastructure\ErrorHandling\V3RequestErrorMiddlewareTests.cs`

- [ ] **Step 1: Write failing tests for ValidationException and NotFoundException mapping**

```csharp
[Test]
public async Task InvokeAsync_WhenValidationException_ReturnsValidationProblemDetails()
{
    // arrange middleware with next throwing ValidationException
    // assert 400 + validationErrors extension + problem+json content type
}
```

- [ ] **Step 2: Run failing tests**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj --filter "FullyQualifiedName~V3RequestErrorMiddlewareTests" -v minimal`  
Expected: FAIL.

- [ ] **Step 3: Implement middleware and v3-only program wiring**

```csharp
if (adminApiMode == AdminApiMode.V3)
{
    app.UseMiddleware<EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling.V3RequestErrorMiddleware>();
}
else
{
    app.UseMiddleware<RequestLoggingMiddleware>();
}
```

- [ ] **Step 4: Run tests**

Run: same command as Step 2  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Program.cs Application/EdFi.Ods.AdminApi.V3/Infrastructure/ErrorHandling/V3RequestErrorMiddleware.cs Application/EdFi.Ods.AdminApi.V3.UnitTests/Infrastructure/ErrorHandling/V3RequestErrorMiddlewareTests.cs
git commit -m "feat(v3): add v3-only exception middleware for problem details"
```

### Task 4: Validate v3 Problem Details via Bruno E2E

**Files:**
- Modify: `Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0\v3\Validate Exception Content Type\Vendors - Invalid Copy.bru`
- Create: `Application\EdFi.Ods.AdminApi.V3\E2E Tests\Bruno Admin API E2E 3.0\v3\Validate Exception Content Type\AdminApi Mode Mismatch Returns ProblemDetails.bru`

- [ ] **Step 1: Add failing assertions for RFC 9457 members**

```javascript
test("Response includes RFC9457 members", function () {
  let body = res.getBody();
  expect(body).to.have.property("title");
  expect(body).to.have.property("status");
  expect(body).to.have.property("detail");
});
```

- [ ] **Step 2: Run targeted Bruno tests**

Run:  
`.\eng\run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode singletenant -BrunoFilter "v3/Validate Exception Content Type" -TearDown`  
Expected: at least one FAIL before middleware changes are complete.

- [ ] **Step 3: Adjust test payload checks to final extension names (`validationErrors`, `correlationId`)**

```javascript
expect(body).to.have.property("validationErrors");
expect(body).to.have.property("correlationId");
```

- [ ] **Step 4: Re-run Bruno folder**

Run: same command as Step 2  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Validate Exception Content Type/Vendors - Invalid Copy.bru" "Application/EdFi.Ods.AdminApi.V3/E2E Tests/Bruno Admin API E2E 3.0/v3/Validate Exception Content Type/AdminApi Mode Mismatch Returns ProblemDetails.bru"
git commit -m "test(v3): assert RFC9457 error payload in bruno suite"
```

### Task 5: Add Admin App dual-envelope normalization

**Files (Admin App repo):**
- Create: `C:\dev\ed-fi\Ed-Fi-AdminApp\packages\fe\src\app\api\errorEnvelope.ts`
- Modify: `C:\dev\ed-fi\Ed-Fi-AdminApp\packages\fe\src\app\api\methods.ts`
- Modify: `C:\dev\ed-fi\Ed-Fi-AdminApp\packages\fe\src\app\api-v2\apiClient.ts`
- Modify: `C:\dev\ed-fi\Ed-Fi-AdminApp\packages\fe\src\app\helpers\mutationErrCallback.ts`
- Test: `C:\dev\ed-fi\Ed-Fi-AdminApp\packages\fe\src\app\api\errorEnvelope.spec.ts`

- [ ] **Step 1: Write failing normalization tests**

```ts
it("normalizes RFC9457 problem details", () => {
  const normalized = normalizeErrorEnvelope({
    title: "Validation failed",
    status: 400,
    detail: "Bad request",
    validationErrors: { company: ["Required"] },
  });
  expect(normalized.type).toBe("ValidationError");
});
```

- [ ] **Step 2: Run FE test to confirm fail**

Run (Admin App repo):  
`npm run test:fe -- --runInBand errorEnvelope.spec.ts`  
Expected: FAIL (new helper missing).

- [ ] **Step 3: Implement adapter and wire both axios clients**

```ts
export const normalizeErrorEnvelope = (err: unknown): StatusResponse | unknown => {
  if (isProblemDetails(err)) {
    if (err.status === 400 && err.validationErrors) {
      return { type: "ValidationError", title: "Invalid submission.", data: { errors: toFieldErrors(err.validationErrors) } };
    }
    return { type: "Error", title: err.title, message: err.detail, data: pickProblemExtensions(err) };
  }
  if (isCompactEnvelope(err)) return { type: "Error", title: err.message, message: String(err.statusCode ?? "") };
  return err;
};
```

- [ ] **Step 4: Run FE tests**

Run (Admin App repo):  
`npm run test:fe -- --runInBand errorEnvelope.spec.ts mutationErrCallback.spec.ts`  
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git -C C:/dev/ed-fi/Ed-Fi-AdminApp add packages/fe/src/app/api/errorEnvelope.ts packages/fe/src/app/api/errorEnvelope.spec.ts packages/fe/src/app/api/methods.ts packages/fe/src/app/api-v2/apiClient.ts packages/fe/src/app/helpers/mutationErrCallback.ts
git -C C:/dev/ed-fi/Ed-Fi-AdminApp commit -m "feat(fe): normalize compact and problem details error envelopes"
```

### Task 6: Full verification and docs sync

**Files:**
- Modify (if needed): `docs\design\admin-api-v3-problem-details-admin-app-compatibility-design.md`

- [ ] **Step 1: Run API unit tests (v3 + common error tests)**

Run:  
`dotnet test Application\EdFi.Ods.AdminApi.V3.UnitTests\EdFi.Ods.AdminApi.V3.UnitTests.csproj -v minimal`  
`dotnet test Application\EdFi.Ods.AdminApi.Common.UnitTests\EdFi.Ods.AdminApi.Common.UnitTests.csproj -v minimal`

Expected: PASS.

- [ ] **Step 2: Run repo unit suite**

Run:  
`.\build.ps1 -Command UnitTest`  
Expected: PASS.

- [ ] **Step 3: Run v3 Bruno smoke**

Run:  
`.\eng\run-bruno-e2e.ps1 -ApiVersion 3 -TenantMode singletenant -BrunoFilter "v3/Validate Exception Content Type" -TearDown`  
Expected: PASS.

- [ ] **Step 4: Commit docs adjustments**

```bash
git add docs/design/admin-api-v3-problem-details-admin-app-compatibility-design.md
git commit -m "docs(v3): align design notes with final RFC9457 implementation details"
```

