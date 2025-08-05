<!-- Generator: Widdershins v4.0.1 -->

<h1 id="admin-api-documentation">Admin API Documentation v2</h1>

> Scroll down for code samples, example requests and responses. Select a language for code samples from the tabs above or the mobile navigation menu.

The Ed-Fi Admin API is a REST API-based administrative interface for managing vendors, applications, client credentials, and authorization rules for accessing an Ed-Fi API.

# Authentication

- oAuth2 authentication. 

    - Flow: clientCredentials

    - Token URL = [https://localhost/connect/token](https://localhost/connect/token)

|Scope|Scope Description|
|---|---|
|edfi_admin_api/full_access|Full access to the Admin API|
|edfi_admin_api/tenant_access|Access to a specific tenant|
|edfi_admin_api/worker|Worker access to the Admin API|

<h1 id="admin-api-documentation-resourceclaims">ResourceClaims</h1>

## Retrieves all resourceClaims.

`GET /v2/resourceClaims`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-resourceclaims.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|Resource Claim Id|
|name|query|string|false|Resource Claim Name|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "name": "string",
    "parentId": 0,
    "parentName": "string",
    "children": [
      {}
    ]
  }
]
```

<h3 id="retrieves-all-resourceclaims.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-resourceclaims.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[resourceClaimModel](#schemaresourceclaimmodel)]|false|none|none|
|» ResourceClaimModel|[resourceClaimModel](#schemaresourceclaimmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|false|none|none|
|»» parentId|integer(int32)¦null|false|none|none|
|»» parentName|string¦null|false|none|none|
|»» children|[[resourceClaimModel](#schemaresourceclaimmodel)]¦null|false|none|Children are collection of SimpleResourceClaimModel|
|»»» ResourceClaimModel|[resourceClaimModel](#schemaresourceclaimmodel)|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific resourceClaim based on the identifier.

`GET /v2/resourceClaims/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-resourceclaim-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "name": "string",
  "parentId": 0,
  "parentName": "string",
  "children": [
    {
      "id": 0,
      "name": "string",
      "parentId": 0,
      "parentName": "string",
      "children": []
    }
  ]
}
```

<h3 id="retrieves-a-specific-resourceclaim-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[resourceClaimModel](#schemaresourceclaimmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-resourceclaimactions">ResourceClaimActions</h1>

## Retrieves all resourceClaimActions.

`GET /v2/resourceClaimActions`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-resourceclaimactions.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|resourceName|query|string|false|none|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "resourceClaimId": 0,
    "resourceName": "string",
    "claimName": "string",
    "actions": [
      {
        "name": "string"
      }
    ]
  }
]
```

<h3 id="retrieves-all-resourceclaimactions.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-resourceclaimactions.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[resourceClaimActionModel](#schemaresourceclaimactionmodel)]|false|none|none|
|» resourceClaimId|integer(int32)|false|none|none|
|» resourceName|string¦null|false|none|none|
|» claimName|string¦null|false|none|none|
|» actions|[[actionForResourceClaimModel](#schemaactionforresourceclaimmodel)]¦null|false|none|none|
|»» name|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-resourceclaimactionauthstrategies">ResourceClaimActionAuthStrategies</h1>

## Retrieves all resourceClaimActionAuthStrategies.

`GET /v2/resourceClaimActionAuthStrategies`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-resourceclaimactionauthstrategies.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|resourceName|query|string|false|none|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "resourceClaimId": 0,
    "resourceName": "string",
    "claimName": "string",
    "authorizationStrategiesForActions": [
      {
        "actionId": 0,
        "actionName": "string",
        "authorizationStrategies": [
          {
            "authStrategyId": 0,
            "authStrategyName": "string"
          }
        ]
      }
    ]
  }
]
```

<h3 id="retrieves-all-resourceclaimactionauthstrategies.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-resourceclaimactionauthstrategies.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[resourceClaimActionAuthStrategyModel](#schemaresourceclaimactionauthstrategymodel)]|false|none|none|
|» resourceClaimId|integer(int32)|false|none|none|
|» resourceName|string¦null|false|none|none|
|» claimName|string¦null|false|none|none|
|» authorizationStrategiesForActions|[[actionWithAuthorizationStrategy](#schemaactionwithauthorizationstrategy)]¦null|false|none|none|
|»» actionId|integer(int32)|false|none|none|
|»» actionName|string¦null|false|none|none|
|»» authorizationStrategies|[[authorizationStrategyModelForAction](#schemaauthorizationstrategymodelforaction)]¦null|false|none|none|
|»»» authStrategyId|integer(int32)|false|none|none|
|»»» authStrategyName|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-vendors">Vendors</h1>

## Retrieves all vendors.

`GET /v2/vendors`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-vendors.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|Vendor/ company id|
|company|query|string|false|Vendor/ company name|
|namespacePrefixes|query|string|false|Namespace prefix for the vendor. Multiple namespace prefixes can be provided as comma separated list if required.|
|contactName|query|string|false|Vendor contact name|
|contactEmailAddress|query|string|false|Vendor contact email id|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "company": "string",
    "namespacePrefixes": "string",
    "contactName": "string",
    "contactEmailAddress": "string"
  }
]
```

<h3 id="retrieves-all-vendors.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-vendors.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[vendorModel](#schemavendormodel)]|false|none|none|
|» Vendor|[vendorModel](#schemavendormodel)|false|none|none|
|»» id|integer(int32)¦null|false|none|none|
|»» company|string¦null|false|none|none|
|»» namespacePrefixes|string¦null|false|none|none|
|»» contactName|string¦null|false|none|none|
|»» contactEmailAddress|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates vendor based on the supplied values.

`POST /v2/vendors`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "company": "string",
  "namespacePrefixes": "string",
  "contactName": "string",
  "contactEmailAddress": "string"
}
```

<h3 id="creates-vendor-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addVendorRequest](#schemaaddvendorrequest)|true|none|

<h3 id="creates-vendor-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific vendor based on the identifier.

`GET /v2/vendors/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-vendor-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "company": "string",
  "namespacePrefixes": "string",
  "contactName": "string",
  "contactEmailAddress": "string"
}
```

<h3 id="retrieves-a-specific-vendor-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[vendorModel](#schemavendormodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates vendor based on the resource identifier.

`PUT /v2/vendors/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "company": "string",
  "namespacePrefixes": "string",
  "contactName": "string",
  "contactEmailAddress": "string"
}
```

<h3 id="updates-vendor-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editVendorRequest](#schemaeditvendorrequest)|true|none|

<h3 id="updates-vendor-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing vendor using the resource identifier.

`DELETE /v2/vendors/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-vendor-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-vendor-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves applications assigned to a specific vendor based on the resource identifier.

`GET /v2/vendors/{id}/applications`

<h3 id="retrieves-applications-assigned-to-a-specific-vendor-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "applicationName": "string",
    "claimSetName": "string",
    "educationOrganizationIds": [
      0
    ],
    "vendorId": 0,
    "profileIds": [
      0
    ],
    "odsInstanceIds": [
      0
    ],
    "enabled": true
  }
]
```

<h3 id="retrieves-applications-assigned-to-a-specific-vendor-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-applications-assigned-to-a-specific-vendor-based-on-the-resource-identifier.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[applicationModel](#schemaapplicationmodel)]|false|none|none|
|» Application|[applicationModel](#schemaapplicationmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» applicationName|string¦null|false|none|none|
|»» claimSetName|string¦null|false|none|none|
|»» educationOrganizationIds|[integer]¦null|false|none|none|
|»» vendorId|integer(int32)¦null|false|none|none|
|»» profileIds|[integer]¦null|false|none|none|
|»» odsInstanceIds|[integer]¦null|false|none|none|
|»» enabled|boolean|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-profiles">Profiles</h1>

## Retrieves all profiles.

`GET /v2/profiles`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-profiles.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|Profile id|
|name|query|string|false|Profile name|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "name": "string"
  }
]
```

