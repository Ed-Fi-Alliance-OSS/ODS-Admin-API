// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using OpenIddict.EntityFrameworkCore.Models;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Security;

public class ApiApplication : OpenIddictEntityFrameworkCoreApplication<int, ApiAuthorization, ApiToken>
{
}

public class ApiScope : OpenIddictEntityFrameworkCoreScope<int>
{
}

public class ApiAuthorization : OpenIddictEntityFrameworkCoreAuthorization<int, ApiApplication, ApiToken>
{
}

public class ApiToken : OpenIddictEntityFrameworkCoreToken<int, ApiApplication, ApiAuthorization>
{
}
