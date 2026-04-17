// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public class Action : Enumeration<Action, string>
{
    public static readonly Action Create = new("Create", "Create");
    public static readonly Action Read = new("Read", "Read");
    public static readonly Action Update = new("Update", "Update");
    public static readonly Action Delete = new("Delete", "Delete");

    private Action(string value, string displayName) : base(value, displayName) { }
}
