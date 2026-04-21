// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetOdsInstanceIdsByApiClientIdQuery
{
    IList<int> Execute(int apiClientId);
    IReadOnlyDictionary<int, IList<int>> Execute(IEnumerable<int> apiClientIds);
}

public class GetOdsInstanceIdsByApiClientIdQuery(IUsersContext context)
    : GetOdsInstanceIdsByApiClientIdQueryCore(context), IGetOdsInstanceIdsByApiClientIdQuery
{
}

