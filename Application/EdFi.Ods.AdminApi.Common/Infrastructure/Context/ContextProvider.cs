// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Context;

public interface IContextProvider<T>
{
    T? Get();
    void Set(T? context);
}

public class ContextProvider<T>(IContextStorage contextStorage) : IContextProvider<T>
{
    private static readonly string? _contextKey = typeof(T?).FullName;

    private readonly IContextStorage _contextStorage = contextStorage;

    public T? Get() => _contextStorage.GetValue<T>(_contextKey!);

    public void Set(T? context) => _contextStorage.SetValue(_contextKey!, context!);
}

