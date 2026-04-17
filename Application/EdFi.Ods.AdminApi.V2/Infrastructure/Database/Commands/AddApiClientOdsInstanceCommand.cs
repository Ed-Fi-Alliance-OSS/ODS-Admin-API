// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;

public interface IAddApiClientOdsInstanceCommand
{
    ApiClientOdsInstance Execute(ApiClientOdsInstance newApiClientOdsInstance);
}

public class AddApiClientOdsInstanceCommand : IAddApiClientOdsInstanceCommand
{
    private readonly IUsersContext _context;

    public AddApiClientOdsInstanceCommand(IUsersContext context)
    {
        _context = context;
    }

    public ApiClientOdsInstance Execute(ApiClientOdsInstance newApiClientOdsInstance)
    {

        var apiClientOdsInstance = new ApiClientOdsInstance
        {
            ApiClient = newApiClientOdsInstance.ApiClient,
            OdsInstance = newApiClientOdsInstance.OdsInstance
        };
        _context.ApiClientOdsInstances.Add(apiClientOdsInstance);
        _context.SaveChanges();
        return apiClientOdsInstance;
    }
}
