// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public class DeleteVendorCommand
{
    private readonly IUsersContext _context;
    private readonly IDeleteApplicationCommand _deleteApplicationCommand;

    public DeleteVendorCommand(IUsersContext context, IDeleteApplicationCommand deleteApplicationCommand)
    {
        _context = context;
        _deleteApplicationCommand = deleteApplicationCommand;
    }

    public void Execute(int id)
    {
        var vendor = _context.Vendors
            .Include(x => x.Applications)
            .Include(x => x.Users)
            .SingleOrDefault(v => v.VendorId == id) ?? throw new NotFoundException<int>("vendor", id);

        if (vendor.IsSystemReservedVendor())
        {
            throw new Exception("This Vendor is required for proper system function and may not be deleted");
        }

        foreach (var application in vendor.Applications.ToList())
        {
            _deleteApplicationCommand.Execute(application.ApplicationId);
        }

        foreach (var user in vendor.Users.ToList())
        {
            _context.Users.Remove(user);
        }

        _context.Vendors.Remove(vendor);
        _context.SaveChanges();
    }
}
