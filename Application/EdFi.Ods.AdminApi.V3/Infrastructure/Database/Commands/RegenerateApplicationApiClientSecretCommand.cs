// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Features;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public class RegenerateApplicationApiClientSecretCommand
{
    private readonly IUsersContext _context;

    public RegenerateApplicationApiClientSecretCommand(IUsersContext context)
    {
        _context = context;
    }

    public RegenerateApplicationApiClientSecretResult Execute(int applicationId)
    {
        var application = _context.Applications
            .Include(x => x.ApiClients)
            .SingleOrDefault(a => a.ApplicationId == applicationId);
        if (application == null)
        {
            throw new NotFoundException<int>("application", applicationId);
        }

        if (application.ApiClients.Count == 0)
        {
            throw new AdminApiException(FeatureConstants.ApplicationResetCredentialNoApiClientsConflictMessage)
            {
                StatusCode = HttpStatusCode.Conflict
            };
        }

        if (application.ApiClients.Count > 1)
        {
            throw new AdminApiException(FeatureConstants.ApplicationResetCredentialMultiClientConflictMessage)
            {
                StatusCode = HttpStatusCode.Conflict
            };
        }

        var apiClient = application.ApiClients.Single();

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



