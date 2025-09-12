# Instance Management Worker Removal

## Overview

This document outlines the changes made to remove the Instance Worker endpoints from the Admin API as part of ticket ADMINAPI-1294. These changes were implemented to streamline the API and remove features that were no longer needed.

## Components Removed

The following components were removed from the codebase:

1. **Instance Worker Endpoints**:
   * Instance worker-specific endpoints in the Admin API
   * Associated route registrations and handlers
   * Endpoints included:
     * `/adminconsole/odsInstances` (GET) - Used to list available ODS instances
     * `/adminconsole/odsInstances/{id}` (GET) - Used to get specific ODS instance metadata
     * `/adminconsole/odsInstances` (POST) - Used to create a new ODS instance request
     * `/adminconsole/odsInstances` (PUT) - Used to update an existing ODS instance
     * `/adminconsole/odsInstances` (DELETE) - Used to request deletion of an ODS instanceemoval
     * `/adminconsole/instances` (GET) - Used to retrieve instances for worker
     * `/adminconsole/instances/{id}` (GET) - Used to get specific instance details
     * `/adminconsole/instances` (POST) - Used to create new instance
     * `/adminconsole/instances` (PUT) - Used to update instance
     * `/adminconsole/instances` (DELETE) - Used to delete instance
     * `/adminconsole/instances/{instanceId}/completed` - Used to mark instance creation as completed
     * `/adminconsole/instances/{instanceId}/deletefailed` - Used to mark instance deletion as failed
     * `/adminconsole/instances/{instanceId}/renameFailed` - Used to mark instance rename as failed
     * `/adminconsole/instances/{instanceId}/renamed` - Used to mark instance rename as completed
     * `/adminconsole/instances/{instanceId}/deleted` - Used to mark instance deletion as completed

2. **Supporting Code**:
   * Instance worker models and DTOs (e.g., `InstanceWorkerModel`)
   * Feature handlers (e.g., `WorkerInstanceRenamed`, `WorkerInstanceDeleted`, `WorkerInstanceRenameFailed`)
   * Instance management services and commands:
     * Instance creation/completion commands
     * Instance deletion commands
     * Instance rename commands
     * Status transition handlers for instance lifecycle management

3. **Database Components**:
   * Tables:
     * `adminconsole.Instances` - Stored instance management information and status tracking for the worker queue
     * `adminconsole.OdsInstanceContexts` - Instance context mapping that duplicated information already in `dbo.OdsInstanceContext`
     * `adminconsole.OdsInstanceDerivatives` - Instance derivative information that duplicated data in `dbo.OdsInstanceDerivative`

These database tables primarily functioned as a job queue system for the Instance Management Worker, tracking the status of instance operations (creation, deletion, renaming). This information was largely redundant with the core data already maintained in the standard `dbo` schema tables, which ultimately contained the authoritative instance data used by the ODS/API.

## Restoring Functionality (If Needed)

If the Instance Worker functionality needs to be restored in the future, follow these steps:

1. **Revert the Git Commit**:

   ```bash
   git revert <commit-hash>
   ```

   Where `<commit-hash>` is the commit hash for the ADMINAPI-1294 changes.

2. **Alternative: Cherry-Pick Previous Implementation**:
   If you need to selectively restore parts of the instance worker functionality:

   ```bash
   git checkout <pre-change-commit> -- Application/EdFi.Ods.AdminApi.AdminConsole/Features/WorkerInstances/
   ```

## Testing After Restoration

1. **API Functionality**:
   * Test instance worker endpoints
   * Verify they respond with expected data

2. **Integration Testing**:
   * Verify that instance worker functionality works properly with client applications
   * Test the full lifecycle of instance creation, renaming, and deletion

## References

* Ticket: ADMINAPI-1294
* Commit: [Include the commit hash once the changes are merged]
