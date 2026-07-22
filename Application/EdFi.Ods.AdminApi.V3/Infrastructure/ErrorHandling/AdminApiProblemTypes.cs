// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

public static class AdminApiProblemTypes
{
    public const string BadRequestValidation = "urn:ed-fi:management-api:bad-request:validation";
    public const string NotFound = "urn:ed-fi:management-api:not-found";
    public const string BadRequestData = "urn:ed-fi:management-api:bad-request:data";
    public const string BadRequest = "urn:ed-fi:management-api:bad-request";
    public const string InternalServerError = "urn:ed-fi:management-api:internal-server-error";
    public const string BadRequestVersionMismatch = "urn:ed-fi:management-api:bad-request:version-mismatch";
}
