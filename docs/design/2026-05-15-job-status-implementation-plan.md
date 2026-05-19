# Job Status Tracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement job status tracking for education organization refresh operations by returning jobIds from POST endpoints and creating a GET endpoint to poll job completion status.

**Architecture:** Extend JobStatus entity with timestamps, update refresh endpoints (v2/v3) to return 201 Created with Location header and jobId, create new GET endpoints (v2/v3) to retrieve full job status details.

**Tech Stack:** Entity Framework Core, NUnit + Shouldly for tests, FakeItEasy for mocks, Quartz.NET job scheduler.

---

## File Structure

**Files to Create:**
- `Application/EdFi.Ods.AdminApi/Features/OdsInstances/GetJobStatus.cs` - v2 endpoint handler
- `Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/GetJobStatus.cs` - v3 endpoint handler
- `Application/EdFi.Ods.AdminApi.Tests/Features/OdsInstances/GetJobStatusTests.cs` - unit tests for v2
- `Application/EdFi.Ods.AdminApi.V3.Tests/Features/OdsInstances/GetJobStatusTests.cs` - unit tests for v3

**Files to Modify:**
- `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/JobStatus.cs` - add CreatedAt and FinishedAt properties
- `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/JobStatusService.cs` - set timestamps on status changes
- `Application/EdFi.Ods.AdminApi/Features/OdsInstances/RefreshEducationOrganizations.cs` - return 201 with jobId and Location
- `Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/RefreshEducationOrganizations.cs` - return 201 with jobId and Location
- `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/AdminApiQuartzJobBase.cs` - ensure jobId extraction for response
- E2E test suite (exact location TBD based on project structure)

**Migration:** Create DbUp migration to add CreatedAt and FinishedAt columns to adminapi.jobstatuses table

---

## Task 1: Add Timestamp Properties to JobStatus Entity

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/JobStatus.cs`

- [ ] **Step 1: View current JobStatus entity**

Run: Open the file to see existing properties (Id, JobId, Status, ErrorMessage)

- [ ] **Step 2: Add CreatedAt and FinishedAt properties**

Insert these properties after the existing ErrorMessage property:
```csharp
public DateTime CreatedAt { get; set; }
public DateTime? FinishedAt { get; set; }
```

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Common/Infrastructure/Jobs/JobStatus.cs
git commit -m "feat: add CreatedAt and FinishedAt timestamps to JobStatus entity"
```

---

## Task 2: Create Database Migration for Timestamps

**Files:**
- Create: `Application/EdFi.Ods.AdminApi/Artifacts/MsSql/0XX-AddJobStatusTimestamps.sql` (choose next sequential number)

- [ ] **Step 1: Create migration script**

```sql
-- Migration: Add timestamp columns to jobstatuses table

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'jobstatuses' AND COLUMN_NAME = 'CreatedAt')
BEGIN
    ALTER TABLE [adminapi].[jobstatuses]
    ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE();
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'jobstatuses' AND COLUMN_NAME = 'FinishedAt')
BEGIN
    ALTER TABLE [adminapi].[jobstatuses]
    ADD FinishedAt DATETIME2 NULL;
END;
```

- [ ] **Step 2: Update existing JobStatus records (migration script)**

Add this to set CreatedAt for existing records:
```sql
-- Set CreatedAt to current time for existing records that don't have it
UPDATE [adminapi].[jobstatuses]
SET CreatedAt = GETUTCDATE()
WHERE CreatedAt IS NULL OR CreatedAt = '1900-01-01';
```

- [ ] **Step 3: Verify migration file is in correct location**

Check that the file matches the pattern of other DbUp migrations in `Artifacts/MsSql/` directory.

- [ ] **Step 4: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Artifacts/MsSql/0XX-AddJobStatusTimestamps.sql
git commit -m "feat: create migration to add CreatedAt and FinishedAt columns to jobstatuses"
```

---

## Task 3: Update JobStatusService to Set Timestamps

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/JobStatusService.cs`

