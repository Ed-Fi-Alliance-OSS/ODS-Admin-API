// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Nodes;
using EdFi.Ods.AdminApi.AdminConsole.Helpers;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;

public interface IGetInstancesQuery
{
    Task<IEnumerable<Instance>> Execute();
}

public class GetInstancesQuery : IGetInstancesQuery
{
    private readonly IQueriesRepository<Instance> _instanceQuery;
    private readonly IEncryptionService _encryptionService;
    private readonly string _encryptionKey;

    public GetInstancesQuery(IQueriesRepository<Instance> instanceQuery, IEncryptionKeyResolver encryptionKeyResolver, IEncryptionService encryptionService)
    {
        _instanceQuery = instanceQuery;
        _encryptionKey = encryptionKeyResolver.GetEncryptionKey();
        _encryptionService = encryptionService;
    }
    public async Task<IEnumerable<Instance>> Execute()
    {
        return await _instanceQuery.Query()
            .Include(i => i.OdsInstanceContexts)
            .Include(i => i.OdsInstanceDerivatives)
            .AsNoTracking()
            .ToListAsync();
    }
}
