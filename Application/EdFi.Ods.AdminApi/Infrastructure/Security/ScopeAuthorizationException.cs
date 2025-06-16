// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using System.Net;

namespace EdFi.Ods.AdminApi.Infrastructure.Security;

/// <summary>
/// Exception thrown when authorization fails due to missing or invalid scope claims.
/// This will be caught by RequestLoggingMiddleware and converted to a 400 Bad Request.
/// </summary>
public class ScopeAuthorizationException : Exception, IAdminApiException
{
    public ScopeAuthorizationException(string message) : base(message)
    {
        StatusCode = HttpStatusCode.BadRequest;
        ResponseMessage = message;
        AllowFeedback = false;
        IsStackTraceRelevant = false;
    }

    public HttpStatusCode? StatusCode { get; }
    public string ResponseMessage { get; }
    public bool AllowFeedback { get; set; }
    public bool IsStackTraceRelevant { get; }
}