- [ ] **Step 1: View JobStatusService to understand current structure**

Locate the `SetStatusAsync` method and understand how it creates/updates job status records.

- [ ] **Step 2: Add CreatedAt when creating new status**

When creating a new JobStatus record (initial status), set CreatedAt to `DateTime.UtcNow`:
```csharp
var jobStatus = new JobStatus
{
    JobId = jobId,
    Status = status.ToString(),
    CreatedAt = DateTime.UtcNow,
    ErrorMessage = errorMessage
};
```

- [ ] **Step 3: Add FinishedAt when job completes or errors**

Update the logic: when status is `JobStatusEnum.Completed` or `JobStatusEnum.Error`, set FinishedAt to `DateTime.UtcNow`:
```csharp
jobStatus.FinishedAt = DateTime.UtcNow;
```

This should only happen on updates where the final status is being set (not on every status change).

- [ ] **Step 4: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Infrastructure/Services/Jobs/JobStatusService.cs
git commit -m "feat: set CreatedAt and FinishedAt timestamps in JobStatusService"
```

---

## Task 4: Update v2 RefreshEducationOrganizations Endpoints

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/RefreshEducationOrganizations.cs` (both handlers)

- [ ] **Step 1: Locate RefreshAllEducationOrganizations handler**

Find the handler around line 44-67 that handles POST /v2/odsInstances/edOrgs/refresh

- [ ] **Step 2: Extract jobId from job key**

After enqueuing the job, extract jobId using the runId format:
```csharp
// Extract jobId from job identity: "RefreshEducationOrganizationsJob-{tenantIdentifier}-{guid}"
var jobId = $"{job.Key.Name}_{context.FireInstanceId}";
```

- [ ] **Step 3: Update response to return 201 with Location header**

Change from `Results.Accepted()` to:
```csharp
var response = new 
{ 
    jobId = jobId, 
    status = "Pending", 
    createdAt = DateTime.UtcNow 
};
var locationUri = $"/v2/jobs/{jobId}";
return Results.Created(locationUri, response);
```

- [ ] **Step 4: Repeat for RefreshEducationOrganizationsByInstance handler**

Apply the same changes to the handler around line 69-101 that handles POST /v2/odsInstances/{instanceId}/edOrgs/refresh

- [ ] **Step 5: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Features/OdsInstances/RefreshEducationOrganizations.cs
git commit -m "feat: return 201 with jobId and Location header from v2 refresh endpoints"
```

---

## Task 5: Update v3 RefreshEducationOrganizations Endpoints

**Files:**
- Modify: `Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/RefreshEducationOrganizations.cs` (both handlers)

- [ ] **Step 1: Locate v3 RefreshAllEducationOrganizations handler**

Find the v3 version of the handler

- [ ] **Step 2: Apply same jobId extraction and 201 response**

Use identical logic as Task 4, but with `/v3/jobs/{jobId}` location URI:
```csharp
var jobId = $"{job.Key.Name}_{context.FireInstanceId}";
var response = new 
{ 
    jobId = jobId, 
    status = "Pending", 
    createdAt = DateTime.UtcNow 
};
var locationUri = $"/v3/jobs/{jobId}";
return Results.Created(locationUri, response);
```

- [ ] **Step 3: Repeat for v3 RefreshEducationOrganizationsByInstance handler**

- [ ] **Step 4: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/RefreshEducationOrganizations.cs
git commit -m "feat: return 201 with jobId and Location header from v3 refresh endpoints"
```

---

## Task 6: Create v2 GetJobStatus Endpoint Handler

**Files:**
- Create: `Application/EdFi.Ods.AdminApi/Features/OdsInstances/GetJobStatus.cs`

- [ ] **Step 1: Create new feature handler file**

