// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using VendorUser = EdFi.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class AddVendorCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected Vendor ExecuteCore(string? company, string? namespacePrefixes, string? contactName, string? contactEmailAddress)
    {
        var prefixes = namespacePrefixes?.Split(",")
            .Where(namespacePrefix => !string.IsNullOrWhiteSpace(namespacePrefix))
            .Select(namespacePrefix => new VendorNamespacePrefix
            {
                NamespacePrefix = namespacePrefix.Trim()
            })
            .ToList();

        var vendor = new Vendor
        {
            VendorName = company?.Trim(),
            VendorNamespacePrefixes = prefixes
        };

        var user = new VendorUser
        {
            FullName = contactName?.Trim(),
            Email = contactEmailAddress?.Trim()
        };

        vendor.Users.Add(user);

        _context.Vendors.Add(vendor);
        _context.SaveChanges();
        return vendor;
    }
}
