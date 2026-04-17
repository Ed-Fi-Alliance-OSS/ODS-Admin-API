// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.Profiles;

[SwaggerSchema(Title = "Profile")]
public class ProfileModel
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

[SwaggerSchema(Title = "ProfileDetails")]
public class ProfileDetailsModel : ProfileModel
{
    public string? Definition { get; set; }
}
