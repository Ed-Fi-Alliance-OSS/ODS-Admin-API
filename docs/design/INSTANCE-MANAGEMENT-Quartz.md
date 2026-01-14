
# Instance Management with Quartz.NET

## Job Triggering Options

Instance management jobs can be triggered in two ways:

### 1. Immediate Trigger

Jobs may be executed immediately upon request, allowing for rapid processing of urgent instance creation or deletion tasks.

### 2. Periodic Scheduled Trigger

Jobs can also be executed on a recurring schedule. Records with a status of `Pending` or `Pending-Delete` are automatically picked up and processed by a scheduled job. By default, this job runs every two hours, but the interval is configurable.

**Configuration:**

* The schedule interval is configurable.
* Only records with status `Pending` or `Pending-Delete` are processed during scheduled runs.

This dual approach supports both on-demand and batch processing of instance management operations.

## Overview

Instance Create and Delete operations are managed asynchronously using Quartz.NET as a job scheduler and queue. Admin API endpoints initiate these operations, which are then executed by Quartz jobs—either immediately or on a schedule. The `adminapi.DbInstances` table is updated with operation details and status throughout the process.

## Process Flow

1. **API Request:**  
   The Admin API receives a request to create or delete an instance.

2. **Job Scheduling:**  
   The API schedules a Quartz job (e.g., `CreateDbInstanceJob`, `DeleteDbInstanceJob`) with the necessary parameters, such as OdsInstance metadata and OdsInstanceId.

3. **Job Persistence:**  
   Quartz.NET stores job and trigger data in the database, tracking job status and metadata by OdsInstanceId.

4. **Job Execution:**  
   Quartz.NET executes the job, which reads/writes OdsInstance metadata and updates job status in the database.

5. **Status Update:**  
   The job updates the `adminapi.DbInstances` table and/or a dedicated job status table with the current operation status (e.g., `Pending`, `Completed`, `InProgress`, `Pending_Delete`, `Deleted`, `Delete_Failed`, `Error`), linked to the OdsInstanceId.

## Quartz.NET Integration

* Each operation type (Create, Delete) has a dedicated Quartz job.
* Jobs are enqueued by the API and executed asynchronously.
* Quartz.NET uses a persistent job store, so all job and trigger data, as well as job status, are stored in the database.
* Each job receives OdsInstance metadata and OdsInstanceId as parameters.
* Job status and results are persisted in the `adminapi.DbInstances` table and a dedicated job status table.

## Sample Implementation

### 1. Define the Job

```csharp
public class CreateInstanceJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var instanceId = context.MergedJobDataMap.GetString("InstanceId");
        var odsInstanceMetadata = context.MergedJobDataMap.GetString("OdsInstanceMetadata");
        // Set job status to InProgress in DB (linked to instanceId)
        // ... perform create logic using odsInstanceMetadata ...
        // On success: set job status to "Completed", update the DbInstances table, and create the related record in dbo.OdsInstances
        // On failure: update job status and DbInstances table to Error, with logging the error details
    }
}
```

### 2. Schedule the Job from API

```csharp
public async Task<IActionResult> CreateInstance([FromBody] InstanceModel model)
{
    // Save initial instance record with status "Pending"
    // ...

    var job = JobBuilder.Create<CreateInstanceJob>()
        .WithIdentity($"CreateInstanceJob-{model.Id}")
        .UsingJobData("InstanceId", model.Id)
        .UsingJobData("OdsInstanceMetadata", JsonConvert.SerializeObject(model))
        .Build();

    var trigger = TriggerBuilder.Create()
        .StartNow()
        .Build();

    await _scheduler.ScheduleJob(job, trigger);

    return Accepted();
}
```

### 3. Update Status in Job

* On job start: set job status to `InProgress` in the database, linked to OdsInstanceId.
* On success: set job status and DbInstances table to `Completed`.
* On failure: set job status and DbInstances table to `Error` and log error details.

### 4. Job Maintenance

* Quartz.NET manages job execution, retries, and persistence.
* Failed jobs can be retried or logged for manual intervention.

### 5. Concurrency

Quartz.NET supports parallel or sequential job processing, depending on configuration. However, business-level conflicts (e.g., renaming and deleting the same instance simultaneously) are not automatically prevented.

**Concurrency Considerations:**

* Conflicting jobs for the same instance may run in parallel unless additional safeguards are implemented.
* This can cause race conditions or inconsistent state.

**Recommended Practices:**

* Use Quartz.NET’s `[DisallowConcurrentExecution]` attribute to prevent concurrent execution of the same job type.
* Implement logic to ensure only one job per instance (by OdsInstanceId) runs at a time, such as database locks or status flags.
* Before starting a job, check the instance status in the database to avoid conflicts.
* Optionally, use job listeners or middleware to enforce these rules.

**Summary:**  
Quartz.NET handles technical job queuing and execution, but you must design your jobs and data model to prevent business-level conflicts. Proper safeguards ensure data integrity and consistent instance state.

## Benefits

* Decouples API responsiveness from long-running operations.
* Centralized job management and monitoring.
* Scalable and reliable execution of instance operations.
* All job and status data is persisted in the database, supporting robust tracking and recovery.
