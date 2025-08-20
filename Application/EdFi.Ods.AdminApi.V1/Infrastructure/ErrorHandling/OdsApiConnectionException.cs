// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Net;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.ErrorHandling;

public class OdsApiConnectionException : Exception, IAdminApiException
{
    public OdsApiConnectionException(HttpStatusCode responseCode, string responseMessage, string exceptionMessage) : base(exceptionMessage)
    {
        StatusCode = responseCode;
        ResponseMessage = responseMessage;
    }

    public OdsApiConnectionException(HttpStatusCode responseCode, string responseMessage, string exceptionMessage, Exception innerException) : base(exceptionMessage, innerException)
    {
        StatusCode = responseCode;
        ResponseMessage = responseMessage;
    }

    public HttpStatusCode? StatusCode { get; }
    public string ResponseMessage { get; }
    public bool AllowFeedback { get; set; }
    public bool IsStackTraceRelevant { get => false; }
}
