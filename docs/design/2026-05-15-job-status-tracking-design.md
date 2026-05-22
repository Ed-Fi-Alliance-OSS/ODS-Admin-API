# Job Status Tracking for Education Organizations Refresh

**Date:** 2026-05-15  
**Status:** Design Approved  
**API Versions:** v2, v3  

## Overview

This design adds a mechanism to track the asynchronous job status for education organization refresh operations. When users initiate a refresh via POST endpoints, they receive a jobId and can poll a new GET endpoint to check completion status, error messages, and timing information.

## Problem Statement

Currently, the refresh endpoints (`POST /odsInstances/edOrgs/refresh` and `POST /odsInstances/{instanceId}/edOrgs/refresh`) are fire-and-forget—they queue a job and return immediately with no way for the Admin App to track job completion. This makes it difficult to provide users with feedback on job progress or handle failures gracefully.

## Solution

Implement three coordinated changes:

1. **Enhance JobStatus table** with timestamp fields to track job lifecycle
2. **Update refresh endpoints** to return 201 Created with Location header and jobId
3. **Create new GET endpoint** to retrieve full job status and details

## Architecture

### Database Schema Changes

Extend the existing `JobStatus` entity in both V2 and V3 contexts:

```csharp
public class JobStatus
{
    public int Id { get; set; }
    public string JobId { get; set; } = null!;           // runId format
    public string Status { get; set; } = null!;           // Pending, InProgress, Completed, Error
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }               // NEW: when job was queued
    public DateTime? FinishedAt { get; set; }             // NEW: when job finished (Completed or Error)
}
```

**Timestamp Semantics:**
- `createdAt`: Set when the job is first created (status = "Pending")
- `finishedAt`: Set when job execution finishes, regardless of success or failure (status = "Completed" or "Error"). Remains null for "Pending" and "InProgress"

### Refresh Endpoints (Updated)

**POST /v2/odsInstances/edOrgs/refresh** and **POST /v3/odsInstances/edOrgs/refresh**

Changes:
- Return HTTP **201 Created** instead of 202 Accepted
- Return `Location` header with value `/v2/jobs/{jobId}` or `/v3/jobs/{jobId}`
- Return jobId in response body
- jobId format: `{JobName}_{fireInstanceId}` (the runId stored in database)

Response:
```json
201 Created
Location: /v2/jobs/RefreshEducationOrganizationsJob-tenant-guid_fireInstanceId

{
  "jobId": "RefreshEducationOrganizationsJob-tenant-guid_fireInstanceId",
  "message": "Education organizations refresh has been queued for all instances"
}
```

**POST /v2/odsInstances/{instanceId}/edOrgs/refresh** and **POST /v3/odsInstances/{instanceId}/edOrgs/refresh**

Same changes as above, with Location header pointing to the job resource:
```json
201 Created
Location: /v2/jobs/RefreshEducationOrganizationsJob-tenant-guid_fireInstanceId

{
  "jobId": "RefreshEducationOrganizationsJob-tenant-guid_fireInstanceId",
  "message": "Education organizations refresh has been queued for instance {instanceId}"
}
```

### New Job Resource Endpoint

**GET /v2/jobs/{jobId}** and **GET /v3/jobs/{jobId}**

Retrieves the complete job status record from the database.

**Success Response (200 OK):**
```json
{
  "jobId": "RefreshEducationOrganizationsJob-tenant-guid_fireInstanceId",
  "status": "Completed",
  "createdAt": "2026-05-15T16:44:22Z",
  "finishedAt": "2026-05-15T16:45:10Z",
  "errorMessage": null
}
```

**Not Found Response (404 Not Found):**
```json
{
  "title": "Not Found",
  "detail": "Job not found"
}
```

**Status Values:**
- `Pending` - Job queued, waiting to execute
- `InProgress` - Job currently executing
- `Completed` - Job finished successfully
- `Error` - Job execution failed

## Implementation Details

### JobStatusService Changes

Update `JobStatusService.SetStatusAsync()` in both V2 and V3:

1. When setting initial status, set `createdAt = DateTime.UtcNow`
2. When transitioning to "Completed" or "Error", set `finishedAt = DateTime.UtcNow`
3. Handle both single-tenant and multi-tenant scenarios

### Endpoint Implementation

Create new feature file(s):
- `EdFi.Ods.AdminApi.Features.Jobs.GetJobStatus` (V2)
- `EdFi.Ods.AdminApi.V3.Features.Jobs.GetJobStatus` (V3)

Modify existing refresh features:
- Extract jobId from created job (already available as `job.Key.Name`)
- Calculate runId as `{jobId}_{fireInstanceId}` before scheduling
- Return 201 with Location header and jobId in response
- Ensure jobId is returned immediately (before job execution)

### Multi-Tenancy Considerations

- jobId includes tenant identifier as part of the job name
- GET endpoint queries the correct tenant database when resolving job status
- TenantIdentificationMiddleware ensures tenant context is available

## Testing Strategy

### Unit Tests
- JobStatusService tests for timestamp behavior (createdAt, finishedAt)
- Endpoint handler tests for 201 response and Location header generation

### E2E Tests
1. **Refresh + Status Flow (Success):**
   - POST to refresh endpoint → receives jobId and 201
   - Poll GET endpoint → eventually returns "Completed" with finishedAt set
   
2. **Refresh + Status Flow (Error):**
   - POST to refresh endpoint → receives jobId
   - Poll GET endpoint → eventually returns "Error" with errorMessage and finishedAt set
   
3. **Status Endpoint (Not Found):**
   - GET with non-existent jobId → returns 404
   
4. **Both API Versions:**
   - Tests cover v2 and v3 endpoints
   - Verify correct Location header paths (/v2/jobs/{jobId} vs /v3/jobs/{jobId})

## Data Migration

A database migration script will be required to:
1. Add `CreatedAt` and `FinishedAt` columns to the `JobStatuses` table
2. Backfill `CreatedAt` with current timestamp for existing records
3. Leave `FinishedAt` as null for records with status "Pending" or "InProgress"
4. Set `FinishedAt` to current timestamp for existing "Completed" or "Error" records

## Error Handling

- Invalid jobId format or non-existent job → 404 Not Found
- Database errors during status retrieval → 500 Internal Server Error (standard error handling)
- Multi-tenant context errors → 403 Forbidden (via existing middleware)

## Performance Considerations

- JobStatus query by jobId is fast (indexed lookup)
- Polling interval recommendation: 1-5 seconds (to be documented in API spec)
- No batch status queries planned at this time

## Backwards Compatibility

- Refresh endpoints change from 202 to 201 (breaking change for clients expecting 202)
- New GET endpoint is purely additive
- Existing job tracking via database remains unchanged
- No impact on scheduled refresh jobs (only affects on-demand refresh endpoints)

## Future Considerations

- Bulk job status queries (GET /v2/jobs with filters)
- Job cancellation support
- Job retention/cleanup policies
- WebSocket support for real-time status updates