<h3 id="retrieves-all-profiles.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-profiles.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[profileModel](#schemaprofilemodel)]|false|none|none|
|» Profile|[profileModel](#schemaprofilemodel)|false|none|none|
|»» id|integer(int32)¦null|false|none|none|
|»» name|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates profile based on the supplied values.

`POST /v2/profiles`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
"{\n  \"name\": \"Test-Profile\",\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\n}"
```

<h3 id="creates-profile-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addProfileRequest](#schemaaddprofilerequest)|true|none|

<h3 id="creates-profile-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific profile based on the identifier.

`GET /v2/profiles/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-profile-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "name": "string",
  "definition": "string"
}
```

<h3 id="retrieves-a-specific-profile-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[profileDetailsModel](#schemaprofiledetailsmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates profile based on the resource identifier.

`PUT /v2/profiles/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
"{\n  \"name\": \"Test-Profile\",\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\n}"
```

<h3 id="updates-profile-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editProfileRequest](#schemaeditprofilerequest)|true|none|

<h3 id="updates-profile-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing profile using the resource identifier.

`DELETE /v2/profiles/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-profile-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-profile-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-odsinstances">OdsInstances</h1>

## Retrieves all odsInstances.

`GET /v2/odsInstances`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-odsinstances.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|List of ODS instance id|
|name|query|string|false|Ods Instance name|
|instanceType|query|string|false|none|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "name": "string",
    "instanceType": "string"
  }
]
```

<h3 id="retrieves-all-odsinstances.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-odsinstances.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[odsInstanceModel](#schemaodsinstancemodel)]|false|none|none|
|» OdsInstance|[odsInstanceModel](#schemaodsinstancemodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|false|none|none|
|»» instanceType|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates odsInstance based on the supplied values.

`POST /v2/odsInstances`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "name": "string",
  "instanceType": "string",
  "connectionString": "string"
}
```

<h3 id="creates-odsinstance-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addOdsInstanceRequest](#schemaaddodsinstancerequest)|true|none|

<h3 id="creates-odsinstance-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific odsInstance based on the identifier.

`GET /v2/odsInstances/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-odsinstance-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "name": "string",
  "instanceType": "string",
  "odsInstanceContexts": [
    {
      "id": 0,
      "odsInstanceId": 0,
      "contextKey": "string",
      "contextValue": "string"
    }
  ],
  "odsInstanceDerivatives": [
    {
      "id": 0,
      "odsInstanceId": 0,
      "derivativeType": "string"
    }
  ]
}
```

<h3 id="retrieves-a-specific-odsinstance-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[odsInstanceDetailModel](#schemaodsinstancedetailmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates odsInstance based on the resource identifier.

`PUT /v2/odsInstances/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "name": "string",
  "instanceType": "string",
  "connectionString": "string"
}
```

<h3 id="updates-odsinstance-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editOdsInstanceRequest](#schemaeditodsinstancerequest)|true|none|

<h3 id="updates-odsinstance-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing odsInstance using the resource identifier.

`DELETE /v2/odsInstances/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-odsinstance-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-odsinstance-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves applications assigned to a specific ODS instance based on the resource identifier.

`GET /v2/odsInstances/{id}/applications`

<h3 id="retrieves-applications-assigned-to-a-specific-ods-instance-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "applicationName": "string",
    "claimSetName": "string",
    "educationOrganizationIds": [
      0
    ],
    "vendorId": 0,
    "profileIds": [
      0
    ],
    "odsInstanceIds": [
      0
    ],
    "enabled": true
  }
]
```

<h3 id="retrieves-applications-assigned-to-a-specific-ods-instance-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-applications-assigned-to-a-specific-ods-instance-based-on-the-resource-identifier.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[applicationModel](#schemaapplicationmodel)]|false|none|none|
|» Application|[applicationModel](#schemaapplicationmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» applicationName|string¦null|false|none|none|
|»» claimSetName|string¦null|false|none|none|
|»» educationOrganizationIds|[integer]¦null|false|none|none|
|»» vendorId|integer(int32)¦null|false|none|none|
|»» profileIds|[integer]¦null|false|none|none|
|»» odsInstanceIds|[integer]¦null|false|none|none|
|»» enabled|boolean|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-odsinstancederivatives">OdsInstanceDerivatives</h1>

## Retrieves all odsInstanceDerivatives.

`GET /v2/odsInstanceDerivatives`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-odsinstancederivatives.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "odsInstanceId": 0,
    "derivativeType": "string"
  }
]
```

<h3 id="retrieves-all-odsinstancederivatives.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-odsinstancederivatives.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[odsInstanceDerivativeModel](#schemaodsinstancederivativemodel)]|false|none|none|
|» OdsInstanceDerivative|[odsInstanceDerivativeModel](#schemaodsinstancederivativemodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» odsInstanceId|integer(int32)¦null|false|none|none|
|»» derivativeType|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates odsInstanceDerivative based on the supplied values.

`POST /v2/odsInstanceDerivatives`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "odsInstanceId": 0,
  "derivativeType": "string",
  "connectionString": "string"
}
```

<h3 id="creates-odsinstancederivative-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addOdsInstanceDerivativeRequest](#schemaaddodsinstancederivativerequest)|true|none|

<h3 id="creates-odsinstancederivative-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific odsInstanceDerivative based on the identifier.

`GET /v2/odsInstanceDerivatives/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-odsinstancederivative-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "odsInstanceId": 0,
  "derivativeType": "string"
}
```

<h3 id="retrieves-a-specific-odsinstancederivative-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[odsInstanceDerivativeModel](#schemaodsinstancederivativemodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates odsInstanceDerivative based on the resource identifier.

`PUT /v2/odsInstanceDerivatives/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "odsInstanceId": 0,
  "derivativeType": "string",
  "connectionString": "string"
}
```

<h3 id="updates-odsinstancederivative-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editOdsInstanceDerivativeRequest](#schemaeditodsinstancederivativerequest)|true|none|

<h3 id="updates-odsinstancederivative-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing odsInstanceDerivative using the resource identifier.

`DELETE /v2/odsInstanceDerivatives/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-odsinstancederivative-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-odsinstancederivative-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-odsinstancecontexts">OdsInstanceContexts</h1>

## Retrieves all odsInstanceContexts.

`GET /v2/odsInstanceContexts`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-odsinstancecontexts.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "odsInstanceId": 0,
    "contextKey": "string",
    "contextValue": "string"
  }
]
```

<h3 id="retrieves-all-odsinstancecontexts.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-odsinstancecontexts.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[odsInstanceContextModel](#schemaodsinstancecontextmodel)]|false|none|none|
|» OdsInstanceContext|[odsInstanceContextModel](#schemaodsinstancecontextmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» odsInstanceId|integer(int32)|false|none|none|
|»» contextKey|string¦null|false|none|none|
|»» contextValue|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates odsInstanceContext based on the supplied values.

`POST /v2/odsInstanceContexts`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "odsInstanceId": 0,
  "contextKey": "string",
  "contextValue": "string"
}
```

<h3 id="creates-odsinstancecontext-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addOdsInstanceContextRequest](#schemaaddodsinstancecontextrequest)|true|none|

<h3 id="creates-odsinstancecontext-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific odsInstanceContext based on the identifier.

`GET /v2/odsInstanceContexts/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-odsinstancecontext-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "odsInstanceId": 0,
  "contextKey": "string",
  "contextValue": "string"
}
```

<h3 id="retrieves-a-specific-odsinstancecontext-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[odsInstanceContextModel](#schemaodsinstancecontextmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates odsInstanceContext based on the resource identifier.

`PUT /v2/odsInstanceContexts/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "odsInstanceId": 0,
  "contextKey": "string",
  "contextValue": "string"
}
```

<h3 id="updates-odsinstancecontext-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editOdsInstanceContextRequest](#schemaeditodsinstancecontextrequest)|true|none|

<h3 id="updates-odsinstancecontext-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing odsInstanceContext using the resource identifier.

`DELETE /v2/odsInstanceContexts/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-odsinstancecontext-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-odsinstancecontext-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-claimsets">ClaimSets</h1>

## Exports a specific claimset by id

`GET /v2/claimSets/{id}/export`

