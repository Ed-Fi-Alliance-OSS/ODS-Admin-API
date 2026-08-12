// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreQuery
{
    OdsInstance Execute(int id);
}

public class GetDataStoreQuery(
    IUsersContext userContext,
    ISymmetricStringEncryptionProvider encryptionProvider,
    IOptions<AppSettings> options) : IGetDataStoreQuery
{
    private readonly IUsersContext _usersContext = userContext;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider = encryptionProvider;
    private readonly IOptions<AppSettings> _options = options;

    // Note: this is called both by the GET /v3/dataStores/{id} endpoint and by several
    // existence-check validators elsewhere (EditDataStore, AddDataStoreContext,
    // EditDataStoreContext, DeleteDataStore, AddDataStoreDerivative, EditDataStoreDerivative).
    // As a side effect, it backfill-encrypts this data store's own connection string and its
    // (small, bounded) set of derivatives if either is still stored in plaintext. This is
    // intentional and pre-existing for the primary connection string; not a bug if you see a
    // SaveChangesAsync fire from a validator call site.
    public OdsInstance Execute(int id)
    {
        var dataStore = _usersContext.OdsInstances
            .Include(p => p.OdsInstanceContexts)
            .Include(p => p.OdsInstanceDerivatives)
            .SingleOrDefault(o => o.OdsInstanceId == id)
            ?? throw new NotFoundException<int>("DataStore", id);

        if (!string.IsNullOrEmpty(_options.Value.EncryptionKey) && !string.IsNullOrEmpty(_options.Value.DatabaseEngine))
        {
            DataStoreEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
                new List<OdsInstance> { dataStore }, _usersContext, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();
            DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
                dataStore.OdsInstanceDerivatives.ToList(), _usersContext, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();
        }

        return dataStore;
    }
}



