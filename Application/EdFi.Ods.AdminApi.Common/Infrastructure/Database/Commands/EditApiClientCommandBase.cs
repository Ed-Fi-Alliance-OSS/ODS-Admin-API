// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class EditApiClientCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected ApiClient ExecuteCore(int id, string name, bool isApproved, IEnumerable<int>? odsInstanceIds)
    {
        var apiClient = _context.ApiClients
            .SingleOrDefault(a => a.ApiClientId == id)
            ?? throw new NotFoundException<int>("apiclient", id);

        var newOdsInstances = odsInstanceIds != null
            ? _context.OdsInstances.Where(p => odsInstanceIds.Contains(p.OdsInstanceId))
            : null;

        var currentApiClientId = apiClient.ApiClientId;
        apiClient.Name = name;
        apiClient.IsApproved = isApproved;

        _context.ApiClientOdsInstances.RemoveRange(_context.ApiClientOdsInstances.Where(o => o.ApiClient.ApiClientId == currentApiClientId));

        if (newOdsInstances != null)
        {
            foreach (var newOdsInstance in newOdsInstances)
            {
                _context.ApiClientOdsInstances.Add(new ApiClientOdsInstance { ApiClient = apiClient, OdsInstance = newOdsInstance });
            }
        }

        _context.SaveChanges();
        return apiClient;
    }
}
