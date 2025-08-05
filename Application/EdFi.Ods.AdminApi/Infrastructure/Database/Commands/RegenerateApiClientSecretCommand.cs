// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.


using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public class RegenerateApiClientSecretCommand : IRegenerateApiClientSecretCommand
{
    private readonly IUsersContext _context;

    public RegenerateApiClientSecretCommand(IUsersContext context)
    {
        _context = context;
    }

    public RegenerateApiClientSecretResult Execute(int apiClientId)
    {
        var apiClient = _context.ApiClients
            .Include(a => a.Application)
            .SingleOrDefault(a => a.ApiClientId == apiClientId);
        if (apiClient == null)
        {
            throw new NotFoundException<int>("ApiClient", apiClientId);
        }

        apiClient.GenerateSecret();
        apiClient.SecretIsHashed = false;
        _context.SaveChanges();

        return new RegenerateApiClientSecretResult
        {
            Id = apiClient.ApiClientId,
            Name = apiClient.Name,
            Key = apiClient.Key,
            Secret = apiClient.Secret,
            Application = apiClient.Application
        };
    }
}

public class RegenerateApiClientSecretResult
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Key { get; set; }
    public string? Secret { get; set; }
    public Application Application { get; set; } = new();
}