<h3 id="exports-a-specific-claimset-by-id-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "name": "string",
  "_isSystemReserved": true,
  "_applications": [
    {
      "applicationName": "string"
    }
  ],
  "resourceClaims": [
    {
      "id": 0,
      "name": "string",
      "actions": [
        {
          "name": "string",
          "enabled": true
        }
      ],
      "_defaultAuthorizationStrategiesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "authorizationStrategyOverridesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "children": [
        {}
      ]
    }
  ]
}
```

<h3 id="exports-a-specific-claimset-by-id-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[claimSetDetailsModel](#schemaclaimsetdetailsmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves all claimSets.

`GET /v2/claimSets`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-claimsets.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|Claim set id|
|name|query|string|false|Claim set name|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "name": "string",
    "_isSystemReserved": true,
    "_applications": [
      {
        "applicationName": "string"
      }
    ]
  }
]
```

<h3 id="retrieves-all-claimsets.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-claimsets.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[claimSetModel](#schemaclaimsetmodel)]|false|none|none|
|» ClaimSet|[claimSetModel](#schemaclaimsetmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|false|none|none|
|»» _isSystemReserved|boolean|false|read-only|none|
|»» _applications|[[simpleApplicationModel](#schemasimpleapplicationmodel)]¦null|false|read-only|none|
|»»» Application|[simpleApplicationModel](#schemasimpleapplicationmodel)|false|none|none|
|»»»» applicationName|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates claimSet based on the supplied values.

`POST /v2/claimSets`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "name": "string"
}
```

<h3 id="creates-claimset-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addClaimSetRequest](#schemaaddclaimsetrequest)|true|none|

<h3 id="creates-claimset-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific claimSet based on the identifier.

`GET /v2/claimSets/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-claimset-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "name": "string",
  "_isSystemReserved": true,
  "_applications": [
    {
      "applicationName": "string"
    }
  ],
  "resourceClaims": [
    {
      "id": 0,
      "name": "string",
      "actions": [
        {
          "name": "string",
          "enabled": true
        }
      ],
      "_defaultAuthorizationStrategiesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "authorizationStrategyOverridesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "children": [
        {}
      ]
    }
  ]
}
```

<h3 id="retrieves-a-specific-claimset-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[claimSetDetailsModel](#schemaclaimsetdetailsmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates claimSet based on the resource identifier.

`PUT /v2/claimSets/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "name": "string"
}
```

<h3 id="updates-claimset-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editClaimSetRequest](#schemaeditclaimsetrequest)|true|none|

<h3 id="updates-claimset-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing claimSet using the resource identifier.

`DELETE /v2/claimSets/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-claimset-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-claimset-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Copies the existing claimset and create a new one.

`POST /v2/claimSets/copy`

> Body parameter

```json
{
  "originalId": 0,
  "name": "string"
}
```

<h3 id="copies-the-existing-claimset-and-create-a-new-one.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[copyClaimSetRequest](#schemacopyclaimsetrequest)|true|none|

<h3 id="copies-the-existing-claimset-and-create-a-new-one.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Imports a new claimset

`POST /v2/claimSets/import`

> Body parameter

```json
{
  "name": "string",
  "resourceClaims": [
    {
      "name": "string",
      "actions": [
        {
          "name": "string",
          "enabled": true
        }
      ],
      "authorizationStrategyOverridesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "children": [
        {}
      ]
    }
  ]
}
```

<h3 id="imports-a-new-claimset-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[importClaimSetRequest](#schemaimportclaimsetrequest)|true|none|

<h3 id="imports-a-new-claimset-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Overrides the default authorization strategies on provided resource claim for a specific action.

`POST /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/overrideAuthorizationStrategy`

Override the default authorization strategies on provided resource claim for a specific action.

ex: actionName = read,  authorizationStrategies= [ "Ownershipbased" ]

> Body parameter

```json
{
  "actionName": "string",
  "authorizationStrategies": [
    "string"
  ]
}
```

<h3 id="overrides-the-default-authorization-strategies-on-provided-resource-claim-for-a-specific-action.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|resourceClaimId|path|integer(int32)|true|none|
|body|body|[overrideAuthStategyOnClaimSetRequest](#schemaoverrideauthstategyonclaimsetrequest)|true|none|

<h3 id="overrides-the-default-authorization-strategies-on-provided-resource-claim-for-a-specific-action.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Resets to default authorization strategies on provided resource claim.

`POST /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/resetAuthorizationStrategies`

<h3 id="resets-to-default-authorization-strategies-on-provided-resource-claim.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|resourceClaimId|path|integer(int32)|true|none|

<h3 id="resets-to-default-authorization-strategies-on-provided-resource-claim.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Adds ResourceClaimAction association to a claim set.

`POST /v2/claimSets/{claimSetId}/resourceClaimActions`

Add resourceClaimAction association to claim set. At least one action should be enabled. Valid actions are read, create, update, delete, readchanges.
resouceclaimId is required fields.

> Body parameter

```json
{
  "resourceClaimId": 0,
  "resourceClaimActions": [
    {
      "name": "string",
      "enabled": true
    }
  ]
}
```

<h3 id="adds-resourceclaimaction-association-to-a-claim-set.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|body|body|[addResourceClaimOnClaimSetRequest](#schemaaddresourceclaimonclaimsetrequest)|true|none|

<h3 id="adds-resourceclaimaction-association-to-a-claim-set.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates the ResourceClaimActions to a specific resource claim on a claimset.

`PUT /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}`

Updates  the resourceClaimActions to a  specific resource claim on a claimset. At least one action should be enabled. Valid actions are read, create, update, delete, readchanges.

> Body parameter

```json
{
  "resourceClaimActions": [
    {
      "name": "string",
      "enabled": true
    }
  ]
}
```

<h3 id="updates-the-resourceclaimactions-to-a-specific-resource-claim-on-a-claimset.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|resourceClaimId|path|integer(int32)|true|none|
|body|body|[editResourceClaimOnClaimSetRequest](#schemaeditresourceclaimonclaimsetrequest)|true|none|

<h3 id="updates-the-resourceclaimactions-to-a-specific-resource-claim-on-a-claimset.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes a resource claims association from a claimset

`DELETE /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}`

<h3 id="deletes-a-resource-claims-association-from-a-claimset-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|resourceClaimId|path|integer(int32)|true|none|

<h3 id="deletes-a-resource-claims-association-from-a-claimset-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-authorizationstrategies">AuthorizationStrategies</h1>

## Retrieves all authorizationStrategies.

`GET /v2/authorizationStrategies`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-authorizationstrategies.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "name": "string",
    "displayName": "string"
  }
]
```

<h3 id="retrieves-all-authorizationstrategies.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-authorizationstrategies.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[authorizationStrategyModel](#schemaauthorizationstrategymodel)]|false|none|none|
|» AuthorizationStrategy|[authorizationStrategyModel](#schemaauthorizationstrategymodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|false|none|none|
|»» displayName|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-applications">Applications</h1>

## Retrieves all applications.

`GET /v2/applications`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-applications.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|Application id|
|applicationName|query|string|false|Application name|
|claimsetName|query|string|false|Claim set name|
|ids|query|string|false|none|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "applicationName": "string",
    "claimSetName": "string",
    "educationOrganizationIds": [
      0
    ],
    "vendorId": 0,
    "profileIds": [
      0
    ],
    "odsInstanceIds": [
      0
    ],
    "enabled": true
  }
]
```

<h3 id="retrieves-all-applications.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-applications.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[applicationModel](#schemaapplicationmodel)]|false|none|none|
|» Application|[applicationModel](#schemaapplicationmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» applicationName|string¦null|false|none|none|
|»» claimSetName|string¦null|false|none|none|
|»» educationOrganizationIds|[integer]¦null|false|none|none|
|»» vendorId|integer(int32)¦null|false|none|none|
|»» profileIds|[integer]¦null|false|none|none|
|»» odsInstanceIds|[integer]¦null|false|none|none|
|»» enabled|boolean|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates application based on the supplied values.

`POST /v2/applications`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "applicationName": "string",
  "vendorId": 0,
  "claimSetName": "string",
  "profileIds": [
    0
  ],
  "educationOrganizationIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ],
  "enabled": true
}
```

<h3 id="creates-application-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addApplicationRequest](#schemaaddapplicationrequest)|true|none|

> Example responses

> 201 Response

```json
{
  "id": 0,
  "key": "string",
  "secret": "string"
}
```

<h3 id="creates-application-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|[applicationResult](#schemaapplicationresult)|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific application based on the identifier.

`GET /v2/applications/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-application-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "applicationName": "string",
  "claimSetName": "string",
  "educationOrganizationIds": [
    0
  ],
  "vendorId": 0,
  "profileIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ],
  "enabled": true
}
```

<h3 id="retrieves-a-specific-application-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[applicationModel](#schemaapplicationmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates application based on the resource identifier.

`PUT /v2/applications/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "applicationName": "string",
  "vendorId": 0,
  "claimSetName": "string",
  "profileIds": [
    0
  ],
  "educationOrganizationIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ],
  "enabled": true
}
```

<h3 id="updates-application-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editApplicationRequest](#schemaeditapplicationrequest)|true|none|

<h3 id="updates-application-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing application using the resource identifier.

`DELETE /v2/applications/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-application-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-application-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Reset application credentials. Returns new key and secret.

