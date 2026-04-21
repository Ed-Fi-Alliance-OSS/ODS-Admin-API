// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;
using VendorUser = EdFi.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class EditVendorCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected Vendor ExecuteCore(int id, string? company, string? namespacePrefixes, string? contactName, string? contactEmailAddress)
    {
        var vendor = _context.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .SingleOrDefault(v => v.VendorId == id) ?? throw new NotFoundException<int>("vendor", id);

        if (vendor.IsSystemReservedVendor())
        {
            throw new ArgumentException("This vendor is required for proper system function and may not be modified.");
        }

        vendor.VendorName = company;

        if (vendor.VendorNamespacePrefixes.Any())
        {
            foreach (var vendorNamespacePrefix in vendor.VendorNamespacePrefixes.ToList())
            {
                _context.VendorNamespacePrefixes.Remove(vendorNamespacePrefix);
            }
        }

        var newNamespacePrefixes = namespacePrefixes?.Split(",")
            .Where(namespacePrefix => !string.IsNullOrWhiteSpace(namespacePrefix))
            .Select(namespacePrefix => new VendorNamespacePrefix
            {
                NamespacePrefix = namespacePrefix.Trim(),
                Vendor = vendor
            });

        foreach (var namespacePrefix in newNamespacePrefixes ?? Enumerable.Empty<VendorNamespacePrefix>())
        {
            _context.VendorNamespacePrefixes.Add(namespacePrefix);
        }

        if (vendor.Users?.FirstOrDefault() != null)
        {
            vendor.Users.First().FullName = contactName;
            vendor.Users.First().Email = contactEmailAddress;
        }
        else
        {
            var vendorContact = new VendorUser
            {
                Vendor = vendor,
                FullName = contactName,
                Email = contactEmailAddress
            };
            vendor.Users = new List<VendorUser> { vendorContact };
        }

        _context.SaveChanges();
        return vendor;
    }
}
