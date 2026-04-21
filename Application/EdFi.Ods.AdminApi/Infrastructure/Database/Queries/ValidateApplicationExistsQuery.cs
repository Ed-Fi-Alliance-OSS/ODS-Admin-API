// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public class ValidateApplicationExistsQuery(IUsersContext context)
    : Common.Infrastructure.Database.Queries.ValidateApplicationExistsQuery(context);
