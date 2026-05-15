// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetOdsInstanceQuery
{
    OdsInstance Execute(int odsInstanceId);
}

public class GetOdsInstanceQuery(
    IUsersContext userContext,
    ISymmetricStringEncryptionProvider encryptionProvider,
    IOptions<AppSettings> options) : IGetOdsInstanceQuery
{
    private readonly IUsersContext _usersContext = userContext;
    private readonly ISymmetricStringEncryptionProvider _encryptionProvider = encryptionProvider;
    private readonly IOptions<AppSettings> _options = options;

    public OdsInstance Execute(int odsInstanceId)
    {
        var odsInstance = _usersContext.OdsInstances
            .Include(p => p.OdsInstanceContexts)
            .Include(p => p.OdsInstanceDerivatives)
            .SingleOrDefault(odsInstance => odsInstance.OdsInstanceId == odsInstanceId) ?? throw new NotFoundException<int>("odsInstance", odsInstanceId);

        EncryptConnectionStringIfNeeded(odsInstance);

        return odsInstance;
    }

    private void EncryptConnectionStringIfNeeded(OdsInstance odsInstance)
    {
        if (string.IsNullOrEmpty(odsInstance.ConnectionString))
            return;

        if (_encryptionProvider.IsEncrypted(odsInstance.ConnectionString))
            return;

        if (string.IsNullOrEmpty(_options.Value.EncryptionKey))
            return;

        odsInstance.ConnectionString = _encryptionProvider.Encrypt(
            odsInstance.ConnectionString,
            Convert.FromBase64String(_options.Value.EncryptionKey));

        _usersContext.SaveChanges();
    }
}



