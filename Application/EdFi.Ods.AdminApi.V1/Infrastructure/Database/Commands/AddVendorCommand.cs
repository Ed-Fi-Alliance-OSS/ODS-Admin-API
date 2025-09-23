// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using VendorUser = EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public class AddVendorCommand
{
    private readonly IUsersContext _context;

    public AddVendorCommand(IUsersContext context)
    {
        _context = context;
    }

    public Vendor Execute(IAddVendorModel newVendor)
    {
        var namespacePrefixes = newVendor.NamespacePrefixes?.Split(",")
            .Where(namespacePrefix => !string.IsNullOrWhiteSpace(namespacePrefix))
            .Select(namespacePrefix => new VendorNamespacePrefix
            {
                NamespacePrefix = namespacePrefix.Trim(),
                Vendor = new Vendor()
            })
            .ToList();

        var vendor = new Vendor
        {
            VendorName = newVendor.Company.Trim(),
            VendorNamespacePrefixes = namespacePrefixes
        };

        var user = new VendorUser
        {
            FullName = newVendor.ContactName.Trim(),
            Email = newVendor.ContactEmailAddress.Trim(),
            Vendor = vendor
        };

        vendor.Users.Add(user);

        _context.Vendors.Add(vendor);
        _context.SaveChanges();
        return vendor;
    }
}

public interface IAddVendorModel
{
    string Company { get; }
    string? NamespacePrefixes { get; }
    string ContactName { get; }
    string ContactEmailAddress { get; }
}
