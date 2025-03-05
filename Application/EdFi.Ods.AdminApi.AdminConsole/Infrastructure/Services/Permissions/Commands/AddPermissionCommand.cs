// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Commands;

public interface IAddPermissionCommand
{
    Task<Permission?> Execute(IAddPermissionModel permission);
}

public class AddPermissionCommand : IAddPermissionCommand
{
    private readonly ICommandRepository<Permission> _permissionCommand;

    public AddPermissionCommand(ICommandRepository<Permission> permissionCommand)
    {
        _permissionCommand = permissionCommand;
    }

    public async Task<Permission?> Execute(IAddPermissionModel permission)
    {
        try
        {
            return await _permissionCommand.AddAsync(new Permission
            {
                InstanceId = permission.InstanceId,
                TenantId = permission.TenantId,
                EdOrgId = permission.EdOrgId,
                Document = permission.Document,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }
}

public interface IAddPermissionModel
{
    int InstanceId { get; }
    int? EdOrgId { get; }
    int TenantId { get; }
    string Document { get; }
}

public class AddPermissionResult
{
    public int DocId { get; set; }
}
