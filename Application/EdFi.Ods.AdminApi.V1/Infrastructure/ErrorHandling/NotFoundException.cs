// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;

public interface INotFoundException
{
    public string Message { get; }
}

public class NotFoundException<T> : Exception, INotFoundException
{
    public string ResourceName { get; }
    public T Id { get; }

    public NotFoundException(string resourceName, T id)
        : base($"Not found: {resourceName} with ID {id}. It may have been recently deleted.")
    {
        ResourceName = resourceName;
        Id = id;
    }
}
