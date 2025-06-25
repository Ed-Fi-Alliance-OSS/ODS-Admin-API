# ADMINAPI-1221: `/applications` Endpoint Documentation

## Purpose

The `/applications` endpoint allows clients to retrieve one or more application resources. The API supports both legacy and new query patterns to maximize compatibility and flexibility for field usage.

## How It Works

* **HTTP Method:** `GET`
* **Route:** `/applications` (with query parameters)
* **Query Parameters:**
  * `id` (single integer, legacy/field usage)
  * `ids` (comma-separated list of integers, official batch retrieval)
* **Response:**
  * For `id`, an array with a single `ApplicationModel` object is returned.
  * For `ids`, an array of `ApplicationModel` objects is returned.
* **Error Handling:**
  * If `id` is present and is not a valid integer, the endpoint will log the error to the console.
  * If `ids` is present and is not a comma-separated list of integers, the endpoint returns a type error (400 Bad Request).
  * If both `id` and `ids` are present, `id` takes precedence and only the single application for that `id` is returned.
  * If neither parameter is present, all applications are returned (default behavior).
  * If none of the provided IDs match existing applications, the endpoint returns a `404 Not Found`.

### Example Requests

```http
GET /applications?id=1
GET /applications?ids=1,2,3
```

### Example Responses

```json
// For id=1
[ 
  {
    "id": 1,
    "applicationName": "App One",
    ...
  }
]

// For ids=1,2,3
[
  {
    "id": 1,
    "applicationName": "App One",
    ...
  },
  {
    "id": 2,
    "applicationName": "App Two",
    ...
  }
]
```

## RESTful Alignment

This approach aligns with REST principles by:

* Using the `GET` method for safe, idempotent, read-only operations.
* Returning a single resource for `/applications?id=` and a collection for `/applications?ids=`.
* Using query parameters for filtering and batch retrieval, a common RESTful pattern.
* Not overloading the single-resource endpoint (`/applications/{id}`), keeping URIs predictable and semantically clear.

## Why Support Both `id` and `ids`?

We are keeping both ways. `applications?id=` is not official, but it is being used by our field so we don't want to modify or break existing integrations. Supporting both `id` and `ids` allows for a smooth transition and maximum compatibility.

* The `id` parameter overrides the `ids` parameter if both are set.
* The `id` parameter must be a single integer. If a non-integer or a comma-separated list is passed (e.g., `id=1,2,3`), a type error is returned.

## Summary

The `/applications` endpoint provides a RESTful, non-breaking way to support both single and batch retrieval of applications. This approach keeps the API clean, predictable, backward compatible, and flexible for both legacy and new client needs.
