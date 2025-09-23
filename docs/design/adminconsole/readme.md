# Admin API Design to Support the Admin App

## Overview

The Ed-Fi Admin App is a web-based user interface tool for managing Ed-Fi
ODS/API Platform installations. User can perform actions that include:

* Manage tenants and database instances
* Manage Client credentials ("keys and secrets")
* Review application and database health

The Ed-Fi Admin App application is a front-end only. The Ed-Fi Admin API 2
application will act as the backend-for-frontend (BFF), serving all of the
interaction needs for Admin App. The Ed-Fi Admin API 2 will in turn rely on
other services and "worker" applications as needed to achieve some of its goals.

The purpose of this document and related documents is to describe the
architecture of the related applications, the interfaces that sustain
communication, and the storage layers requirements.

> [!TIP]
> The following sections utilize the [C4 model](https://c4model.com/) for
> describing the System Context and decomposing Containers within that Context.
> The notes further refine the architecture with detailed [UML sequence
> diagrams](https://en.wikipedia.org/wiki/Sequence_diagram).

## System Context

```mermaid
C4Context
    title System Context for Ed-Fi Admin App

    Enterprise_Boundary(edorg, "Education Organization") {
        Person(User, "Admin App User", "A system administrator")
    }

    Enterprise_Boundary(other, "Other Services") {
        System(Keycloak, "Keycloak", "OpenID Connect authorization provider")
    }

    Enterprise_Boundary(edfi, "Ed-Fi ODS/API Platform") {
        System(AdminConsole, "Ed-Fi Admin App", "A web application for managing ODS/API Deployments")


        System_Boundary(backend, "Backend Systems") {
            System(AdminAPI, "Ed-Fi Admin API 2 and Workers", "A REST API system for managing<br />administrative data and deployments,<br />plus background worker apps")
            System(OdsApi, "Ed-Fi ODS/API", "A REST API system for<br />educational data interoperability")
        }
    }

    Rel(User, AdminConsole, "Manages instances,<br/>Manages client credentials,<br/>Checks API health")
    UpdateRelStyle(User, AdminConsole, $offsetX="0", $offsetY="-10")

    Rel(AdminConsole, AdminAPI, "Issues HTTP requests")
    UpdateRelStyle(AdminConsole, AdminAPI, $offsetY="-40", $offsetX="20")

    Rel(AdminAPI, OdsApi, "Reads and<br />configures")
    UpdateRelStyle(AdminAPI, OdsApi, $offsetY="-20", $offsetX="-30")

    Rel(User, Keycloak, "Authentication")
    UpdateRelStyle(User, Keycloak, $offsetX="-20", $offsetY="10")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="2")
```

## Containers

Two of the functions needed by Admin API 2 will benefit from background
execution, so that the Admin App end user can experience a quick response
time in the web browser. Two worker applications will provide the background
processing:

1. Instance Management Worker - creates new database instances.
2. Health Check Worker - polls the ODS/API to find record counts and records
   them in Admin API 2.

```mermaid
C4Container
    title "Admin API Containers"

    System(AdminConsole, "Ed-Fi Admin App", "A web application for managing ODS/API Deployments")

    System_Boundary(backend, "Backend Systems") {

        Boundary(b0, "Admin API") {
            Container(AdminAPI, "Ed-Fi Admin API 2")
            Container(HealthCheck, "Admin API Health<br />Check Worker")
            Container(Instance, "Admin API Instance<br />Management Worker")
        }

        Boundary(b1, "ODS/API") {
            System(OdsApi, "Ed-Fi ODS/API", "A REST API system for<br />educational data interoperability")
            SystemDb(ods3, "EdFi_ODS_<instanceN>")
        }

        Boundary(b2, "Shared Databases") {
            ContainerDb(Admin, "EdFi_Admin,<br />EdFi_Security")
        }
    }
    
    Rel(AdminConsole, AdminAPI, "Issues HTTP requests")

    Rel(HealthCheck, AdminAPI, "Reads ODS/API connections,<br />Writes health info")
    UpdateRelStyle(HealthCheck, AdminAPI, $offsetY="50")

    Rel(HealthCheck, OdsApi, "Reads records counts")
    UpdateRelStyle(HealthCheck, OdsApi, $offsetX="-60", $offsetY="20")

    Rel(Instance, AdminAPI, "Reads instance requests,<br />Write instance status")
    UpdateRelStyle(Instance, AdminAPI, $offsetY="20", $offsetX="10")

    Rel(Instance, ods3, "Creates new ODS instances")
    UpdateRelStyle(Instance, ods3, $offsetY="20", $offsetX="-50")

    Rel(OdsApi, ods3, "Reads and writes")
    UpdateRelStyle(OdsApi, ods3, $offsetX="10")
    
    Rel(AdminAPI, Admin, "Reads and writes")
    UpdateRelStyle(AdminAPI, Admin, $offsetY="50", $offsetX="10")

    Rel(OdsApi, Admin, "Reads")
    UpdateRelStyle(OdsApi, Admin, $offsetY="20", $offsetX="-10")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="2")
```

## Interfaces and Interactions

The API interfaces and the interactions between specific containers are
described in detail in the following documents:

* [REST API Support for Admin App](./APIS-FOR-ADMIN-CONSOLE.md)
* [Instance Management Worker](./INSTANCE-MANAGEMENT.md)
* [Health Check Worker](./HEALTH-CHECK-WORKER.md)

Also see [Keycloak Configuration](./KEYCLOAK.md) for more information on using
Keycloak as the Open ID Connect provider.