`PUT /v2/applications/{id}/reset-credential`

<h3 id="reset-application-credentials.-returns-new-key-and-secret.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "key": "string",
  "secret": "string"
}
```

<h3 id="reset-application-credentials.-returns-new-key-and-secret.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[applicationResult](#schemaapplicationresult)|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-apiclients">Apiclients</h1>

## Retrieves all apiclients.

`GET /v2/apiclients`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-apiclients.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|applicationid|query|integer(int32)|true|none|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "key": "string",
    "name": "string",
    "isApproved": true,
    "useSandbox": true,
    "sandboxType": 0,
    "applicationId": 0,
    "keyStatus": "string",
    "educationOrganizationIds": [
      0
    ],
    "odsInstanceIds": [
      0
    ]
  }
]
```

<h3 id="retrieves-all-apiclients.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-apiclients.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[apiClientModel](#schemaapiclientmodel)]|false|none|none|
|» ApiClient|[apiClientModel](#schemaapiclientmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» key|string¦null|false|none|none|
|»» name|string¦null|false|none|none|
|»» isApproved|boolean|false|none|none|
|»» useSandbox|boolean|false|none|none|
|»» sandboxType|integer(int32)|false|none|none|
|»» applicationId|integer(int32)|false|none|none|
|»» keyStatus|string¦null|false|none|none|
|»» educationOrganizationIds|[integer]¦null|false|none|none|
|»» odsInstanceIds|[integer]¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates apiclient based on the supplied values.

`POST /v2/apiclients`

The POST operation can be used to create or update resources. In database terms, this is often referred to as an "upsert" operation (insert + update). Clients should NOT include the resource "id" in the JSON body because it will result in an error. The web service will identify whether the resource already exists based on the natural key values provided, and update or create the resource appropriately. It is recommended to use POST for both create and update except while updating natural key of a resource in which case PUT operation must be used.

> Body parameter

```json
{
  "name": "string",
  "isApproved": true,
  "applicationId": 0,
  "odsInstanceIds": [
    0
  ]
}
```

<h3 id="creates-apiclient-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addApiClientRequest](#schemaaddapiclientrequest)|true|none|

> Example responses

> 201 Response

```json
{
  "id": 0,
  "name": "string",
  "key": "string",
  "secret": "string",
  "applicationId": 0
}
```

<h3 id="creates-apiclient-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|[apiClientResult](#schemaapiclientresult)|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific apiclient based on the identifier.

`GET /v2/apiclients/{id}`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-a-specific-apiclient-based-on-the-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "key": "string",
  "name": "string",
  "isApproved": true,
  "useSandbox": true,
  "sandboxType": 0,
  "applicationId": 0,
  "keyStatus": "string",
  "educationOrganizationIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ]
}
```

<h3 id="retrieves-a-specific-apiclient-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[apiClientModel](#schemaapiclientmodel)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates apiclient based on the resource identifier.

`PUT /v2/apiclients/{id}`

The PUT operation is used to update a resource by identifier. If the resource identifier ("id") is provided in the JSON body, it will be ignored. Additionally, this API resource is not configured for cascading natural key updates. Natural key values for this resource cannot be changed using PUT operation, so the recommendation is to use POST as that supports upsert behavior.

> Body parameter

```json
{
  "name": "string",
  "isApproved": true,
  "applicationId": 0,
  "odsInstanceIds": [
    0
  ]
}
```

<h3 id="updates-apiclient-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editApiClientRequest](#schemaeditapiclientrequest)|true|none|

<h3 id="updates-apiclient-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing apiclient using the resource identifier.

`DELETE /v2/apiclients/{id}`

The DELETE operation is used to delete an existing resource by identifier. If the resource doesn't exist, an error will result (the resource will not be found).

<h3 id="deletes-an-existing-apiclient-using-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

<h3 id="deletes-an-existing-apiclient-using-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Resource was successfully deleted.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Reset apiclient credentials. Returns new key and secret.

`PUT /v2/apiclients/{id}/reset-credential`

<h3 id="reset-apiclient-credentials.-returns-new-key-and-secret.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|

> Example responses

> 200 Response

```json
{
  "id": 0,
  "name": "string",
  "key": "string",
  "secret": "string",
  "applicationId": 0
}
```

<h3 id="reset-apiclient-credentials.-returns-new-key-and-secret.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[apiClientResult](#schemaapiclientresult)|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-actions">Actions</h1>

## Retrieves all actions.

`GET /v2/actions`

This GET operation provides access to resources using the "Get" search pattern. The values of any properties of the resource that are specified will be used to return all matching results (if it exists).

<h3 id="retrieves-all-actions.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|false|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|false|Indicates the maximum number of items that should be returned in the results.|
|orderBy|query|string|false|Indicates the property name by which the results will be sorted.|
|direction|query|string|false|Indicates whether the result should be sorted in descending order (DESC) or ascending order (ASC).|
|id|query|integer(int32)|false|Action id|
|name|query|string|false|Action name|

#### Enumerated Values

|Parameter|Value|
|---|---|
|direction|Ascending|
|direction|Descending|

> Example responses

> 200 Response

```json
[
  {
    "id": 0,
    "name": "string",
    "uri": "string"
  }
]
```

<h3 id="retrieves-all-actions.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|409|[Conflict](https://tools.ietf.org/html/rfc7231#section-6.5.8)|Conflict. The request is authenticated, but it has a conflict with an existing element|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-actions.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[actionModel](#schemaactionmodel)]|false|none|none|
|» Action|[actionModel](#schemaactionmodel)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|false|none|none|
|»» uri|string¦null|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-information">Information</h1>

## Retrieve API informational metadata

`GET /`

> Example responses

> 200 Response

```json
{
  "version": "string",
  "build": "string"
}
```

<h3 id="retrieve-api-informational-metadata-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|OK|[informationResult](#schemainformationresult)|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|[informationResult](#schemainformationresult)|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="admin-api-documentation-connect">Connect</h1>

## Registers new client

`POST /connect/register`

Registers new client

> Body parameter

```yaml
ClientId: string
ClientSecret: string
DisplayName: string

```

<h3 id="registers-new-client-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|object|false|none|
|» ClientId|body|string|false|Client id|
|» ClientSecret|body|string|false|Client secret|
|» DisplayName|body|string|false|Client display name|

<h3 id="registers-new-client-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Application registered successfully.|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves bearer token

`POST /connect/token`

To authenticate Swagger requests, execute using "Authorize" above, not "Try It Out" here.

> Body parameter

```yaml
client_id: null
client_secret: null
grant_type: null
scope: string

