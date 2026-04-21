// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public class AddVendorCommand(IUsersContext context) : AddVendorCommandBase(context)
{
    public Vendor Execute(IAddVendorModel newVendor)
    {
        return ExecuteCore(newVendor.Company, newVendor.NamespacePrefixes, newVendor.ContactName, newVendor.ContactEmailAddress);
    }
}

public interface IAddVendorModel
{
    string? Company { get; }
    string? NamespacePrefixes { get; }
    string? ContactName { get; }
    string? ContactEmailAddress { get; }
}

