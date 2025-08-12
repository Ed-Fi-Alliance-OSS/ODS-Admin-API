# Integrating EdFi.Ods.AdminApi.HealthCheck into EdFi.Ods.AdminApi with Quartz.NET

## Overview

This document describes the design and process for integrating the
`EdFi.Ods.AdminApi.HealthCheck` into the `EdFi.Ods.AdminApi` application,
leveraging Quartz.NET for scheduled and on-demand execution of health checks.

---

## Goals

* Enable scheduled health checks of ODS API instances via Quartz.NET.
* Allow on-demand triggering of health checks via an API endpoint.
* Ensure only one health check job runs at a time to prevent data conflicts.
* Centralize health check logic in `EdFi.Ods.AdminApi.HealthCheck`.

---

## Architecture

### Components

* **HealthCheckService**: Service class that performs health checks across tenants and instances.
* **HealthCheckJob**: Quartz.NET job that invokes `HealthCheckService.Run()`.
* **Quartz.NET Scheduler**: Manages scheduled and ad-hoc job execution.
* **HealthCheckTrigger Endpoint**: API endpoint to trigger health checks on demand.

---

## Process Flow

### 1. Service Registration

* Register `HealthCheckService` and its dependencies in the DI container (typically as `scoped` or `transient`).
* Register `HealthCheckJob` with Quartz.NET using `AddQuartz` and `AddQuartzHostedService`.

### 2. Scheduling with Quartz.NET

* Configure Quartz.NET to schedule `HealthCheckJob` at a configurable interval (e.g., every 10 minutes, using `HealthCheckFrequencyInMinutes` from configuration).
* Use the `[DisallowConcurrentExecution]` attribute on `HealthCheckJob` to prevent overlapping executions.

### 3. On-Demand Triggering

* Implement an API endpoint (e.g., `/v2/healthcheck`) in `EdFi.Ods.AdminApi`. Note: Grouped with `v2` endpoints for consistency.
* The endpoint uses `ISchedulerFactory` to schedule an immediate, one-time execution of `HealthCheckJob`.

### 4. Concurrency Control

* `[DisallowConcurrentExecution]` ensures only one instance of `HealthCheckJob` runs at a time, regardless of trigger source (scheduled or on-demand).

---

## Configuration

* **appsettings.json**:
  * `HealthCheck:HealthCheckFrequencyInMinutes`: Controls the schedule interval.
    Set to 0 to disable scheduled health checks.
  * `AppSettings:EnableAdminConsoleAPI`: Enables or disables the health check
    API endpoint and scheduled health checks.

Please refer to the [POC PR #323](https://github.com/Ed-Fi-Alliance-OSS/AdminAPI-2.x/pull/323) for implementation details and code examples.