```

<h3 id="retrieves-bearer-token-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|object|false|none|
|» client_id|body|string |false|none|
|» client_secret|body|string |false|none|
|» grant_type|body|string |false|none|
|» scope|body|string|false|none|

<h3 id="retrieves-bearer-token-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Sign-in successful.|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad request, such as invalid scope.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

# Schemas

<h2 id="tocS_actionForResourceClaimModel">actionForResourceClaimModel</h2>
<!-- backwards compatibility -->
<a id="schemaactionforresourceclaimmodel"></a>
<a id="schema_actionForResourceClaimModel"></a>
<a id="tocSactionforresourceclaimmodel"></a>
<a id="tocsactionforresourceclaimmodel"></a>

```json
{
  "name": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string¦null|false|none|none|

<h2 id="tocS_actionModel">actionModel</h2>
<!-- backwards compatibility -->
<a id="schemaactionmodel"></a>
<a id="schema_actionModel"></a>
<a id="tocSactionmodel"></a>
<a id="tocsactionmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "uri": "string"
}

```

Action

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|uri|string¦null|false|none|none|

<h2 id="tocS_actionWithAuthorizationStrategy">actionWithAuthorizationStrategy</h2>
<!-- backwards compatibility -->
<a id="schemaactionwithauthorizationstrategy"></a>
<a id="schema_actionWithAuthorizationStrategy"></a>
<a id="tocSactionwithauthorizationstrategy"></a>
<a id="tocsactionwithauthorizationstrategy"></a>

```json
{
  "actionId": 0,
  "actionName": "string",
  "authorizationStrategies": [
    {
      "authStrategyId": 0,
      "authStrategyName": "string"
    }
  ]
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|actionId|integer(int32)|false|none|none|
|actionName|string¦null|false|none|none|
|authorizationStrategies|[[authorizationStrategyModelForAction](#schemaauthorizationstrategymodelforaction)]¦null|false|none|none|

<h2 id="tocS_addApiClientRequest">addApiClientRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddapiclientrequest"></a>
<a id="schema_addApiClientRequest"></a>
<a id="tocSaddapiclientrequest"></a>
<a id="tocsaddapiclientrequest"></a>

```json
{
  "name": "string",
  "isApproved": true,
  "applicationId": 0,
  "odsInstanceIds": [
    0
  ]
}

```

AddApiClientRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Api client name|
|isApproved|boolean|false|none|Is approved|
|applicationId|integer(int32)|false|none|Application id|
|odsInstanceIds|[integer]|false|none|List of ODS instance id|

<h2 id="tocS_addApplicationRequest">addApplicationRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddapplicationrequest"></a>
<a id="schema_addApplicationRequest"></a>
<a id="tocSaddapplicationrequest"></a>
<a id="tocsaddapplicationrequest"></a>

```json
{
  "applicationName": "string",
  "vendorId": 0,
  "claimSetName": "string",
  "profileIds": [
    0
  ],
  "educationOrganizationIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ],
  "enabled": true
}

```

AddApplicationRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|applicationName|string|false|none|Application name|
|vendorId|integer(int32)|false|none|Vendor/ company id|
|claimSetName|string|false|none|Claim set name|
|profileIds|[integer]¦null|false|none|Profile id|
|educationOrganizationIds|[integer]|false|none|Education organization ids|
|odsInstanceIds|[integer]|false|none|List of ODS instance id|
|enabled|boolean¦null|false|none|Indicates whether the ApiClient's credetials is enabled. Defaults to true if not provided.|

<h2 id="tocS_addClaimSetRequest">addClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddclaimsetrequest"></a>
<a id="schema_addClaimSetRequest"></a>
<a id="tocSaddclaimsetrequest"></a>
<a id="tocsaddclaimsetrequest"></a>

```json
{
  "name": "string"
}

```

AddClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Claim set name|

<h2 id="tocS_addOdsInstanceContextRequest">addOdsInstanceContextRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddodsinstancecontextrequest"></a>
<a id="schema_addOdsInstanceContextRequest"></a>
<a id="tocSaddodsinstancecontextrequest"></a>
<a id="tocsaddodsinstancecontextrequest"></a>

```json
{
  "odsInstanceId": 0,
  "contextKey": "string",
  "contextValue": "string"
}

```

AddOdsInstanceContextRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|odsInstanceId|integer(int32)|false|none|ODS instance context ODS instance id.|
|contextKey|string|false|none|context key.|
|contextValue|string|false|none|context value.|

<h2 id="tocS_addOdsInstanceDerivativeRequest">addOdsInstanceDerivativeRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddodsinstancederivativerequest"></a>
<a id="schema_addOdsInstanceDerivativeRequest"></a>
<a id="tocSaddodsinstancederivativerequest"></a>
<a id="tocsaddodsinstancederivativerequest"></a>

```json
{
  "odsInstanceId": 0,
  "derivativeType": "string",
  "connectionString": "string"
}

```

AddOdsInstanceDerivativeRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|odsInstanceId|integer(int32)|false|none|ODS instance derivative ODS instance id.|
|derivativeType|string|false|none|derivative type.|
|connectionString|string|false|none|connection string.|

<h2 id="tocS_addOdsInstanceRequest">addOdsInstanceRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddodsinstancerequest"></a>
<a id="schema_addOdsInstanceRequest"></a>
<a id="tocSaddodsinstancerequest"></a>
<a id="tocsaddodsinstancerequest"></a>

```json
{
  "name": "string",
  "instanceType": "string",
  "connectionString": "string"
}

```

AddOdsInstanceRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Ods Instance name|
|instanceType|string¦null|false|none|Ods Instance type|
|connectionString|string|false|none|Ods Instance connection string|

<h2 id="tocS_addProfileRequest">addProfileRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddprofilerequest"></a>
<a id="schema_addProfileRequest"></a>
<a id="tocSaddprofilerequest"></a>
<a id="tocsaddprofilerequest"></a>

```json
"{\n  \"name\": \"Test-Profile\",\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\n}"

```

AddProfileRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Profile name|
|definition|string|false|none|Profile definition|

<h2 id="tocS_addResourceClaimOnClaimSetRequest">addResourceClaimOnClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddresourceclaimonclaimsetrequest"></a>
<a id="schema_addResourceClaimOnClaimSetRequest"></a>
<a id="tocSaddresourceclaimonclaimsetrequest"></a>
<a id="tocsaddresourceclaimonclaimsetrequest"></a>

```json
{
  "resourceClaimId": 0,
  "resourceClaimActions": [
    {
      "name": "string",
      "enabled": true
    }
  ]
}

```

AddResourceClaimActionsOnClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|resourceClaimId|integer(int32)|false|none|ResourceClaim id|
|resourceClaimActions|[[resourceClaimAction](#schemaresourceclaimaction)]|false|none|none|

<h2 id="tocS_addVendorRequest">addVendorRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddvendorrequest"></a>
<a id="schema_addVendorRequest"></a>
<a id="tocSaddvendorrequest"></a>
<a id="tocsaddvendorrequest"></a>

```json
{
  "company": "string",
  "namespacePrefixes": "string",
  "contactName": "string",
  "contactEmailAddress": "string"
}

```

AddVendorRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|company|string|false|none|Vendor/ company name|
|namespacePrefixes|string|false|none|Namespace prefix for the vendor. Multiple namespace prefixes can be provided as comma separated list if required.|
|contactName|string|false|none|Vendor contact name|
|contactEmailAddress|string|false|none|Vendor contact email id|

<h2 id="tocS_adminApiError">adminApiError</h2>
<!-- backwards compatibility -->
<a id="schemaadminapierror"></a>
<a id="schema_adminApiError"></a>
<a id="tocSadminapierror"></a>
<a id="tocsadminapierror"></a>

```json
{}

```

AdminApiError

### Properties

*None*

<h2 id="tocS_apiClientModel">apiClientModel</h2>
<!-- backwards compatibility -->
<a id="schemaapiclientmodel"></a>
<a id="schema_apiClientModel"></a>
<a id="tocSapiclientmodel"></a>
<a id="tocsapiclientmodel"></a>

```json
{
  "id": 0,
  "key": "string",
  "name": "string",
  "isApproved": true,
  "useSandbox": true,
  "sandboxType": 0,
  "applicationId": 0,
  "keyStatus": "string",
  "educationOrganizationIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ]
}

```

ApiClient

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|key|string¦null|false|none|none|
|name|string¦null|false|none|none|
|isApproved|boolean|false|none|none|
|useSandbox|boolean|false|none|none|
|sandboxType|integer(int32)|false|none|none|
|applicationId|integer(int32)|false|none|none|
|keyStatus|string¦null|false|none|none|
|educationOrganizationIds|[integer]¦null|false|none|none|
|odsInstanceIds|[integer]¦null|false|none|none|

<h2 id="tocS_apiClientResult">apiClientResult</h2>
<!-- backwards compatibility -->
<a id="schemaapiclientresult"></a>
<a id="schema_apiClientResult"></a>
<a id="tocSapiclientresult"></a>
<a id="tocsapiclientresult"></a>

```json
{
  "id": 0,
  "name": "string",
  "key": "string",
  "secret": "string",
  "applicationId": 0
}

```

ApiClient

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|key|string¦null|false|none|none|
|secret|string¦null|false|none|none|
|applicationId|integer(int32)|false|none|none|

<h2 id="tocS_applicationModel">applicationModel</h2>
<!-- backwards compatibility -->
<a id="schemaapplicationmodel"></a>
<a id="schema_applicationModel"></a>
<a id="tocSapplicationmodel"></a>
<a id="tocsapplicationmodel"></a>

```json
{
  "id": 0,
  "applicationName": "string",
  "claimSetName": "string",
  "educationOrganizationIds": [
    0
  ],
  "vendorId": 0,
  "profileIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ],
  "enabled": true
}

```

Application

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|applicationName|string¦null|false|none|none|
|claimSetName|string¦null|false|none|none|
|educationOrganizationIds|[integer]¦null|false|none|none|
|vendorId|integer(int32)¦null|false|none|none|
|profileIds|[integer]¦null|false|none|none|
|odsInstanceIds|[integer]¦null|false|none|none|
|enabled|boolean|false|none|none|

<h2 id="tocS_applicationResult">applicationResult</h2>
<!-- backwards compatibility -->
<a id="schemaapplicationresult"></a>
<a id="schema_applicationResult"></a>
<a id="tocSapplicationresult"></a>
<a id="tocsapplicationresult"></a>

```json
{
  "id": 0,
  "key": "string",
  "secret": "string"
}

```

ApplicationKeySecret

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|key|string¦null|false|none|none|
|secret|string¦null|false|none|none|

<h2 id="tocS_authorizationStrategy">authorizationStrategy</h2>
<!-- backwards compatibility -->
<a id="schemaauthorizationstrategy"></a>
<a id="schema_authorizationStrategy"></a>
<a id="tocSauthorizationstrategy"></a>
<a id="tocsauthorizationstrategy"></a>

```json
{
  "authStrategyId": 0,
  "authStrategyName": "string",
  "isInheritedFromParent": true
}

```

ResourceClaimAuthorizationStrategy

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|authStrategyId|integer(int32)|false|none|none|
|authStrategyName|string¦null|false|none|none|
|isInheritedFromParent|boolean|false|none|none|

<h2 id="tocS_authorizationStrategyModel">authorizationStrategyModel</h2>
<!-- backwards compatibility -->
<a id="schemaauthorizationstrategymodel"></a>
<a id="schema_authorizationStrategyModel"></a>
<a id="tocSauthorizationstrategymodel"></a>
<a id="tocsauthorizationstrategymodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "displayName": "string"
}

```

AuthorizationStrategy

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|displayName|string¦null|false|none|none|

<h2 id="tocS_authorizationStrategyModelForAction">authorizationStrategyModelForAction</h2>
<!-- backwards compatibility -->
<a id="schemaauthorizationstrategymodelforaction"></a>
<a id="schema_authorizationStrategyModelForAction"></a>
<a id="tocSauthorizationstrategymodelforaction"></a>
<a id="tocsauthorizationstrategymodelforaction"></a>

```json
{
  "authStrategyId": 0,
  "authStrategyName": "string"
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|authStrategyId|integer(int32)|false|none|none|
|authStrategyName|string¦null|false|none|none|

<h2 id="tocS_claimSetDetailsModel">claimSetDetailsModel</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetdetailsmodel"></a>
<a id="schema_claimSetDetailsModel"></a>
<a id="tocSclaimsetdetailsmodel"></a>
<a id="tocsclaimsetdetailsmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "_isSystemReserved": true,
  "_applications": [
    {
      "applicationName": "string"
    }
  ],
  "resourceClaims": [
    {
      "id": 0,
      "name": "string",
      "actions": [
        {
          "name": "string",
          "enabled": true
        }
      ],
      "_defaultAuthorizationStrategiesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "authorizationStrategyOverridesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "children": [
        {}
      ]
    }
  ]
}

```

ClaimSetWithResources

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|_isSystemReserved|boolean|false|read-only|none|
|_applications|[[simpleApplicationModel](#schemasimpleapplicationmodel)]¦null|false|read-only|none|
|resourceClaims|[[claimSetResourceClaimModel](#schemaclaimsetresourceclaimmodel)]¦null|false|none|none|

<h2 id="tocS_claimSetModel">claimSetModel</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetmodel"></a>
<a id="schema_claimSetModel"></a>
<a id="tocSclaimsetmodel"></a>
<a id="tocsclaimsetmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "_isSystemReserved": true,
  "_applications": [
    {
      "applicationName": "string"
    }
  ]
}

```

ClaimSet

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|_isSystemReserved|boolean|false|read-only|none|
|_applications|[[simpleApplicationModel](#schemasimpleapplicationmodel)]¦null|false|read-only|none|

<h2 id="tocS_claimSetResourceClaimActionAuthStrategies">claimSetResourceClaimActionAuthStrategies</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetresourceclaimactionauthstrategies"></a>
<a id="schema_claimSetResourceClaimActionAuthStrategies"></a>
<a id="tocSclaimsetresourceclaimactionauthstrategies"></a>
<a id="tocsclaimsetresourceclaimactionauthstrategies"></a>

```json
{
  "actionId": 0,
  "actionName": "string",
  "authorizationStrategies": [
    {
      "authStrategyId": 0,
      "authStrategyName": "string",
      "isInheritedFromParent": true
    }
  ]
}

```

ClaimSetResourceClaimActionAuthorizationStrategies

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|actionId|integer(int32)¦null|false|none|none|
|actionName|string¦null|false|none|none|
|authorizationStrategies|[[authorizationStrategy](#schemaauthorizationstrategy)]¦null|false|none|none|

<h2 id="tocS_claimSetResourceClaimModel">claimSetResourceClaimModel</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetresourceclaimmodel"></a>
<a id="schema_claimSetResourceClaimModel"></a>
<a id="tocSclaimsetresourceclaimmodel"></a>
<a id="tocsclaimsetresourceclaimmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "actions": [
    {
      "name": "string",
      "enabled": true
    }
  ],
  "_defaultAuthorizationStrategiesForCRUD": [
    {
      "actionId": 0,
      "actionName": "string",
      "authorizationStrategies": [
        {
          "authStrategyId": 0,
          "authStrategyName": "string",
          "isInheritedFromParent": true
        }
      ]
    }
  ],
  "authorizationStrategyOverridesForCRUD": [
    {
      "actionId": 0,
      "actionName": "string",
      "authorizationStrategies": [
        {
          "authStrategyId": 0,
          "authStrategyName": "string",
          "isInheritedFromParent": true
        }
      ]
    }
  ],
  "children": [
    {
      "id": 0,
      "name": "string",
      "actions": [
        {
          "name": "string",
          "enabled": true
        }
      ],
      "_defaultAuthorizationStrategiesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "authorizationStrategyOverridesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "children": []
    }
  ]
}

```

ClaimSetResourceClaim

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|read-only|none|
|name|string¦null|false|none|none|
|actions|[[resourceClaimAction](#schemaresourceclaimaction)]¦null|false|none|none|
|_defaultAuthorizationStrategiesForCRUD|[[claimSetResourceClaimActionAuthStrategies](#schemaclaimsetresourceclaimactionauthstrategies)]¦null|false|read-only|none|
|authorizationStrategyOverridesForCRUD|[[claimSetResourceClaimActionAuthStrategies](#schemaclaimsetresourceclaimactionauthstrategies)]¦null|false|none|none|
|children|[[claimSetResourceClaimModel](#schemaclaimsetresourceclaimmodel)]¦null|false|none|Children are collection of ResourceClaim|

<h2 id="tocS_copyClaimSetRequest">copyClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemacopyclaimsetrequest"></a>
<a id="schema_copyClaimSetRequest"></a>
<a id="tocScopyclaimsetrequest"></a>
<a id="tocscopyclaimsetrequest"></a>

```json
{
  "originalId": 0,
  "name": "string"
}

```

CopyClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|originalId|integer(int32)|false|none|ClaimSet id to copy|
|name|string|false|none|New claimset name|

<h2 id="tocS_editApiClientRequest">editApiClientRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditapiclientrequest"></a>
<a id="schema_editApiClientRequest"></a>
<a id="tocSeditapiclientrequest"></a>
<a id="tocseditapiclientrequest"></a>

```json
{
  "name": "string",
  "isApproved": true,
  "applicationId": 0,
  "odsInstanceIds": [
    0
  ]
}

```

EditApiClientRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Api client name|
|isApproved|boolean|false|none|Is approved|
|applicationId|integer(int32)|false|none|Application id|
|odsInstanceIds|[integer]|false|none|List of ODS instance id|

<h2 id="tocS_editApplicationRequest">editApplicationRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditapplicationrequest"></a>
<a id="schema_editApplicationRequest"></a>
<a id="tocSeditapplicationrequest"></a>
<a id="tocseditapplicationrequest"></a>

```json
{
  "applicationName": "string",
  "vendorId": 0,
  "claimSetName": "string",
  "profileIds": [
    0
  ],
  "educationOrganizationIds": [
    0
  ],
  "odsInstanceIds": [
    0
  ],
  "enabled": true
}

```

EditApplicationRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|applicationName|string|false|none|Application name|
|vendorId|integer(int32)|false|none|Vendor/ company id|
|claimSetName|string|false|none|Claim set name|
|profileIds|[integer]¦null|false|none|Profile id|
|educationOrganizationIds|[integer]|false|none|Education organization ids|
|odsInstanceIds|[integer]|false|none|List of ODS instance id|
|enabled|boolean¦null|false|none|Indicates whether the ApiClient's credetials is enabled. Defaults to true if not provided.|

<h2 id="tocS_editClaimSetRequest">editClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditclaimsetrequest"></a>
<a id="schema_editClaimSetRequest"></a>
<a id="tocSeditclaimsetrequest"></a>
<a id="tocseditclaimsetrequest"></a>

```json
{
  "name": "string"
}

```

EditClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Claim set name|

<h2 id="tocS_editOdsInstanceContextRequest">editOdsInstanceContextRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditodsinstancecontextrequest"></a>
<a id="schema_editOdsInstanceContextRequest"></a>
<a id="tocSeditodsinstancecontextrequest"></a>
<a id="tocseditodsinstancecontextrequest"></a>

```json
{
  "odsInstanceId": 0,
  "contextKey": "string",
  "contextValue": "string"
}

```

EditOdsInstanceContextRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|odsInstanceId|integer(int32)|false|none|ODS instance context ODS instance id.|
|contextKey|string|false|none|context key.|
|contextValue|string|false|none|context value.|

<h2 id="tocS_editOdsInstanceDerivativeRequest">editOdsInstanceDerivativeRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditodsinstancederivativerequest"></a>
<a id="schema_editOdsInstanceDerivativeRequest"></a>
<a id="tocSeditodsinstancederivativerequest"></a>
<a id="tocseditodsinstancederivativerequest"></a>

```json
{
  "odsInstanceId": 0,
  "derivativeType": "string",
  "connectionString": "string"
}

```

EditOdsInstanceDerivativeRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|odsInstanceId|integer(int32)|false|none|ODS instance derivative ODS instance id.|
|derivativeType|string|false|none|derivative type.|
|connectionString|string|false|none|connection string.|

<h2 id="tocS_editOdsInstanceRequest">editOdsInstanceRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditodsinstancerequest"></a>
<a id="schema_editOdsInstanceRequest"></a>
<a id="tocSeditodsinstancerequest"></a>
<a id="tocseditodsinstancerequest"></a>

```json
{
  "name": "string",
  "instanceType": "string",
  "connectionString": "string"
}

```

EditOdsInstanceRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Ods Instance name|
|instanceType|string¦null|false|none|Ods Instance type|
|connectionString|string¦null|false|none|Ods Instance connection string|

<h2 id="tocS_editProfileRequest">editProfileRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditprofilerequest"></a>
<a id="schema_editProfileRequest"></a>
<a id="tocSeditprofilerequest"></a>
<a id="tocseditprofilerequest"></a>

```json
"{\n  \"name\": \"Test-Profile\",\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\n}"

```

EditProfileRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Profile name|
|definition|string|false|none|Profile definition|

<h2 id="tocS_editResourceClaimOnClaimSetRequest">editResourceClaimOnClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditresourceclaimonclaimsetrequest"></a>
<a id="schema_editResourceClaimOnClaimSetRequest"></a>
<a id="tocSeditresourceclaimonclaimsetrequest"></a>
<a id="tocseditresourceclaimonclaimsetrequest"></a>

```json
{
  "resourceClaimActions": [
    {
      "name": "string",
      "enabled": true
    }
  ]
}

```

EditResourceClaimActionsOnClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|resourceClaimActions|[[resourceClaimAction](#schemaresourceclaimaction)]|false|none|none|

<h2 id="tocS_editVendorRequest">editVendorRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditvendorrequest"></a>
<a id="schema_editVendorRequest"></a>
<a id="tocSeditvendorrequest"></a>
<a id="tocseditvendorrequest"></a>

```json
{
  "company": "string",
  "namespacePrefixes": "string",
  "contactName": "string",
  "contactEmailAddress": "string"
}

```

EditVendorRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|company|string|false|none|Vendor/ company name|
|namespacePrefixes|string|false|none|Namespace prefix for the vendor. Multiple namespace prefixes can be provided as comma separated list if required.|
|contactName|string|false|none|Vendor contact name|
|contactEmailAddress|string|false|none|Vendor contact email id|

<h2 id="tocS_importClaimSetRequest">importClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaimportclaimsetrequest"></a>
<a id="schema_importClaimSetRequest"></a>
<a id="tocSimportclaimsetrequest"></a>
<a id="tocsimportclaimsetrequest"></a>

```json
{
  "name": "string",
  "resourceClaims": [
    {
      "id": 0,
      "name": "string",
      "actions": [
        {
          "name": "string",
          "enabled": true
        }
      ],
      "_defaultAuthorizationStrategiesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "authorizationStrategyOverridesForCRUD": [
        {
          "actionId": 0,
          "actionName": "string",
          "authorizationStrategies": [
            {
              "authStrategyId": 0,
              "authStrategyName": "string",
              "isInheritedFromParent": true
            }
          ]
        }
      ],
      "children": [
        {}
      ]
    }
  ]
}

```

ImportClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|false|none|Claim set name|
|resourceClaims|[[claimSetResourceClaimModel](#schemaclaimsetresourceclaimmodel)]|false|none|Resource Claims|

<h2 id="tocS_informationResult">informationResult</h2>
<!-- backwards compatibility -->
<a id="schemainformationresult"></a>
<a id="schema_informationResult"></a>
<a id="tocSinformationresult"></a>
<a id="tocsinformationresult"></a>

```json
{
  "version": "string",
  "build": "string"
}

```

Information

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|version|string|false|none|Application version|
|build|string|false|none|Build / release version|

<h2 id="tocS_odsInstanceContextModel">odsInstanceContextModel</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancecontextmodel"></a>
<a id="schema_odsInstanceContextModel"></a>
<a id="tocSodsinstancecontextmodel"></a>
<a id="tocsodsinstancecontextmodel"></a>

```json
{
  "id": 0,
  "odsInstanceId": 0,
  "contextKey": "string",
  "contextValue": "string"
}

```

OdsInstanceContext

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|odsInstanceId|integer(int32)|false|none|none|
|contextKey|string¦null|false|none|none|
|contextValue|string¦null|false|none|none|

<h2 id="tocS_odsInstanceDerivativeModel">odsInstanceDerivativeModel</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancederivativemodel"></a>
<a id="schema_odsInstanceDerivativeModel"></a>
<a id="tocSodsinstancederivativemodel"></a>
<a id="tocsodsinstancederivativemodel"></a>

```json
{
  "id": 0,
  "odsInstanceId": 0,
  "derivativeType": "string"
}

```

OdsInstanceDerivative

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|odsInstanceId|integer(int32)¦null|false|none|none|
|derivativeType|string¦null|false|none|none|

<h2 id="tocS_odsInstanceDetailModel">odsInstanceDetailModel</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancedetailmodel"></a>
<a id="schema_odsInstanceDetailModel"></a>
<a id="tocSodsinstancedetailmodel"></a>
<a id="tocsodsinstancedetailmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "instanceType": "string",
  "odsInstanceContexts": [
    {
      "id": 0,
      "odsInstanceId": 0,
      "contextKey": "string",
      "contextValue": "string"
    }
  ],
  "odsInstanceDerivatives": [
    {
      "id": 0,
      "odsInstanceId": 0,
      "derivativeType": "string"
    }
  ]
}

```

OdsInstanceDetail

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|instanceType|string¦null|false|none|none|
|odsInstanceContexts|[[odsInstanceContextModel](#schemaodsinstancecontextmodel)]¦null|false|none|none|
|odsInstanceDerivatives|[[odsInstanceDerivativeModel](#schemaodsinstancederivativemodel)]¦null|false|none|none|

<h2 id="tocS_odsInstanceModel">odsInstanceModel</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancemodel"></a>
<a id="schema_odsInstanceModel"></a>
<a id="tocSodsinstancemodel"></a>
<a id="tocsodsinstancemodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "instanceType": "string"
}

```

OdsInstance

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|instanceType|string¦null|false|none|none|

<h2 id="tocS_overrideAuthStategyOnClaimSetRequest">overrideAuthStategyOnClaimSetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaoverrideauthstategyonclaimsetrequest"></a>
<a id="schema_overrideAuthStategyOnClaimSetRequest"></a>
<a id="tocSoverrideauthstategyonclaimsetrequest"></a>
<a id="tocsoverrideauthstategyonclaimsetrequest"></a>

```json
{
  "actionName": "string",
  "authorizationStrategies": [
    "string"
  ]
}

```

OverrideAuthStategyOnClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|actionName|string¦null|false|none|none|
|authorizationStrategies|[string]|false|none|AuthorizationStrategy Names|

<h2 id="tocS_profileDetailsModel">profileDetailsModel</h2>
<!-- backwards compatibility -->
<a id="schemaprofiledetailsmodel"></a>
<a id="schema_profileDetailsModel"></a>
<a id="tocSprofiledetailsmodel"></a>
<a id="tocsprofiledetailsmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "definition": "string"
}

```

ProfileDetails

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)¦null|false|none|none|
|name|string¦null|false|none|none|
|definition|string¦null|false|none|none|

<h2 id="tocS_profileModel">profileModel</h2>
<!-- backwards compatibility -->
<a id="schemaprofilemodel"></a>
<a id="schema_profileModel"></a>
<a id="tocSprofilemodel"></a>
<a id="tocsprofilemodel"></a>

```json
{
  "id": 0,
  "name": "string"
}

```

Profile

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)¦null|false|none|none|
|name|string¦null|false|none|none|

<h2 id="tocS_registerClientRequest">registerClientRequest</h2>
<!-- backwards compatibility -->
<a id="schemaregisterclientrequest"></a>
<a id="schema_registerClientRequest"></a>
<a id="tocSregisterclientrequest"></a>
<a id="tocsregisterclientrequest"></a>

```json
{
  "clientId": "string",
  "clientSecret": "string",
  "displayName": "string"
}

```

RegisterClientRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|clientId|string|false|none|Client id|
|clientSecret|string|false|none|Client secret|
|displayName|string|false|none|Client display name|

<h2 id="tocS_resourceClaimAction">resourceClaimAction</h2>
<!-- backwards compatibility -->
<a id="schemaresourceclaimaction"></a>
<a id="schema_resourceClaimAction"></a>
<a id="tocSresourceclaimaction"></a>
<a id="tocsresourceclaimaction"></a>

```json
{
  "name": "string",
  "enabled": true
}

```

ResourceClaimAction

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string¦null|false|none|none|
|enabled|boolean|false|none|none|

<h2 id="tocS_resourceClaimActionAuthStrategyModel">resourceClaimActionAuthStrategyModel</h2>
<!-- backwards compatibility -->
<a id="schemaresourceclaimactionauthstrategymodel"></a>
<a id="schema_resourceClaimActionAuthStrategyModel"></a>
<a id="tocSresourceclaimactionauthstrategymodel"></a>
<a id="tocsresourceclaimactionauthstrategymodel"></a>

```json
{
  "resourceClaimId": 0,
  "resourceName": "string",
  "claimName": "string",
  "authorizationStrategiesForActions": [
    {
      "actionId": 0,
      "actionName": "string",
      "authorizationStrategies": [
        {
          "authStrategyId": 0,
          "authStrategyName": "string"
        }
      ]
    }
  ]
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|resourceClaimId|integer(int32)|false|none|none|
|resourceName|string¦null|false|none|none|
|claimName|string¦null|false|none|none|
|authorizationStrategiesForActions|[[actionWithAuthorizationStrategy](#schemaactionwithauthorizationstrategy)]¦null|false|none|none|

<h2 id="tocS_resourceClaimActionModel">resourceClaimActionModel</h2>
<!-- backwards compatibility -->
<a id="schemaresourceclaimactionmodel"></a>
<a id="schema_resourceClaimActionModel"></a>
<a id="tocSresourceclaimactionmodel"></a>
<a id="tocsresourceclaimactionmodel"></a>

```json
{
  "resourceClaimId": 0,
  "resourceName": "string",
  "claimName": "string",
  "actions": [
    {
      "name": "string"
    }
  ]
}

```

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|resourceClaimId|integer(int32)|false|none|none|
|resourceName|string¦null|false|none|none|
|claimName|string¦null|false|none|none|
|actions|[[actionForResourceClaimModel](#schemaactionforresourceclaimmodel)]¦null|false|none|none|

<h2 id="tocS_resourceClaimModel">resourceClaimModel</h2>
<!-- backwards compatibility -->
<a id="schemaresourceclaimmodel"></a>
<a id="schema_resourceClaimModel"></a>
<a id="tocSresourceclaimmodel"></a>
<a id="tocsresourceclaimmodel"></a>

```json
{
  "id": 0,
  "name": "string",
  "parentId": 0,
  "parentName": "string",
  "children": [
    {
      "id": 0,
      "name": "string",
      "parentId": 0,
      "parentName": "string",
      "children": []
    }
  ]
}

```

ResourceClaimModel

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|false|none|none|
|name|string¦null|false|none|none|
|parentId|integer(int32)¦null|false|none|none|
|parentName|string¦null|false|none|none|
|children|[[resourceClaimModel](#schemaresourceclaimmodel)]¦null|false|none|Children are collection of SimpleResourceClaimModel|

<h2 id="tocS_simpleApplicationModel">simpleApplicationModel</h2>
<!-- backwards compatibility -->
<a id="schemasimpleapplicationmodel"></a>
<a id="schema_simpleApplicationModel"></a>
<a id="tocSsimpleapplicationmodel"></a>
<a id="tocssimpleapplicationmodel"></a>

```json
{
  "applicationName": "string"
}

```

Application

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|applicationName|string¦null|false|none|none|

<h2 id="tocS_vendorModel">vendorModel</h2>
<!-- backwards compatibility -->
<a id="schemavendormodel"></a>
<a id="schema_vendorModel"></a>
<a id="tocSvendormodel"></a>
<a id="tocsvendormodel"></a>

```json
{
  "id": 0,
  "company": "string",
  "namespacePrefixes": "string",
  "contactName": "string",
  "contactEmailAddress": "string"
}

```

Vendor

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)¦null|false|none|none|
|company|string¦null|false|none|none|
|namespacePrefixes|string¦null|false|none|none|
|contactName|string¦null|false|none|none|
|contactEmailAddress|string¦null|false|none|none|

