// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.AdminConsole.Features.Healthcheck;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.AutoMapper;

public class AdminConsoleMappingProfile : Profile
{
    public AdminConsoleMappingProfile()
    {
        CreateMap<HealthCheck, HealthCheckModel?>().ConvertUsing(src => JsonConvert.DeserializeObject<HealthCheckModel>(src.Document));

    }
}
