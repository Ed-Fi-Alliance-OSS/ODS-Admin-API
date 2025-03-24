// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;

public class InstanceCommon(AdminConsoleSettings options, IUsersContext context)
{
    private readonly AdminConsoleSettings _options = options;
    private readonly IUsersContext _context = context;

    public static OdsInstance NewOdsInstance(Instance adminConsoleInstance)
    {
        var newOdsInstance = new OdsInstance()
        {
            Name = adminConsoleInstance.InstanceName,
            InstanceType = adminConsoleInstance.InstanceType,
            ConnectionString = string.Empty,
        };

        if (adminConsoleInstance.OdsInstanceContexts != null && adminConsoleInstance.OdsInstanceContexts.Count > 0)
        {
            newOdsInstance.OdsInstanceContexts = [];
            foreach (var adminConsoleOdsInstanceContext in adminConsoleInstance.OdsInstanceContexts)
            {
                var odsInstanceContext = new Admin.DataAccess.Models.OdsInstanceContext()
                {
                    ContextKey = adminConsoleOdsInstanceContext.ContextKey,
                    ContextValue = adminConsoleOdsInstanceContext.ContextValue,
                };
                newOdsInstance.OdsInstanceContexts.Add(odsInstanceContext);
            }
        }

        if (adminConsoleInstance.OdsInstanceDerivatives != null && adminConsoleInstance.OdsInstanceDerivatives.Count > 0)
        {
            newOdsInstance.OdsInstanceDerivatives = [];
            foreach (var adminConsoleOdsInstanceDerivatives in adminConsoleInstance.OdsInstanceDerivatives)
            {
                var odsInstanceDerivative = new Admin.DataAccess.Models.OdsInstanceDerivative()
                {
                    DerivativeType = adminConsoleOdsInstanceDerivatives.DerivativeType.ToString(),
                    ConnectionString = string.Empty
                };
                newOdsInstance.OdsInstanceDerivatives.Add(odsInstanceDerivative);
            }
        }
        return newOdsInstance;
    }

    public async Task<ApiClient> NewApiClient()
    {
        var application = await _context.Applications.Where(app => app.ApplicationName.Equals(_options.ApplicationName)).FirstAsync();

        var user = await _context.Users.Where(user => user.Vendor.VendorName.Equals(_options.VendorCompany)).FirstOrDefaultAsync();

        var apiClient = new ApiClient(true)
        {
            Name = _options.ApplicationName,
            KeyStatus = "Active",
            SecretIsHashed = false,
            Application = application,
            User = user
        };

        return apiClient;
    }

}
