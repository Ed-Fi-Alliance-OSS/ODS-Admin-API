// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;

namespace EdFi.Ods.AdminApi.Infrastructure.Helpers;

public static class OdsInstanceEncryptionHelper
{
    public static async Task EncryptConnectionStringsIfNeededAsync(
        List<OdsInstance> instances,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        string encryptionKey,
        string databaseEngine,
        CancellationToken cancellationToken = default)
    {
        byte[] key = Convert.FromBase64String(encryptionKey);
        bool anyUpdated = false;

        foreach (var instance in instances)
        {
            if (string.IsNullOrEmpty(instance.ConnectionString))
                continue;

            if (encryptionProvider.IsEncrypted(instance.ConnectionString))
                continue;

            if (!ConnectionStringHelper.ValidateConnectionString(databaseEngine, instance.ConnectionString))
                continue;

            instance.ConnectionString = encryptionProvider.Encrypt(instance.ConnectionString, key);
            anyUpdated = true;
        }

        if (anyUpdated)
            await usersContext.SaveChangesAsync(cancellationToken);
    }
}
