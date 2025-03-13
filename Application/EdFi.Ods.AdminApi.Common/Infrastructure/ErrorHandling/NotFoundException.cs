// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

public interface INotFoundException
{
    public string Message { get; }
}

public class NotFoundException<T>(string resourceName, T id) : Exception($"Not found: {resourceName} with ID {id}. It may have been recently deleted."), INotFoundException
{
    public string ResourceName { get; } = resourceName;
    public T Id { get; } = id;
}