```csharp
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public class GetJobStatus
{
    public class Request
    {
        public string JobId { get; set; }
    }

    public class Response
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Handler
    {
        private readonly IJobStatusService _jobStatusService;

        public Handler(IJobStatusService jobStatusService)
        {
            _jobStatusService = jobStatusService;
        }

        public async Task<IResult> Handle(string jobId)
        {
            var jobStatus = await _jobStatusService.GetStatusAsync(jobId);

            if (jobStatus == null)
            {
                return Results.NotFound(new { message = "Job not found" });
            }

            var response = new Response
            {
                JobId = jobStatus.JobId,
                Status = jobStatus.Status,
                CreatedAt = jobStatus.CreatedAt,
                FinishedAt = jobStatus.FinishedAt,
                ErrorMessage = jobStatus.ErrorMessage
            };

            return Results.Ok(response);
        }
    }

    public class Feature : IFeature
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints
                .MapGroup("/v2/jobs")
                .WithName("Jobs");

            group
                .MapGet("/{jobId}", Handle)
                .WithName("GetJobStatus")
                .WithOpenApi()
                .WithDescription("Get the status of a job");
        }

        private static async Task<IResult> Handle(string jobId, IJobStatusService jobStatusService)
        {
            var handler = new Handler(jobStatusService);
            return await handler.Handle(jobId);
        }
    }
}
```

- [ ] **Step 2: Verify IJobStatusService has GetStatusAsync method**

Check that the service has a method to retrieve a single job status by jobId. If not, add it.

- [ ] **Step 3: Register the feature**

Find where features are registered (likely in Program.cs or a feature registration module) and add:
```csharp
builder.Services.AddScoped<GetJobStatus.Feature>();
```

- [ ] **Step 4: Commit**

```bash
git add Application/EdFi.Ods.AdminApi/Features/OdsInstances/GetJobStatus.cs
git commit -m "feat: create v2 GET /v2/jobs/{jobId} endpoint"
```

---

## Task 7: Create v3 GetJobStatus Endpoint Handler

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/GetJobStatus.cs`

- [ ] **Step 1: Create v3 version of GetJobStatus handler**

Use identical structure as Task 6, but with `/v3/jobs` in the MapGroup call:

```csharp
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.V3.Features.OdsInstances;

public class GetJobStatus
{
    // Identical Request, Response, and Handler classes as v2

    public class Feature : IFeature
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints
                .MapGroup("/v3/jobs")
                .WithName("Jobs");

            group
                .MapGet("/{jobId}", Handle)
                .WithName("GetJobStatus")
                .WithOpenApi()
                .WithDescription("Get the status of a job");
        }

        private static async Task<IResult> Handle(string jobId, IJobStatusService jobStatusService)
        {
            var handler = new Handler(jobStatusService);
            return await handler.Handle(jobId);
        }
    }
}
```

- [ ] **Step 2: Register the v3 feature**

Add to Program.cs or appropriate registration location:
```csharp
builder.Services.AddScoped<GetJobStatus.Feature>();
```

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.V3/Features/OdsInstances/GetJobStatus.cs
git commit -m "feat: create v3 GET /v3/jobs/{jobId} endpoint"
```

---

## Task 8: Write Unit Tests for JobStatusService Timestamp Logic

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.Tests/Features/OdsInstances/GetJobStatusTests.cs`

- [ ] **Step 1: Create test class**

```csharp
using NUnit.Framework;
using Shouldly;
using FakeItEasy;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using System;
using System.Threading.Tasks;

namespace EdFi.Ods.AdminApi.Tests.Features.OdsInstances;

[TestFixture]
public class GetJobStatusTests
{
    private IJobStatusService _jobStatusService;

    [SetUp]
    public void SetUp()
    {
        _jobStatusService = A.Fake<IJobStatusService>();
    }

    [Test]
    public async Task Handle_WhenJobStatusExists_ReturnsOkWithCompleteData()
    {
        // Arrange
        var jobId = "RefreshEducationOrganizationsJob-tenant-123_fireinstance-456";
        var createdAt = DateTime.UtcNow.AddSeconds(-30);
        var finishedAt = DateTime.UtcNow;
        
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Completed",
            CreatedAt = createdAt,
            FinishedAt = finishedAt,
            ErrorMessage = null
        };

        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        var handler = new GetJobStatus.Handler(_jobStatusService);

