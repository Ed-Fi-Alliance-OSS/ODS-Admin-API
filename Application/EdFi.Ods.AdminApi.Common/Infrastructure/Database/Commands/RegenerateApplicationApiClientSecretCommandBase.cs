// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class RegenerateApplicationApiClientSecretCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected RegenerateApplicationApiClientSecretResult ExecuteCore(int applicationId)
    {
        var application = _context.Applications
            .Include(x => x.ApiClients)
            .SingleOrDefault(a => a.ApplicationId == applicationId)
            ?? throw new NotFoundException<int>("application", applicationId);

        var apiClient = application.ApiClients.First();

        apiClient.GenerateSecret();
        apiClient.SecretIsHashed = false;
        _context.SaveChanges();

        return new RegenerateApplicationApiClientSecretResult
        {
            Key = apiClient.Key,
            Secret = apiClient.Secret,
            Application = application
        };
    }
}

public class RegenerateApplicationApiClientSecretResult
{
    public string? Key { get; set; }
    public string? Secret { get; set; }
    public Application Application { get; set; } = new();
}
