// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditApiClientCommand
{
    ApiClient Execute(IEditApiClientModel model);
}

public class EditApiClientCommand : IEditApiClientCommand
{
    private readonly IUsersContext _context;

    public EditApiClientCommand(IUsersContext context)
    {
        _context = context;
    }

    public ApiClient Execute(IEditApiClientModel model)
    {
        var apiClient = _context.ApiClients
            .SingleOrDefault(a => a.ApiClientId == model.Id) ?? throw new NotFoundException<int>("apiclient", model.Id);

        var newOdsInstances = model.OdsInstanceIds != null
            ? _context.OdsInstances.Where(p => model.OdsInstanceIds.Contains(p.OdsInstanceId))
            : null;

        var currentApiClientId = apiClient.ApiClientId;
        apiClient.Name = model.Name;
        apiClient.IsApproved = model.IsApproved;

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

public interface IEditApiClientModel
{
    int Id { get; }
    string Name { get; }
    bool IsApproved { get; }
    int ApplicationId { get; }
    IEnumerable<int>? OdsInstanceIds { get; }
}
