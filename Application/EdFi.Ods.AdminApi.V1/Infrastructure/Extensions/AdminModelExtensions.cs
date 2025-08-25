// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using Profile = EdFi.Ods.AdminApi.V1.Features.Applications.Profile;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

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

    public static string? ProfileName(this Application application)
    {
        return application?.Profiles?.FirstOrDefault()?.ProfileName;
    }
    public static string? OdsInstanceName(this Application application)
    {
        return application?.OdsInstance?.Name;
    }

    public static IList<Profile> Profiles(this Application application)
    {
        var profiles = new List<Profile>();
        foreach (var profile in application.Profiles)
        {
            profiles.Add(new Profile { Id =  profile.ProfileId });
        }
        return profiles;
    }

    public static int? VendorId(this Application application)
    {
        return application?.Vendor?.VendorId;
    }

    public static IList<int>? EducationOrganizationIds(this Application application)
    {
        return application?.ApplicationEducationOrganizations?.Select(eu => eu.EducationOrganizationId).ToList();
    }
}
