// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class AddApiClientCommandBase(IUsersContext usersContext)
{
    private readonly IUsersContext _usersContext = usersContext;

    protected AddApiClientResult ExecuteCore(string name, bool isApproved, int applicationId, IEnumerable<int>? odsInstanceIds, IOptions<Settings.AppSettings> options)
    {
        var application = _usersContext.Applications
            .Include(a => a.Vendor)
            .Single(a => a.ApplicationId == applicationId);

        var odsInstances = odsInstanceIds != null
            ? _usersContext.OdsInstances.Where(o => odsInstanceIds.Contains(o.OdsInstanceId))
            : null;

        var applicationEdOrgs = application.EducationOrganizationIds();

        var apiClient = new ApiClient(true)
        {
            Name = name,
            IsApproved = isApproved,
            Application = application,
            UseSandbox = false,
            KeyStatus = "Active",
            User = application.Vendor.Users.FirstOrDefault(),
            ApplicationEducationOrganizations = applicationEdOrgs?.Select(eu => new ApplicationEducationOrganization
            {
                EducationOrganizationId = eu,
                Application = application,
            }).ToList(),
        };

        _usersContext.ApiClients.Add(apiClient);

        if (odsInstances != null && odsInstances.Any())
        {
            foreach (var odsInstance in odsInstances)
            {
                _usersContext.ApiClientOdsInstances.Add(new ApiClientOdsInstance
                {
                    OdsInstance = odsInstance,
                    ApiClient = apiClient,
                });
            }
        }

        _usersContext.SaveChanges();

        return new AddApiClientResult
        {
            Id = apiClient.ApiClientId,
            Name = apiClient.Name,
            ApplicationId = application.ApplicationId,
            Key = apiClient.Key,
            Secret = apiClient.Secret,
        };
    }
}

public class AddApiClientResult
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

