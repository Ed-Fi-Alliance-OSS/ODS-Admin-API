// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public class RegenerateApiClientSecretCommand
{
    private readonly IUsersContext _context;

    public RegenerateApiClientSecretCommand(IUsersContext context)
    {
        _context = context;
    }

    public RegenerateApiClientSecretResult Execute(int applicationId)
    {
        var application = _context.Applications
            .Include(x => x.ApiClients)
            .SingleOrDefault(a => a.ApplicationId == applicationId);
        if (application == null)
        {
            throw new NotFoundException<int>("application", applicationId);
        }

        var apiClient = application.ApiClients.First();

        apiClient.GenerateSecret();
        apiClient.SecretIsHashed = false;
        _context.SaveChanges();

        return new RegenerateApiClientSecretResult
        {
            Key = apiClient.Key,
            Secret = apiClient.Secret,
            Application = application
        };
    }
}

public class RegenerateApiClientSecretResult
{
    public string? Key { get; set; }
    public string? Secret { get; set; }
    public Application Application { get; set; } = new() { OperationalContextUri = string.Empty };
}
