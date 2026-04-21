// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class DeleteVendorCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected void ExecuteCore(int id, Action<int> deleteApplication)
    {
        var vendor = _context.Vendors
            .Include(v => v.Applications)
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .SingleOrDefault(v => v.VendorId == id) ?? throw new NotFoundException<int>("vendor", id);

        if (vendor.IsSystemReservedVendor())
        {
            throw new ArgumentException("This Vendor is required for proper system function and may not be deleted");
        }

        foreach (var application in vendor.Applications.ToList())
        {
            deleteApplication(application.ApplicationId);
        }

        foreach (var user in vendor.Users.ToList())
        {
            if (_context.ApiClients.Any())
            {
                var apiClient =
                    _context.ApiClients
                        .AsEnumerable().SingleOrDefault(o => o.User?.UserId == user?.UserId);
                if (apiClient != null)
                {
                    _context.ApiClients.Remove(apiClient);
                }
            }

            _context.Users.Remove(user);
        }

        _context.Vendors.Remove(vendor);
        _context.SaveChanges();
    }
}
