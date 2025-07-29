// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IDeleteApiClientCommand
{
    void Execute(int id);
}

public class DeleteApiClientCommand : IDeleteApiClientCommand
{
    private readonly IUsersContext _context;

    public DeleteApiClientCommand(IUsersContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var apiClient = _context.ApiClients
            .SingleOrDefault(a => a.ApiClientId == id) ?? throw new NotFoundException<int>("apiclient", id);

        var currentClientAccessTokens = _context.ClientAccessTokens.Where(o => apiClient.ApiClientId.Equals(o.ApiClient.ApiClientId));

        if (currentClientAccessTokens.Any())
        {
            _context.ClientAccessTokens.RemoveRange(currentClientAccessTokens);
        }

        var currentApiClientOdsInstances = _context.ApiClientOdsInstances.Where(o => apiClient.ApiClientId.Equals(o.ApiClient.ApiClientId));

        if (currentApiClientOdsInstances.Any())
        {
            _context.ApiClientOdsInstances.RemoveRange(currentApiClientOdsInstances);
        }

        _context.ApiClients.Remove(apiClient);
        _context.SaveChanges();
    }

}
