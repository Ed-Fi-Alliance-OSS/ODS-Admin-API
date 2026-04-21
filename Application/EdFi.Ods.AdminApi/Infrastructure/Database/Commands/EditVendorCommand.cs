// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public class EditVendorCommand(IUsersContext context) : EditVendorCommandBase(context)
{
    public Vendor Execute(IEditVendor changedVendorData)
    {
        return ExecuteCore(
            changedVendorData.Id,
            changedVendorData.Company,
            changedVendorData.NamespacePrefixes,
            changedVendorData.ContactName,
            changedVendorData.ContactEmailAddress);
    }
}

public interface IEditVendor
{
    int Id { get; set; }
    string? Company { get; set; }
    string? NamespacePrefixes { get; set; }
    string? ContactName { get; set; }
    string? ContactEmailAddress { get; set; }
}