        // Act
        var result = await handler.Handle(jobId);

        // Assert
        result.ShouldNotBeNull();
        // Verify result is Ok and contains expected data
    }

    [Test]
    public async Task Handle_WhenJobNotFound_ReturnsNotFound()
    {
        // Arrange
        var jobId = "nonexistent-job-id";
        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns((JobStatus)null);

        var handler = new GetJobStatus.Handler(_jobStatusService);

        // Act
        var result = await handler.Handle(jobId);

        // Assert
        result.ShouldNotBeNull();
        // Verify result is NotFound
    }

    [Test]
    public async Task Handle_WhenJobInProgress_ReturnsStatusWithoutFinishedAt()
    {
        // Arrange
        var jobId = "RefreshEducationOrganizationsJob-tenant-123_fireinstance-456";
        var createdAt = DateTime.UtcNow;
        
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "InProgress",
            CreatedAt = createdAt,
            FinishedAt = null,
            ErrorMessage = null
        };

        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        var handler = new GetJobStatus.Handler(_jobStatusService);

        // Act
        var result = await handler.Handle(jobId);

        // Assert
        result.ShouldNotBeNull();
        // Verify FinishedAt is null in response
    }

    [Test]
    public async Task Handle_WhenJobErrored_ReturnsStatusWithErrorMessage()
    {
        // Arrange
        var jobId = "RefreshEducationOrganizationsJob-tenant-123_fireinstance-456";
        var errorMsg = "Database connection failed";
        var finishedAt = DateTime.UtcNow;
        
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            Status = "Error",
            CreatedAt = DateTime.UtcNow.AddSeconds(-60),
            FinishedAt = finishedAt,
            ErrorMessage = errorMsg
        };

        A.CallTo(() => _jobStatusService.GetStatusAsync(jobId))
            .Returns(jobStatus);

        var handler = new GetJobStatus.Handler(_jobStatusService);

        // Act
        var result = await handler.Handle(jobId);

        // Assert
        result.ShouldNotBeNull();
        // Verify ErrorMessage is included in response
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test Application/EdFi.Ods.AdminApi.Tests/Features/OdsInstances/GetJobStatusTests.cs -v
```

Expected: FAIL (handler doesn't exist yet)

- [ ] **Step 3: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.Tests/Features/OdsInstances/GetJobStatusTests.cs
git commit -m "test: add unit tests for GetJobStatus handler"
```

---

## Task 9: Create E2E Tests for All Three Endpoints

**Files:**
- Create: `Application/EdFi.Ods.AdminApi.E2E.Tests/Features/OdsInstances/RefreshAndStatusCheckTests.cs` (adjust path based on project structure)

- [ ] **Step 1: Create E2E test class**

