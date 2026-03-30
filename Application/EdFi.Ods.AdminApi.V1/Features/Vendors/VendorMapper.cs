// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.V1.Features.Vendors;

public static class VendorMapper
{
    public static VendorModel ToModel(Vendor source)
    {
        return new VendorModel
        {
            VendorId = source.VendorId,
            Company = source.VendorName,
            ContactName = source.ContactName(),
            ContactEmailAddress = source.ContactEmail(),
            NamespacePrefixes = source.VendorNamespacePrefixes != null ? source.VendorNamespacePrefixes.ToCommaSeparated() : null,
        };
    }

    public static List<VendorModel> ToModelList(IEnumerable<Vendor> source)
    {
        return source.Select(ToModel).ToList();
    }
}