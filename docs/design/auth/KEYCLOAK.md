# Keycloak Configuration

## How to Add Scopes in Keycloak

This guide will walk you through the steps to add three scopes (edfi_admin_api/full_access, edfi_admin_api/tenant_access, and edfi_admin_api/worker) in Keycloak.

### Prerequisites

* Ensure you have Keycloak installed and running.
* Access to the Keycloak administration console.

### Steps

#### Step 1: Log in to the Keycloak Admin Console

1. Open your web browser and navigate to the Keycloak admin console.
2. Enter your admin username and password, then click `Sign In`.

#### Step 2: Select the Realm

1. In the top-left corner of the console, click on the dropdown menu to select t

#### Step 3: Navigate to Client Scopes

1. In the left-hand menu, click on `Client Scopes` under the `Configure` section.
2. This will open the client scopes management page for the selected realm.

#### Step 4: Create Scope edfi_admin_api/full_access

1. Click the `Create` button to add a new client scope.
2. Fill in the following details:
   * **Name**: Enter `edfi_admin_api/full_access` as the name for the scope.
   * **Description**: (Optional) Provide a description for the scope.
3. Click the `Save` button to create the scope `edfi_admin_api/full_access`.

#### Step 5: Create Scope edfi_admin_api/tenant_access

1. Click the `Create` button again to add another client scope.
2. Fill in the following details:
   * **Name**: Enter `edfi_admin_api/tenant_access` as the name for the scope.
   * **Description**: (Optional) Provide a description for the scope.
3. Click the `Save` button to create the scope `edfi_admin_api/tenant_access`.

#### Step 6: Create Scope edfi_admin_api/worker

1. Click the `Create` button one more time to add a third client scope.
2. Fill in the following details:
   * **Name**: Enter `edfi_admin_api/worker` as the name for the scope.
   * **Description**: (Optional) Provide a description for the scope.
3. Click the `Save` button to create the scope `edfi_admin_api/worker`.

## How to Add a Realm Role to a Realm

### Prerequisites

* Access to the Keycloak administration console.

### Steps

#### Step 1: Log in to the Keycloak Admin Console

1. Open your web browser and navigate to the Keycloak admin console.
2. Enter your admin username and password, then click `Sign In`.

#### Step 2: Select the Realm

1. In the top-left corner of the console, click on the dropdown menu to select the desired realm.

#### Step 3: Navigate to Roles

1. In the left-hand menu, click on `Realm Roles` under the `Manage` section.
2. This will open the roles management page for the selected realm.

#### Step 4: Add an adminapi-client

1. Click the `Create Role` button at the top right corner of the roles management page.
2. Fill in the following details:
   * **Role Name**: adminapi-client.
   * **Description**: (Optional) Provide a description for the role.
3. Click the `Save` button to create the new role.

#### Step 4: Add an adminconsole-user

1. Click the `Create Role` button at the top right corner of the roles management page.
2. Fill in the following details:
   * **Role Name**: adminconsole-user.
   * **Description**: (Optional) Provide a description for the role.
3. Click the `Save` button to create the new role.

#### Step 5: Configure adminconsole-user Role Settings

1. Select the role adminconsole-user
2. Click the `Associated Roles` tab
3. Click the `Assign Role` button
4. Filter by realm roles
5. Check adminapi-client

#### Step 6: Assign the Role to Users

1. To assign the new realm role to users, navigate to the `Users` section in the left-hand menu.
2. Select the user you want to assign the role to.
3. Go to the `Role Mappings` tab.
4. In the `Available Roles` section, find and select the new realm role.
5. Click the `Add selected` button to assign the role to the user.

## How to Add a Mapper to Realm Roles

To add the claim `<http://schemas.microsoft.com/ws/2008/06/identity/claims/role>` to store the list of roles.

### Prerequisites

* Access to the Keycloak administration console.
* A realm with existing roles.

### Steps

#### Step 1: Log in to the Keycloak Admin Console

1. Open your web browser and navigate to the Keycloak admin console.
2. Enter your admin username and password, then click `Sign In`.

#### Step 2: Select the Realm

1. In the top-left corner of the console, click on the dropdown menu to select the desired realm.
2. If you need to create a new realm, click `Add Realm` and follow the prompts.

#### Step 3: Navigate to Client Scopes

1. In the left-hand menu, click on `Client Scopes` under the `Configure` section.
2. This will open the client scopes management page for the selected realm.

#### Step 4: Select or Create a Dedicated Scope

1. Click on an existing client (admin-console) scope from the list.
2. If creating a new client scope, provide a name and description, then click `Save`.

#### Step 5: Add a Mapper to the Dedicated Scope

1. Within the selected client scope, navigate to the `Mappers` tab.
2. Click the `Add Mapper` button and select `From predefined mappers` to add a new mapper.
3. Select `realm roles`
4. Click on the `realm roles` link
5. Fill in the following details:
   * **Name**: Enter a name for the new mapper.
   * **Mapper Type**: Select `Role Name Mapper` from the dropdown.
   * **Token Claim Name**: Enter the name "<http://schemas\.microsoft\.com/ws/2008/06/identity/claims/role>".
   * **Claim JSON Type**: Select `String` or `Array` depending on your needs.
   * **Add to ID token**: Check this box if you want to add the claim to the ID token.
   * **Add to access token**: Check this box if you want to add the claim to the access token.
   * **Add to userinfo**: Check this box if you want to add the claim to the userinfo response.
6. Click the `Save` button to create the new mapper.

#### Step 6: Explain the Purpose of the Mapper

The mapper created in the previous step is used to add a label "<http://schemas.microsoft.com/ws/2008/06/identity/claims/role>" to the tokens. By adding this label, you can ensure that the tokens contain the necessary information for your application's security and access control requirements.

> [!IMPORTANT]
> Detailed notes to be filled out, replacing these bullet points.
>
> * Should make sure we can configure Keycloak once for use both by Admin API
>   and by Admin Console.
> * Review the configuration script used by the  Data Management Service. The
>   configuration script approach is not required; Keycloak may have a better
>   way of handling this. We just need to get both projects to use the same
>   settings. Maybe create a Docker container for Ed-Fi Keycloak with shared
>   settings already in place.
> * Need to describe how to create the role for the worker processes, which
>   allows them to access client credentials via the `/adminconsole/instances`
>   endpoint.
