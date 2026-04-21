// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetVendorsQuery
{
    List<Vendor> Execute();
    List<Vendor> Execute(CommonQueryParams commonQueryParams, int? id, string? company, string? namespacePrefixes, string? contactName, string? contactEmailAddress);
}

public class GetVendorsQuery(IUsersContext context, IOptions<AppSettings> options)
    : GetVendorsQueryCore(context, options), IGetVendorsQuery
{
}
