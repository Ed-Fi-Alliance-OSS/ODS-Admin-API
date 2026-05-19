# Task 4: Update v2 RefreshEducationOrganizations Endpoints

## Objective
Update the v2 refresh education organizations endpoints to return 201 Created with jobId, status, and createdAt instead of 202 Accepted.

## Current State
- Both endpoints return 202 Accepted
- Response contains only a Message field
- No jobId or Location header in response

## Desired State
- Both endpoints return 201 Created
- Response contains jobId, status (Pending), and createdAt (UTC timestamp)
- Location header points to `/v2/jobs/{jobId}`

## Implementation Steps

### Step 1: Understand Current Job Naming
- Job identity format: `{RefreshEducationOrganizationsJobName}-{tenantIdentifier}-{Guid}`
- JobConstants.RefreshEducationOrganizationsJobName = "RefreshEducationOrganizationsJob"
- Example: `RefreshEducationOrganizationsJob-EdFi_Admin-9f8d3c2b-1a4e-4f8c-9e2d-5a6b7c8d9e0f`

### Step 2: Extract JobId After Scheduling
The Quartz scheduler provides access to the FireInstanceId when a job is scheduled.
Need to extract: `{job.Key.Name}_{fireInstanceId}`

### Step 3: Modify RefreshAllEducationOrganizations Handler
1. After `scheduler.ScheduleJob(job, trigger)`, capture the job details
2. Build jobId from job.Key.Name and scheduler fire instance ID
3. Create response object with jobId, status="Pending", createdAt=DateTime.UtcNow
4. Return Results.Created(`/v2/jobs/{jobId}`, response)

### Step 4: Modify RefreshEducationOrganizationsByInstance Handler
Same changes as Step 3

### Step 5: Update Response Documentation
- Change response code from 202 to 201 in endpoint builder
- Update endpoint Swagger annotations

### Step 6: Create Unit Tests
Create `RefreshEducationOrganizationsTests.cs`:
- Test RefreshAllEducationOrganizations returns 201 Created
- Test Location header format is correct
- Test response body contains jobId, status, createdAt
- Test RefreshEducationOrganizationsByInstance returns 201 Created
- Test Location header for instance-specific endpoint
- Test response body for instance-specific endpoint

### Step 7: Run Tests
Verify no breaking changes:
```bash
dotnet test Application/EdFi.Ods.AdminApi.UnitTests/ --filter "RefreshEducationOrganizations"
```

### Step 8: Commit Changes
```bash
git add Application/EdFi.Ods.AdminApi/Features/OdsInstances/RefreshEducationOrganizations.cs
git add Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/RefreshEducationOrganizationsTests.cs
git commit -m "feat: return 201 Created with jobId and Location header from v2 refresh endpoints"
```

## Files to Modify
1. `Application/EdFi.Ods.AdminApi/Features/OdsInstances/RefreshEducationOrganizations.cs` - Update handlers
2. `Application/EdFi.Ods.AdminApi.UnitTests/Features/OdsInstances/RefreshEducationOrganizationsTests.cs` - Create new test file

## Notes
- Task 5 will do identical changes for v3 endpoints
- Task 6-7 will create the GET `/v2/jobs/{jobId}` polling endpoint
- jobId extraction must handle Quartz FireInstanceId format
- No changes to job creation/enqueue logic needed
