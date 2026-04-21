// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditApplicationCommand
{
    Application Execute(IEditApplicationModel model);
}

public class EditApplicationCommand(IUsersContext context) : EditApplicationCommandBase(context), IEditApplicationCommand
{
    public Application Execute(IEditApplicationModel model)
    {
        return ExecuteCore(
            model.Id,
            model.ApplicationName,
            model.VendorId,
            model.ClaimSetName,
            model.ProfileIds,
            model.EducationOrganizationIds,
            model.OdsInstanceIds,
            model.Enabled);
    }
}

public interface IEditApplicationModel
{
    int Id { get; }
    string? ApplicationName { get; }
    int VendorId { get; }
    string? ClaimSetName { get; }
    IEnumerable<int>? ProfileIds { get; }
    IEnumerable<long>? EducationOrganizationIds { get; }
    IEnumerable<int>? OdsInstanceIds { get; }
    bool? Enabled { get; }
}
