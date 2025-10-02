// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public class AddTenantCommand(IAppSettingsFileProvider fileProvider)
{
    private static readonly object _fileLock = new();
    private readonly IAppSettingsFileProvider _fileProvider = fileProvider;

    public virtual void Execute(IAddTenantModel model)
    {
        lock (_fileLock)
        {
            var json = _fileProvider.ReadAllText();
            try
            {
                var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("appsettings.json is empty or invalid.");

                var tenantsNode = root["Tenants"] as JsonObject ?? throw new InvalidOperationException("Tenants section missing in appsettings.json.");

                if (tenantsNode.ContainsKey(model.TenantName))
                {
                    throw new InvalidOperationException($"Tenant '{model.TenantName}' already exists.");
                }

                var tenantObj = new JsonObject
                {
                    ["ConnectionStrings"] = new JsonObject
                    {
                        ["EdFi_Security"] = model.EdFiSecurityConnectionString,
                        ["EdFi_Admin"] = model.EdFiAdminConnectionString
                    }
                };

                tenantsNode[model.TenantName] = tenantObj;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = root.ToJsonString(options);

                _fileProvider.WriteAllText(updatedJson);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("appsettings.json contains invalid JSON.", ex);
            }
        }
    }

}

public interface IAddTenantModel
{
    public string TenantName { get; }
    public string? EdFiSecurityConnectionString { get; }
    public string? EdFiAdminConnectionString { get; }
}
