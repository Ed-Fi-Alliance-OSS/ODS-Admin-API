// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Features.Vendors;

public static class VendorMapper
{
    public static VendorModel ToModel(Vendor source)
    {
        return new VendorModel
        {
            Id = source.VendorId,
            Company = source.VendorName,
            ContactName = source.ContactName(),
            ContactEmailAddress = source.ContactEmail(),
            NamespacePrefixes = source.VendorNamespacePrefixes.ToCommaSeparated()
        };
    }

    public static List<VendorModel> ToModelList(IEnumerable<Vendor> source)
    {
        return source.Select(ToModel).ToList();
    }
}
