// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

public class ConflictException : AdminApiException
{
    public ConflictException(string message, string problemType = AdminApiProblemTypes.Conflict)
        : base(message)
    {
        StatusCode = HttpStatusCode.Conflict;
        ProblemType = problemType;
    }

    public string ProblemType { get; }
}
