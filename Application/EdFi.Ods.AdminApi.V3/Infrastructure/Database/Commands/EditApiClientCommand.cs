// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

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
            .Include(c => c.Application)
            .Include(c => c.ApplicationEducationOrganizations)
            .SingleOrDefault(a => a.ApiClientId == model.Id) ?? throw new NotFoundException<int>("apiclient", model.Id);

        var target = _context.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Vendor)
                .ThenInclude(v => v.Users)
            .SingleOrDefault(a => a.ApplicationId == model.ApplicationId)
                ?? throw new ConflictException(
                    $"Application with ID {model.ApplicationId} not found.",
                    AdminApiProblemTypes.ConflictUnresolvedReference);

        var isMove = target.ApplicationId != apiClient.Application!.ApplicationId;

        var duplicateName = _context.ApiClients.Any(c =>
            c.Application!.ApplicationId == target.ApplicationId
            && c.ApiClientId != apiClient.ApiClientId
            && c.Name == model.Name);

        if (duplicateName)
        {
            throw new ConflictException(FeatureConstants.ApiClientCombinedKeyMustBeUnique);
        }

        var current = new ClientEducationOrganizationScope(
            _context.GetInheritsApplicationEducationOrganizations(apiClient),
            apiClient.ApplicationEducationOrganizations.Select(r => r.EducationOrganizationId).ToList());

        // Resolve before mutating anything so a rejected request leaves nothing to save.
        var scope = ClientEducationOrganizationScopeResolver.Resolve(
            model.EducationOrganizationIds,
            model.InheritsApplicationEducationOrganizations,
            target.ApplicationEducationOrganizations.Select(r => r.EducationOrganizationId).ToList(),
            current,
            isMove);

        // Clearing the client's collection removes only the join rows, not the
        // ApplicationEducationOrganization rows themselves.
        apiClient.ApplicationEducationOrganizations.Clear();

        if (isMove)
        {
            apiClient.Application = target;
            apiClient.User = target.Vendor.Users.FirstOrDefault();
        }

        foreach (var row in target.ApplicationEducationOrganizations
            .Where(r => scope.EducationOrganizationIds.Contains(r.EducationOrganizationId)))
        {
            apiClient.ApplicationEducationOrganizations.Add(row);
        }

        _context.SetInheritsApplicationEducationOrganizations(apiClient, scope.Inherits);

        var newOdsInstances = model.DataStoreIds != null
            ? _context.OdsInstances.Where(p => model.DataStoreIds.Contains(p.OdsInstanceId))
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
    IEnumerable<int>? DataStoreIds { get; }
    IEnumerable<long>? EducationOrganizationIds { get; }
    bool? InheritsApplicationEducationOrganizations { get; }
}



