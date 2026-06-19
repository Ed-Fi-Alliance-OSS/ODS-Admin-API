// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public interface IAddApiClientDataStoreCommand
{
    ApiClientOdsInstance Execute(ApiClientOdsInstance newApiClientDataStore);
}

public class AddApiClientDataStoreCommand : IAddApiClientDataStoreCommand
{
    private readonly IUsersContext _context;

    public AddApiClientDataStoreCommand(IUsersContext context)
    {
        _context = context;
    }

    public ApiClientOdsInstance Execute(ApiClientOdsInstance newApiClientDataStore)
    {

        var apiClientDataStore = new ApiClientOdsInstance
        {
            ApiClient = newApiClientDataStore.ApiClient,
            OdsInstance = newApiClientDataStore.OdsInstance
        };
        _context.ApiClientOdsInstances.Add(apiClientDataStore);
        _context.SaveChanges();
        return apiClientDataStore;
    }
}

