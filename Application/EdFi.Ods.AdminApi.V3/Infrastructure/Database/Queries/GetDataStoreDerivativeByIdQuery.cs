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

public interface IGetDataStoreDerivativeByIdQuery
{
    OdsInstanceDerivative Execute(int dataStoreDerivativeId);
}

public class GetDataStoreDerivativeByIdQuery : IGetDataStoreDerivativeByIdQuery
{
    private readonly IUsersContext _context;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider;
    private readonly IOptions<AppSettings> _options;

    public GetDataStoreDerivativeByIdQuery(IUsersContext context, ISymmetricStringEncryptionProvider encryptionProvider, IOptions<AppSettings> options)
    {
        _context = context;
        _encryptionProvider = encryptionProvider;
        _options = options;
    }

    public OdsInstanceDerivative Execute(int dataStoreDerivativeId)
    {
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(app => app.OdsInstanceDerivativeId == dataStoreDerivativeId);
        if (odsInstanceDerivative == null)
        {
            throw new NotFoundException<int>("DataStoreDerivative", dataStoreDerivativeId);
        }

        if (!string.IsNullOrEmpty(_options.Value.EncryptionKey) && !string.IsNullOrEmpty(_options.Value.DatabaseEngine))
            DataStoreEncryptionHelper.EncryptDerivativeConnectionStringsIfNeededAsync(
                new List<OdsInstanceDerivative> { odsInstanceDerivative }, _context, _encryptionProvider, _options.Value.EncryptionKey, _options.Value.DatabaseEngine).GetAwaiter().GetResult();

        return odsInstanceDerivative;
    }
}
