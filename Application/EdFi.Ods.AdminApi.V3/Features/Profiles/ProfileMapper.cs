// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AdminProfile = EdFi.Admin.DataAccess.Models.Profile;

namespace EdFi.Ods.AdminApi.V3.Features.Profiles;

public static class ProfileMapper
{
    public static ProfileModel ToModel(AdminProfile source)
    {
        return new ProfileModel
        {
            Id = source.ProfileId,
            Name = source.ProfileName
        };
    }

    public static ProfileDetailsModel ToDetailsModel(AdminProfile source)
    {
        return new ProfileDetailsModel
        {
            Id = source.ProfileId,
            Name = source.ProfileName,
            Definition = source.ProfileDefinition
        };
    }

    public static List<ProfileModel> ToModelList(IEnumerable<AdminProfile> source)
    {
        return source.Select(ToModel).ToList();
    }
}
