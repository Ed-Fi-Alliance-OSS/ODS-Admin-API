# Keycloak Configuration

## How to Add Scopes in Keycloak

This guide will walk you through the steps to add scope `edfi_admin_api/full_access` in Keycloak.

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

## How to Add a Mapper to Realm Roles

To add the claim `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` to store the list of roles.

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
   * **Token Claim Name**: Enter the name `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`.
   * **Claim JSON Type**: Select `String` or `Array` depending on your needs.
   * **Add to ID token**: Check this box if you want to add the claim to the ID token.
   * **Add to access token**: Check this box if you want to add the claim to the access token.
   * **Add to userinfo**: Check this box if you want to add the claim to the userinfo response.
6. Click the `Save` button to create the new mapper.

#### Step 6: Explain the Purpose of the Mapper

The mapper created in the previous step is used to add a label `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` to the tokens. By adding this label, you can ensure that the tokens contain the necessary information for your application's security and access control requirements.
