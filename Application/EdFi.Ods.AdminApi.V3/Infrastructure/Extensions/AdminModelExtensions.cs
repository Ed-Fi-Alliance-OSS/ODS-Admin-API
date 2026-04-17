// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure;

public static class AdminModelExtensions
{
    public static string? ContactName(this Vendor vendor)
    {
        return vendor?.Users?.FirstOrDefault()?.FullName;
    }

    public static string? ContactEmail(this Vendor vendor)
    {
        return vendor?.Users?.FirstOrDefault()?.Email;
    }

    public static IList<int> Profiles(this Application application)
    {
        var profiles = new List<int>();
        foreach (var profile in application.Profiles)
        {
            profiles.Add(profile.ProfileId);
        }
        return profiles;
    }

    public static int? VendorId(this Application application)
    {
        return application?.Vendor?.VendorId;
    }

    public static int? OdsInstanceId(this OdsInstanceContext odsInstanceContext)
    {
        return odsInstanceContext.OdsInstance?.OdsInstanceId;
    }

    public static int? OdsInstanceId(this OdsInstanceDerivative odsInstanceDerivative)
    {
        return odsInstanceDerivative.OdsInstance?.OdsInstanceId;
    }

    public static IList<long>? EducationOrganizationIds(this Application application)
    {
        return application?.ApplicationEducationOrganizations?.Select(eu => eu.EducationOrganizationId).ToList();
    }
}
