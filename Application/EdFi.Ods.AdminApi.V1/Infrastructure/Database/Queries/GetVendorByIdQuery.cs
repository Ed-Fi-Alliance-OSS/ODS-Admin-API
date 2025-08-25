// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public interface IGetVendorByIdQuery
{
    Vendor? Execute(int vendorId);
}

public class GetVendorByIdQuery : IGetVendorByIdQuery
{
    private readonly IUsersContext _context;

    public GetVendorByIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public Vendor? Execute(int vendorId)
    {
        return _context.Vendors
            .Include(x => x.VendorNamespacePrefixes)
            .Include(x => x.Users)
            .SingleOrDefault(v => v.VendorId == vendorId);
    }
}
