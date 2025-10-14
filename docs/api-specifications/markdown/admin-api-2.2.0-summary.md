<!-- Generator: Widdershins v4.0.1 -->

<h1 id="api">Admin API v2.2.0</h1>

The Ed-Fi Admin API is a REST API-based administrative interface for managing vendors, applications, client credentials, and authorization rules for accessing an Ed-Fi API.

# Authentication

- oAuth2 authentication. 

    - Flow: clientCredentials

    - Token URL = [https://localhost/adminapi/connect/token](https://localhost/adminapi/connect/token)

|Scope|Scope Description|
|---|---|
|edfi_admin_api/full_access|Unrestricted access to all Admin API endpoints|

<h1 id="api-resourceclaims">ResourceClaims</h1>

## Retrieves all resourceClaims.

`GET /v2/resourceClaims`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-resourceclaims.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[resourceClaim](#schemaresourceclaim)]|false|none|none|
|» ResourceClaimModel|[resourceClaim](#schemaresourceclaim)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» name|string¦null|true|none|none|
|»» parentId|integer(int32)¦null|true|none|none|
|»» parentName|string¦null|true|none|none|
|»» children|[[resourceClaim](#schemaresourceclaim)]¦null|true|none|Children are collection of SimpleResourceClaimModel|
|»»» ResourceClaimModel|[resourceClaim](#schemaresourceclaim)|false|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific resourceClaim based on the identifier.

`GET /v2/resourceClaims/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[resourceClaim](#schemaresourceclaim)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-vendors">Vendors</h1>

## Retrieves all vendors.

`GET /v2/vendors`

<h3 id="retrieves-all-vendors.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-vendors.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[vendor](#schemavendor)]|false|none|none|
|» Vendor|[vendor](#schemavendor)|false|none|none|
|»» id|integer(int32)¦null|true|none|none|
|»» company|string¦null|true|none|none|
|»» namespacePrefixes|string¦null|true|none|none|
|»» contactName|string¦null|true|none|none|
|»» contactEmailAddress|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates vendor based on the supplied values.

`POST /v2/vendors`

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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific vendor based on the identifier.

`GET /v2/vendors/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[vendor](#schemavendor)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates vendor based on the resource identifier.

`PUT /v2/vendors/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing vendor using the resource identifier.

`DELETE /v2/vendors/{id}`

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
    ]
  }
]
```

<h3 id="retrieves-applications-assigned-to-a-specific-vendor-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-applications-assigned-to-a-specific-vendor-based-on-the-resource-identifier.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[application](#schemaapplication)]|false|none|none|
|» Application|[application](#schemaapplication)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» applicationName|string¦null|true|none|none|
|»» claimSetName|string¦null|true|none|none|
|»» educationOrganizationIds|[integer]¦null|true|none|none|
|»» vendorId|integer(int32)¦null|true|none|none|
|»» profileIds|[integer]¦null|true|none|none|
|»» odsInstanceIds|[integer]¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-profiles">Profiles</h1>

## Retrieves all profiles.

`GET /v2/profiles`

<h3 id="retrieves-all-profiles.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-profiles.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[profile](#schemaprofile)]|false|none|none|
|» Profile|[profile](#schemaprofile)|false|none|none|
|»» id|integer(int32)¦null|true|none|none|
|»» name|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates profile based on the supplied values.

`POST /v2/profiles`

> Body parameter

```json
"{\r\n  \"name\": \"Test-Profile\",\r\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\r\n}"
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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific profile based on the identifier.

`GET /v2/profiles/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[profileDetails](#schemaprofiledetails)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates profile based on the resource identifier.

`PUT /v2/profiles/{id}`

> Body parameter

```json
"{\r\n  \"name\": \"Test-Profile\",\r\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\r\n}"
```

<h3 id="updates-profile-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editProfileRequest](#schemaeditprofilerequest)|true|none|

<h3 id="updates-profile-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing profile using the resource identifier.

`DELETE /v2/profiles/{id}`

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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-odsinstances">OdsInstances</h1>

## Retrieves all odsInstances.

`GET /v2/odsInstances`

<h3 id="retrieves-all-odsinstances.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-odsinstances.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[odsInstance](#schemaodsinstance)]|false|none|none|
|» OdsInstance|[odsInstance](#schemaodsinstance)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|true|none|none|
|»» instanceType|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates odsInstance based on the supplied values.

`POST /v2/odsInstances`

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
|body|body|[addOdsIntanceRequest](#schemaaddodsintancerequest)|true|none|

<h3 id="creates-odsinstance-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific odsInstance based on the identifier.

`GET /v2/odsInstances/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[odsInstanceDetail](#schemaodsinstancedetail)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates odsInstance based on the resource identifier.

`PUT /v2/odsInstances/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing odsInstance using the resource identifier.

`DELETE /v2/odsInstances/{id}`

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
    ]
  }
]
```

<h3 id="retrieves-applications-assigned-to-a-specific-ods-instance-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-applications-assigned-to-a-specific-ods-instance-based-on-the-resource-identifier.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[application](#schemaapplication)]|false|none|none|
|» Application|[application](#schemaapplication)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» applicationName|string¦null|true|none|none|
|»» claimSetName|string¦null|true|none|none|
|»» educationOrganizationIds|[integer]¦null|true|none|none|
|»» vendorId|integer(int32)¦null|true|none|none|
|»» profileIds|[integer]¦null|true|none|none|
|»» odsInstanceIds|[integer]¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-odsinstancederivatives">OdsInstanceDerivatives</h1>

## Retrieves all odsInstanceDerivatives.

`GET /v2/odsInstanceDerivatives`

<h3 id="retrieves-all-odsinstancederivatives.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-odsinstancederivatives.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[odsInstanceDerivative](#schemaodsinstancederivative)]|false|none|none|
|» OdsInstanceDerivative|[odsInstanceDerivative](#schemaodsinstancederivative)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» odsInstanceId|integer(int32)¦null|true|none|none|
|»» derivativeType|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates odsInstanceDerivative based on the supplied values.

`POST /v2/odsInstanceDerivatives`

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
|body|body|[addOdsInstanceDerivateRequest](#schemaaddodsinstancederivaterequest)|true|none|

<h3 id="creates-odsinstancederivative-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific odsInstanceDerivative based on the identifier.

`GET /v2/odsInstanceDerivatives/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[odsInstanceDerivative](#schemaodsinstancederivative)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates odsInstanceDerivative based on the resource identifier.

`PUT /v2/odsInstanceDerivatives/{id}`

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
|body|body|[editOdsInstanceDerivateRequest](#schemaeditodsinstancederivaterequest)|true|none|

<h3 id="updates-odsinstancederivative-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing odsInstanceDerivative using the resource identifier.

`DELETE /v2/odsInstanceDerivatives/{id}`

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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-odsinstancecontexts">OdsInstanceContexts</h1>

## Retrieves all odsInstanceContexts.

`GET /v2/odsInstanceContexts`

<h3 id="retrieves-all-odsinstancecontexts.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-odsinstancecontexts.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[odsInstanceContext](#schemaodsinstancecontext)]|false|none|none|
|» OdsInstanceContext|[odsInstanceContext](#schemaodsinstancecontext)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» odsInstanceId|integer(int32)|true|none|none|
|»» contextKey|string¦null|true|none|none|
|»» contextValue|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates odsInstanceContext based on the supplied values.

`POST /v2/odsInstanceContexts`

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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific odsInstanceContext based on the identifier.

`GET /v2/odsInstanceContexts/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[odsInstanceContext](#schemaodsinstancecontext)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates odsInstanceContext based on the resource identifier.

`PUT /v2/odsInstanceContexts/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing odsInstanceContext using the resource identifier.

`DELETE /v2/odsInstanceContexts/{id}`

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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-claimsets">ClaimSets</h1>

## Exports a specific claimSet based on the identifier.

`GET /v2/claimSets/{id}/export`

<h3 id="exports-a-specific-claimset-based-on-the-identifier.-parameters">Parameters</h3>

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
  ]
}
```

<h3 id="exports-a-specific-claimset-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[claimsetWithResources](#schemaclaimsetwithresources)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves all claimSets.

`GET /v2/claimSets`

<h3 id="retrieves-all-claimsets.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-claimsets.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[claimset](#schemaclaimset)]|false|none|none|
|» ClaimSet|[claimset](#schemaclaimset)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» name|string¦null|true|none|none|
|»» _isSystemReserved|boolean|false|read-only|none|
|»» _applications|[[simpleApplication](#schemasimpleapplication)]¦null|false|read-only|none|
|»»» Application|[simpleApplication](#schemasimpleapplication)|false|none|none|
|»»»» applicationName|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates claimSet based on the supplied values.

`POST /v2/claimSets`

> Body parameter

```json
{
  "name": "string"
}
```

<h3 id="creates-claimset-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[addClaimsetRequest](#schemaaddclaimsetrequest)|true|none|

<h3 id="creates-claimset-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific claimSet based on the identifier.

`GET /v2/claimSets/{id}`

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
  ]
}
```

<h3 id="retrieves-a-specific-claimset-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[claimsetWithResources](#schemaclaimsetwithresources)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates claimSet based on the resource identifier.

`PUT /v2/claimSets/{id}`

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
|body|body|[editClaimsetRequest](#schemaeditclaimsetrequest)|true|none|

<h3 id="updates-claimset-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing claimSet using the resource identifier.

`DELETE /v2/claimSets/{id}`

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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Copies the existing claimset and create new one.

`POST /v2/claimSets/copy`

> Body parameter

```json
{
  "originalId": 0,
  "name": "string"
}
```

<h3 id="copies-the-existing-claimset-and-create-new-one.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[copyClaimsetRequest](#schemacopyclaimsetrequest)|true|none|

<h3 id="copies-the-existing-claimset-and-create-new-one.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Imports a new claimSet based on the supplied values.

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
  ]
}
```

<h3 id="imports-a-new-claimset-based-on-the-supplied-values.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|[importClaimsetRequest](#schemaimportclaimsetrequest)|true|none|

<h3 id="imports-a-new-claimset-based-on-the-supplied-values.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Overrides the default authorization strategies on provided resource claim for a specific action.

`POST /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}/overrideAuthorizationStrategy`

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
|body|body|[overrideAuthorizationStrategyOnClaimsetRequest](#schemaoverrideauthorizationstrategyonclaimsetrequest)|true|none|

<h3 id="overrides-the-default-authorization-strategies-on-provided-resource-claim-for-a-specific-action.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Adds resourceClaimAction association to a claimSet.

`POST /v2/claimSets/{claimSetId}/resourceClaimActions`

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

<h3 id="adds-resourceclaimaction-association-to-a-claimset.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|body|body|[addResourceClaimActionsOnClaimsetRequest](#schemaaddresourceclaimactionsonclaimsetrequest)|true|none|

<h3 id="adds-resourceclaimaction-association-to-a-claimset.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|201|[Created](https://tools.ietf.org/html/rfc7231#section-6.3.2)|Created|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates the resourceClaimAction to a specific resourceClaim on a claimSet.

`PUT /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}`

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

<h3 id="updates-the-resourceclaimaction-to-a-specific-resourceclaim-on-a-claimset.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|resourceClaimId|path|integer(int32)|true|none|
|body|body|[editResourceClaimActionsOnClaimsetRequest](#schemaeditresourceclaimactionsonclaimsetrequest)|true|none|

<h3 id="updates-the-resourceclaimaction-to-a-specific-resourceclaim-on-a-claimset.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes a resourceClaims association from a claimSet.

`DELETE /v2/claimSets/{claimSetId}/resourceClaimActions/{resourceClaimId}`

<h3 id="deletes-a-resourceclaims-association-from-a-claimset.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|claimSetId|path|integer(int32)|true|none|
|resourceClaimId|path|integer(int32)|true|none|

<h3 id="deletes-a-resourceclaims-association-from-a-claimset.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-authorizationstrategies">AuthorizationStrategies</h1>

## Retrieves all authorizationStrategies.

`GET /v2/authorizationStrategies`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-authorizationstrategies.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[authorizationStrategy](#schemaauthorizationStrategy)]|false|none|none|
|» AuthorizationStrategy|[authorizationStrategy](#schemaauthorizationStrategy)|false|none|none|
|»» id|integer(int32)|false|none|none|
|»» name|string¦null|false|none|none|
|»» displayName|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-applications">Applications</h1>

## Retrieves all applications.

`GET /v2/applications`

<h3 id="retrieves-all-applications.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|offset|query|integer(int32)|true|Indicates how many items should be skipped before returning results.|
|limit|query|integer(int32)|true|Indicates the maximum number of items that should be returned in the results.|

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
    ]
  }
]
```

<h3 id="retrieves-all-applications.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-applications.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[application](#schemaapplication)]|false|none|none|
|» Application|[application](#schemaapplication)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» applicationName|string¦null|true|none|none|
|»» claimSetName|string¦null|true|none|none|
|»» educationOrganizationIds|[integer]¦null|true|none|none|
|»» vendorId|integer(int32)¦null|true|none|none|
|»» profileIds|[integer]¦null|true|none|none|
|»» odsInstanceIds|[integer]¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Creates application based on the supplied values.

`POST /v2/applications`

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
  ]
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
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Retrieves a specific application based on the identifier.

`GET /v2/applications/{id}`

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
  ]
}
```

<h3 id="retrieves-a-specific-application-based-on-the-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[application](#schemaapplication)|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Updates application based on the resource identifier.

`PUT /v2/applications/{id}`

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
  ]
}
```

<h3 id="updates-application-based-on-the-resource-identifier.-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|id|path|integer(int32)|true|none|
|body|body|[editApplicationResult](#schemaeditapplicationresult)|true|none|

<h3 id="updates-application-based-on-the-resource-identifier.-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

## Deletes an existing application using the resource identifier.

`DELETE /v2/applications/{id}`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[applicationResult](#schemaapplicationresult)|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|404|[Not Found](https://tools.ietf.org/html/rfc7231#section-6.5.4)|Not found. A resource with given identifier could not be found.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-actions">Actions</h1>

## Retrieves all actions.

`GET /v2/actions`

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|Inline|
|401|[Unauthorized](https://tools.ietf.org/html/rfc7235#section-3.1)|Unauthorized. The request requires authentication|None|
|403|[Forbidden](https://tools.ietf.org/html/rfc7231#section-6.5.3)|Forbidden. The request is authenticated, but not authorized to access this resource|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<h3 id="retrieves-all-actions.-responseschema">Response Schema</h3>

Status Code **200**

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|*anonymous*|[[action](#schemaaction)]|false|none|none|
|» Action|[action](#schemaaction)|false|none|none|
|»» id|integer(int32)|true|none|none|
|»» name|string¦null|true|none|none|
|»» uri|string¦null|true|none|none|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-information">Information</h1>

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
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Success|[information](#schemainformation)|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|[information](#schemainformation)|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

<h1 id="api-connect">Connect</h1>

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
client_id: string
client_secret: string
grant_type: string
scope: string

```

<h3 id="retrieves-bearer-token-parameters">Parameters</h3>

|Name|In|Type|Required|Description|
|---|---|---|---|---|
|body|body|object|false|none|
|» client_id|body|string|false|none|
|» client_secret|body|string|false|none|
|» grant_type|body|string|false|none|
|» scope|body|string|false|none|

<h3 id="retrieves-bearer-token-responses">Responses</h3>

|Status|Meaning|Description|Schema|
|---|---|---|---|
|200|[OK](https://tools.ietf.org/html/rfc7231#section-6.3.1)|Sign-in successful.|None|
|400|[Bad Request](https://tools.ietf.org/html/rfc7231#section-6.5.1)|Bad Request. The request was invalid and cannot be completed. See the response body for details.|None|
|500|[Internal Server Error](https://tools.ietf.org/html/rfc7231#section-6.6.1)|Internal server error. An unhandled error occurred on the server. See the response body for details.|None|

<aside class="warning">
To perform this operation, you must be authenticated by means of one of the following methods:
oauth ( Scopes: api )
</aside>

# Schemas

<h2 id="tocS_action">action</h2>
<!-- backwards compatibility -->
<a id="schemaaction"></a>
<a id="schema_action"></a>
<a id="tocSaction"></a>
<a id="tocsaction"></a>

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
|id|integer(int32)|true|none|none|
|name|string¦null|true|none|none|
|uri|string¦null|true|none|none|

<h2 id="tocS_adminApiError">adminApiError</h2>
<!-- backwards compatibility -->
<a id="schemaadminapierror"></a>
<a id="schema_adminApiError"></a>
<a id="tocSadminapierror"></a>
<a id="tocsadminapierror"></a>

```json
{
  "title": "string",
  "errors": [
    "string"
  ]
}

```

AdminApiError

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|title|string¦null|true|read-only|none|
|errors|[string]¦null|true|read-only|none|

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
  ]
}

```

AddApplicationRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|applicationName|string|true|none|Application name|
|vendorId|integer(int32)|true|none|Vendor/ company id|
|claimSetName|string|true|none|Claim set name|
|profileIds|[integer]¦null|false|none|Profile id|
|educationOrganizationIds|[integer]|true|none|Education organization ids|
|odsInstanceIds|[integer]|true|none|List of ODS instance id|

<h2 id="tocS_application">application</h2>
<!-- backwards compatibility -->
<a id="schemaapplication"></a>
<a id="schema_application"></a>
<a id="tocSapplication"></a>
<a id="tocsapplication"></a>

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
  ]
}

```

Application

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|true|none|none|
|applicationName|string¦null|true|none|none|
|claimSetName|string¦null|true|none|none|
|educationOrganizationIds|[integer]¦null|true|none|none|
|vendorId|integer(int32)¦null|true|none|none|
|profileIds|[integer]¦null|true|none|none|
|odsInstanceIds|[integer]¦null|true|none|none|

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
|id|integer(int32)|true|none|none|
|key|string¦null|true|none|none|
|secret|string¦null|true|none|none|

<h2 id="tocS_editApplicationResult">editApplicationResult</h2>
<!-- backwards compatibility -->
<a id="schemaeditapplicationresult"></a>
<a id="schema_editApplicationResult"></a>
<a id="tocSeditapplicationresult"></a>
<a id="tocseditapplicationresult"></a>

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
  ]
}

```

EditApplicationRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|applicationName|string|true|none|Application name|
|vendorId|integer(int32)|true|none|Vendor/ company id|
|claimSetName|string|true|none|Claim set name|
|profileIds|[integer]¦null|false|none|Profile id|
|educationOrganizationIds|[integer]|true|none|Education organization ids|
|odsInstanceIds|[integer]|true|none|List of ODS instance id|

<h2 id="tocS_simpleApplication">simpleApplication</h2>
<!-- backwards compatibility -->
<a id="schemasimpleapplication"></a>
<a id="schema_simpleApplication"></a>
<a id="tocSsimpleapplication"></a>
<a id="tocssimpleapplication"></a>

```json
{
  "applicationName": "string"
}

```

Application

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|applicationName|string¦null|true|none|none|

<h2 id="tocS_authorizationStrategy">authorizationStrategy</h2>
<!-- backwards compatibility -->
<a id="schemaauthorizationStrategy"></a>
<a id="schema_authorizationStrategy"></a>
<a id="tocSauthorizationStrategy"></a>
<a id="tocsauthorizationStrategy"></a>

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
|displayName|string¦null|true|none|none|

<h2 id="tocS_addClaimsetRequest">addClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddclaimsetrequest"></a>
<a id="schema_addClaimsetRequest"></a>
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
|name|string|true|none|Claim set name|

<h2 id="tocS_claimsetResourceClaim">claimsetResourceClaim</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetresourceclaim"></a>
<a id="schema_claimsetResourceClaim"></a>
<a id="tocSclaimsetresourceclaim"></a>
<a id="tocsclaimsetresourceclaim"></a>

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
  ]
}

```

ClaimSetResourceClaim

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|true|read-only|none|
|name|string¦null|true|none|none|
|actions|[[resourceClaimAction](#schemaresourceclaimaction)]¦null|true|none|none|
|_defaultAuthorizationStrategiesForCRUD|[[claimsetResourceClaimActionAuthorizationStrategies](#schemaclaimsetresourceclaimactionauthorizationstrategies)]¦null|false|read-only|none|
|authorizationStrategyOverridesForCRUD|[[claimsetResourceClaimActionAuthorizationStrategies](#schemaclaimsetresourceclaimactionauthorizationstrategies)]¦null|true|none|none|
|children|[[claimsetResourcesClaim](#schemaclaimsetresourcesclaim)]¦null|true|none|Children are collection of ResourceClaim|

<h2 id="tocS_claimsetWithResources">claimsetWithResources</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetwithresources"></a>
<a id="schema_claimsetWithResources"></a>
<a id="tocSclaimsetwithresources"></a>
<a id="tocsclaimsetwithresources"></a>

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
  ]
}

```

ClaimSetWithResources

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|true|none|none|
|name|string¦null|true|none|none|
|_isSystemReserved|boolean|false|read-only|none|
|_applications|[[simpleApplication](#schemasimpleapplication)]¦null|false|read-only|none|
|resourceClaims|[[claimsetResourcesClaim](#schemaclaimsetresourcesclaim)]¦null|true|none|none|

<h2 id="tocS_claimset">claimset</h2>
<!-- backwards compatibility -->
<a id="schemaclaimset"></a>
<a id="schema_claimset"></a>
<a id="tocSclaimset"></a>
<a id="tocsclaimset"></a>

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
|id|integer(int32)|true|none|none|
|name|string¦null|true|none|none|
|_isSystemReserved|boolean|false|read-only|none|
|_applications|[[simpleApplication](#schemasimpleapplication)]¦null|false|read-only|none|

<h2 id="tocS_claimsetResourcesClaim">claimsetResourcesClaim</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetresourcesclaim"></a>
<a id="schema_claimsetResourcesClaim"></a>
<a id="tocSclaimsetresourcesclaim"></a>
<a id="tocsclaimsetresourcesclaim"></a>

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
  ]
}

```

ClaimSetResourceClaim

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|id|integer(int32)|true|read-only|none|
|name|string¦null|true|none|none|
|actions|[[resourceClaimAction](#schemaresourceclaimaction)]¦null|true|none|none|
|_defaultAuthorizationStrategiesForCRUD|[[claimsetResourceClaimActionAuthorizationStrategies](#schemaclaimsetresourceclaimactionauthorizationstrategies)]¦null|false|read-only|none|
|authorizationStrategyOverridesForCRUD|[[claimsetResourceClaimActionAuthorizationStrategies](#schemaclaimsetresourceclaimactionauthorizationstrategies)]¦null|true|none|none|
|children|[[claimsetResourceClaim](#schemaclaimsetresourceclaim)]¦null|true|none|Children are collection of ResourceClaim|

<h2 id="tocS_copyClaimsetRequest">copyClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemacopyclaimsetrequest"></a>
<a id="schema_copyClaimsetRequest"></a>
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
|originalId|integer(int32)|true|none|ClaimSet id to copy|
|name|string|true|none|New claimset name|

<h2 id="tocS_editClaimsetRequest">editClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditclaimsetrequest"></a>
<a id="schema_editClaimsetRequest"></a>
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
|name|string|true|none|Claim set name|

<h2 id="tocS_importClaimsetRequest">importClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaimportclaimsetrequest"></a>
<a id="schema_importClaimsetRequest"></a>
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
  ]
}

```

ImportClaimSetRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|true|none|Claim set name|
|resourceClaims|[[claimsetResourcesClaim](#schemaclaimsetresourcesclaim)]|true|none|Resource Claims|

<h2 id="tocS_resourceClaim">resourceClaim</h2>
<!-- backwards compatibility -->
<a id="schemaresourceclaim"></a>
<a id="schema_resourceClaim"></a>
<a id="tocSresourceclaim"></a>
<a id="tocsresourceclaim"></a>

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
|id|integer(int32)|true|none|none|
|name|string¦null|true|none|none|
|parentId|integer(int32)¦null|true|none|none|
|parentName|string¦null|true|none|none|
|children|[[resourceClaim](#schemaresourceclaim)]¦null|true|none|Children are collection of SimpleResourceClaimModel|

<h2 id="tocS_overrideAuthorizationStrategyOnClaimsetRequest">overrideAuthorizationStrategyOnClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaoverrideauthorizationstrategyonclaimsetrequest"></a>
<a id="schema_overrideAuthorizationStrategyOnClaimsetRequest"></a>
<a id="tocSoverrideauthorizationstrategyonclaimsetrequest"></a>
<a id="tocsoverrideauthorizationstrategyonclaimsetrequest"></a>

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
|actionName|string¦null|true|none|none|
|authorizationStrategies|[string]|true|none|AuthorizationStrategy Names|

<h2 id="tocS_addResourceClaimActionsOnClaimsetRequest">addResourceClaimActionsOnClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddresourceclaimactionsonclaimsetrequest"></a>
<a id="schema_addResourceClaimActionsOnClaimsetRequest"></a>
<a id="tocSaddresourceclaimactionsonclaimsetrequest"></a>
<a id="tocsaddresourceclaimactionsonclaimsetrequest"></a>

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
|resourceClaimId|integer(int32)|true|none|ResourceClaim id|
|resourceClaimActions|[[resourceClaimAction](#schemaresourceclaimaction)]|true|none|none|

<h2 id="tocS_editResourceClaimActionsOnClaimsetRequest">editResourceClaimActionsOnClaimsetRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditresourceclaimactionsonclaimsetrequest"></a>
<a id="schema_editResourceClaimActionsOnClaimsetRequest"></a>
<a id="tocSeditresourceclaimactionsonclaimsetrequest"></a>
<a id="tocseditresourceclaimactionsonclaimsetrequest"></a>

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
|resourceClaimActions|[[resourceClaimAction](#schemaresourceclaimaction)]|true|none|none|

<h2 id="tocS_registeClientRequest">registerClientRequest</h2>
<!-- backwards compatibility -->
<a id="schemaregisteclientrequest"></a>
<a id="schema_registeClientRequest"></a>
<a id="tocsregisterclientrequest"></a>
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
|clientId|string|true|none|Client id|
|clientSecret|string|true|none|Client secret|
|displayName|string|true|none|Client display name|

<h2 id="tocS_information">information</h2>
<!-- backwards compatibility -->
<a id="schemainformation"></a>
<a id="schema_information"></a>
<a id="tocSinformation"></a>
<a id="tocsinformation"></a>

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
|version|string|true|none|Application version|
|build|string|true|none|Build / release version|

<h2 id="tocS_odsInstanceDetail">odsInstanceDetail</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancedetail"></a>
<a id="schema_odsInstanceDetail"></a>
<a id="tocSodsinstancedetail"></a>
<a id="tocsodsinstancedetail"></a>

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
|name|string¦null|true|none|none|
|instanceType|string¦null|true|none|none|
|odsInstanceContexts|[[odsInstanceContext](#schemaodsinstancecontext)]¦null|true|none|none|
|odsInstanceDerivatives|[[odsInstanceDerivative](#schemaodsinstancederivative)]¦null|true|none|none|

<h2 id="tocS_odsInstance">odsInstance</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstance"></a>
<a id="schema_odsInstance"></a>
<a id="tocSodsinstance"></a>
<a id="tocsodsinstance"></a>

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
|name|string¦null|true|none|none|
|instanceType|string¦null|true|none|none|

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
|odsInstanceId|integer(int32)|true|none|ODS instance context ODS instance id.|
|contextKey|string|true|none|context key.|
|contextValue|string|true|none|context value.|

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
|odsInstanceId|integer(int32)|true|none|ODS instance context ODS instance id.|
|contextKey|string|true|none|context key.|
|contextValue|string|true|none|context value.|

<h2 id="tocS_odsInstanceContext">odsInstanceContext</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancecontext"></a>
<a id="schema_odsInstanceContext"></a>
<a id="tocSodsinstancecontext"></a>
<a id="tocsodsinstancecontext"></a>

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
|odsInstanceId|integer(int32)|true|none|none|
|contextKey|string¦null|true|none|none|
|contextValue|string¦null|true|none|none|

<h2 id="tocS_addOdsInstanceDerivateRequest">addOdsInstanceDerivateRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddodsinstancederivaterequest"></a>
<a id="schema_addOdsInstanceDerivateRequest"></a>
<a id="tocSaddodsinstancederivaterequest"></a>
<a id="tocsaddodsinstancederivaterequest"></a>

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
|odsInstanceId|integer(int32)|true|none|ODS instance derivative ODS instance id.|
|derivativeType|string|true|none|derivative type.|
|connectionString|string|true|none|connection string.|

<h2 id="tocS_editOdsInstanceDerivateRequest">editOdsInstanceDerivateRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditodsinstancederivaterequest"></a>
<a id="schema_editOdsInstanceDerivateRequest"></a>
<a id="tocSeditodsinstancederivaterequest"></a>
<a id="tocseditodsinstancederivaterequest"></a>

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
|odsInstanceId|integer(int32)|true|none|ODS instance derivative ODS instance id.|
|derivativeType|string|true|none|derivative type.|
|connectionString|string|true|none|connection string.|

<h2 id="tocS_odsInstanceDerivative">odsInstanceDerivative</h2>
<!-- backwards compatibility -->
<a id="schemaodsinstancederivative"></a>
<a id="schema_odsInstanceDerivative"></a>
<a id="tocSodsinstancederivative"></a>
<a id="tocsodsinstancederivative"></a>

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
|id|integer(int32)|true|none|none|
|odsInstanceId|integer(int32)¦null|true|none|none|
|derivativeType|string¦null|true|none|none|

<h2 id="tocS_addOdsIntanceRequest">addOdsIntanceRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddodsintancerequest"></a>
<a id="schema_addOdsIntanceRequest"></a>
<a id="tocSaddodsintancerequest"></a>
<a id="tocsaddodsintancerequest"></a>

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
|name|string|true|none|Ods Instance name|
|instanceType|string|true|none|Ods Instance type|
|connectionString|string|true|none|Ods Instance connection string|

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
|name|string|true|none|Ods Instance name|
|instanceType|string|true|none|Ods Instance type|
|connectionString|string¦null|true|none|Ods Instance connection string|

<h2 id="tocS_addProfileRequest">addProfileRequest</h2>
<!-- backwards compatibility -->
<a id="schemaaddprofilerequest"></a>
<a id="schema_addProfileRequest"></a>
<a id="tocSaddprofilerequest"></a>
<a id="tocsaddprofilerequest"></a>

```json
"{\r\n  \"name\": \"Test-Profile\",\r\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\r\n}"

```

AddProfileRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|true|none|Profile name|
|definition|string|true|none|Profile definition|

<h2 id="tocS_editProfileRequest">editProfileRequest</h2>
<!-- backwards compatibility -->
<a id="schemaeditprofilerequest"></a>
<a id="schema_editProfileRequest"></a>
<a id="tocSeditprofilerequest"></a>
<a id="tocseditprofilerequest"></a>

```json
"{\r\n  \"name\": \"Test-Profile\",\r\n  \"definition\": \"<Profile name=\\\"Test-Profile\\\"><Resource name=\\\"Resource1\\\"><ReadContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection1\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></ReadContentType><WriteContentType memberSelection=\\\"IncludeOnly\\\"><Collection name=\\\"Collection2\\\" memberSelection=\\\"IncludeOnly\\\"><Property name=\\\"Property1\\\" /><Property name=\\\"Property2\\\" /></Collection></WriteContentType></Resource></Profile>\"\r\n}"

```

EditProfileRequest

### Properties

|Name|Type|Required|Restrictions|Description|
|---|---|---|---|---|
|name|string|true|none|Profile name|
|definition|string|true|none|Profile definition|

<h2 id="tocS_profileDetails">profileDetails</h2>
<!-- backwards compatibility -->
<a id="schemaprofiledetails"></a>
<a id="schema_profileDetails"></a>
<a id="tocSprofiledetails"></a>
<a id="tocsprofiledetails"></a>

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
|id|integer(int32)¦null|true|none|none|
|name|string¦null|true|none|none|
|definition|string¦null|true|none|none|

<h2 id="tocS_profile">profile</h2>
<!-- backwards compatibility -->
<a id="schemaprofile"></a>
<a id="schema_profile"></a>
<a id="tocSprofile"></a>
<a id="tocsprofile"></a>

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
|id|integer(int32)¦null|true|none|none|
|name|string¦null|true|none|none|

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
|company|string|true|none|Vendor/ company name|
|namespacePrefixes|string|true|none|Namespace prefix for the vendor. Multiple namespace prefixes can be provided as comma separated list if required.|
|contactName|string|true|none|Vendor contact name|
|contactEmailAddress|string|true|none|Vendor contact email id|

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
|company|string|true|none|Vendor/ company name|
|namespacePrefixes|string|true|none|Namespace prefix for the vendor. Multiple namespace prefixes can be provided as comma separated list if required.|
|contactName|string|true|none|Vendor contact name|
|contactEmailAddress|string|true|none|Vendor contact email id|

<h2 id="tocS_vendor">vendor</h2>
<!-- backwards compatibility -->
<a id="schemavendor"></a>
<a id="schema_vendor"></a>
<a id="tocSvendor"></a>
<a id="tocsvendor"></a>

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
|id|integer(int32)¦null|true|none|none|
|company|string¦null|true|none|none|
|namespacePrefixes|string¦null|true|none|none|
|contactName|string¦null|true|none|none|
|contactEmailAddress|string¦null|true|none|none|

<h2 id="tocS_resourceClaimAuthorizationStrategy">resourceClaimAuthorizationStrategy</h2>
<!-- backwards compatibility -->
<a id="schemaresourceclaimauthorizationstrategy"></a>
<a id="schema_resourceClaimAuthorizationStrategy"></a>
<a id="tocSresourceclaimauthorizationstrategy"></a>
<a id="tocsresourceclaimauthorizationstrategy"></a>

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
|authStrategyId|integer(int32)|true|none|none|
|authStrategyName|string¦null|true|none|none|
|isInheritedFromParent|boolean|true|none|none|

<h2 id="tocS_claimsetResourceClaimActionAuthorizationStrategies">claimsetResourceClaimActionAuthorizationStrategies</h2>
<!-- backwards compatibility -->
<a id="schemaclaimsetresourceclaimactionauthorizationstrategies"></a>
<a id="schema_claimsetResourceClaimActionAuthorizationStrategies"></a>
<a id="tocSclaimsetresourceclaimactionauthorizationstrategies"></a>
<a id="tocsclaimsetresourceclaimactionauthorizationstrategies"></a>

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
|actionId|integer(int32)¦null|true|none|none|
|actionName|string¦null|true|none|none|
|authorizationStrategies|[[resourceClaimAuthorizationStrategy](#schemaresourceclaimauthorizationstrategy)]¦null|true|none|none|

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
|name|string¦null|true|none|none|
|enabled|boolean|true|none|none|

