// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Infrastructure;

public abstract class Enumeration<TEnumeration> : EdFi.Ods.AdminApi.Common.Infrastructure.Enumeration<TEnumeration>
        where TEnumeration : Enumeration<TEnumeration>
{
    protected Enumeration(int value, string displayName)
        : base(value, displayName)
    {
    }
}

public abstract class Enumeration<TEnumeration, TValue> : EdFi.Ods.AdminApi.Common.Infrastructure.Enumeration<TEnumeration, TValue>
    where TEnumeration : Enumeration<TEnumeration, TValue>
    where TValue : IComparable
{
    protected Enumeration(TValue value, string displayName)
        : base(value, displayName)
    {
    }
}