```csharp
using NUnit.Framework;
using Shouldly;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EdFi.Ods.AdminApi.E2E.Tests.Features.OdsInstances;

[TestFixture]
public class RefreshAndStatusCheckTests
{
    private HttpClient _httpClient;

    [SetUp]
    public void SetUp()
    {
        // Initialize HTTP client pointing to test API
        _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    }

    [Test]
    public async Task RefreshAllEdOrgs_ReturnsCreatedWithJobIdAndLocationHeader()
    {
        // Arrange
        var tenantId = "test-tenant";
        
        // Act
        var response = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldContain("/v2/jobs/");

        var content = await response.Content.ReadAsAsync<dynamic>();
        content.jobId.ShouldNotBeNull();
        content.status.ShouldBe("Pending");
        content.createdAt.ShouldNotBeNull();
    }

    [Test]
    public async Task RefreshEdOrgsByInstance_ReturnsCreatedWithJobIdAndLocationHeader()
    {
        // Arrange
        var instanceId = "test-instance-id";

        // Act
        var response = await _httpClient.PostAsync($"/v2/odsInstances/{instanceId}/edOrgs/refresh", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldContain("/v2/jobs/");

        var content = await response.Content.ReadAsAsync<dynamic>();
        content.jobId.ShouldNotBeNull();
    }

    [Test]
    public async Task GetJobStatus_ReturnsCompleteJobDetails()
    {
        // Arrange
        var refreshResponse = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);
        var refreshContent = await refreshResponse.Content.ReadAsAsync<dynamic>();
        var jobId = refreshContent.jobId;

        // Act
        var statusResponse = await _httpClient.GetAsync($"/v2/jobs/{jobId}");

        // Assert
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var statusContent = await statusResponse.Content.ReadAsAsync<dynamic>();
        statusContent.jobId.ShouldBe(jobId);
        statusContent.status.ShouldNotBeNull();
        statusContent.createdAt.ShouldNotBeNull();
    }

    [Test]
    public async Task V3RefreshAndStatusCheck_WorksWithV3Endpoints()
    {
        // Arrange - same pattern but with /v3 paths

        // Act
        var refreshResponse = await _httpClient.PostAsync("/v3/odsInstances/edOrgs/refresh", null);
        var refreshContent = await refreshResponse.Content.ReadAsAsync<dynamic>();
        var jobId = refreshContent.jobId;

        // Assert
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var statusResponse = await _httpClient.GetAsync($"/v3/jobs/{jobId}");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetJobStatus_WithNonexistentJobId_ReturnsNotFound()
    {
        // Act
        var response = await _httpClient.GetAsync("/v2/jobs/nonexistent-job-id");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Application/EdFi.Ods.AdminApi.E2E.Tests/Features/OdsInstances/RefreshAndStatusCheckTests.cs
git commit -m "test: add E2E tests for refresh endpoints and job status polling"
```

---

## Task 10: Run All Tests and Verify Implementation

**Files:** None (validation only)

- [ ] **Step 1: Run unit tests**

```bash
dotnet test Application/EdFi.Ods.AdminApi.Tests/ -v
```

Expected: All tests pass

- [ ] **Step 2: Run integration tests**

```bash
dotnet test Application/EdFi.Ods.AdminApi.IntegrationTests/ -v
```

Expected: All tests pass

- [ ] **Step 3: Run full build**

```bash
./build.ps1 -Command Build
```

Expected: Build succeeds

- [ ] **Step 4: Run local API and manually test endpoints**

```bash
./build.ps1 -Command Run
```

Then use curl or Postman:
```bash
# Test v2 refresh - should return 201
curl -X POST http://localhost:5000/v2/odsInstances/edOrgs/refresh

# Test v2 job status - should return 200 with full details
curl -X GET http://localhost:5000/v2/jobs/{jobId}

# Test v3 endpoints similarly
curl -X POST http://localhost:5000/v3/odsInstances/edOrgs/refresh
curl -X GET http://localhost:5000/v3/jobs/{jobId}
```

- [ ] **Step 5: Verify timestamp fields in database**

Query the jobstatuses table to confirm CreatedAt and FinishedAt are being populated:
```sql
SELECT JobId, Status, CreatedAt, FinishedAt, ErrorMessage FROM adminapi.jobstatuses ORDER BY CreatedAt DESC LIMIT 5;
```

- [ ] **Step 6: Final commit**

```bash
git add -A
git commit -m "test: verify all implementations and E2E functionality"
```

---

## Summary

This plan implements complete job status tracking across:
1. **Entity & Database** - Add timestamp columns (Tasks 1-2)
2. **Service Layer** - Set timestamps on job status transitions (Task 3)
3. **Refresh Endpoints** - Return 201 with jobId and Location header (Tasks 4-5)
4. **Status Endpoint** - New GET endpoints for v2 and v3 (Tasks 6-7)
5. **Testing** - Unit and E2E test coverage (Tasks 8-9)
6. **Validation** - Full test suite execution (Task 10)

All requirements from the design specification are covered. Features work independently—can commit after each task.

