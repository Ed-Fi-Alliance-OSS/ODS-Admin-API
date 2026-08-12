// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;

public static class DataStoreEncryptionHelper
{
    public static Task EncryptConnectionStringsIfNeededAsync(
        List<OdsInstance> instances,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        string encryptionKey,
        string databaseEngine,
        CancellationToken cancellationToken = default)
    {
        return EncryptConnectionStringsIfNeededAsync(
            instances,
            static instance => instance.ConnectionString,
            static (instance, value) => instance.ConnectionString = value,
            usersContext,
            encryptionProvider,
            encryptionKey,
            databaseEngine,
            cancellationToken);
    }

    public static Task EncryptDerivativeConnectionStringsIfNeededAsync(
        List<OdsInstanceDerivative> derivatives,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        string encryptionKey,
        string databaseEngine,
        CancellationToken cancellationToken = default)
    {
        return EncryptConnectionStringsIfNeededAsync(
            derivatives,
            static derivative => derivative.ConnectionString,
            static (derivative, value) => derivative.ConnectionString = value,
            usersContext,
            encryptionProvider,
            encryptionKey,
            databaseEngine,
            cancellationToken);
    }

    /// <summary>
    /// Shared skip/encrypt/persist logic for any entity that has a plaintext-or-encrypted
    /// <c>ConnectionString</c> property. <typeparamref name="T"/> is accessed structurally via
    /// <paramref name="getConnectionString"/>/<paramref name="setConnectionString"/> rather than a
    /// shared interface, because the entity types (<see cref="OdsInstance"/>,
    /// <see cref="OdsInstanceDerivative"/>) are defined in the external EdFi.Suite3.Admin.DataAccess
    /// package and can't be retrofitted to implement one.
    /// </summary>
    private static async Task EncryptConnectionStringsIfNeededAsync<T>(
        List<T> items,
        Func<T, string?> getConnectionString,
        Action<T, string> setConnectionString,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        string encryptionKey,
        string databaseEngine,
        CancellationToken cancellationToken)
    {
        byte[] key = Convert.FromBase64String(encryptionKey);
        bool anyUpdated = false;

        foreach (var item in items)
        {
            string? connectionString = getConnectionString(item);

            if (string.IsNullOrEmpty(connectionString))
                continue;

            if (encryptionProvider.IsEncrypted(connectionString))
                continue;

            if (!ConnectionStringHelper.ValidateConnectionString(databaseEngine, connectionString))
                continue;

            setConnectionString(item, encryptionProvider.Encrypt(connectionString, key));
            anyUpdated = true;
        }

        if (anyUpdated)
            await usersContext.SaveChangesAsync(cancellationToken);
    }
}
