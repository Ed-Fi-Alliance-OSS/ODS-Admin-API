// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetVendorByIdQueryBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected Vendor? ExecuteCore(int vendorId)
    {
        return _context.Vendors
            .Include(v => v.Users)
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Applications)
            .SingleOrDefault(v => v.VendorId == vendorId);
    }
}
