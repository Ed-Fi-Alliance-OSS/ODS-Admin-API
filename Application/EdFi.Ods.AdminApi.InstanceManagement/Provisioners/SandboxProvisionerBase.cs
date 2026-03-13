// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data.Common;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Microsoft.Extensions.Configuration;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

public abstract class SandboxProvisionerBase : ISandboxProvisioner
{
    private readonly IConfiguration _configuration;
    private readonly IConfigConnectionStringsProvider _connectionStringsProvider;
    protected readonly IDatabaseNameBuilder _databaseNameBuilder;

    protected SandboxProvisionerBase(IConfiguration configuration,
        IConfigConnectionStringsProvider connectionStringsProvider, IDatabaseNameBuilder databaseNameBuilder)
    {
        _configuration = configuration;
        _connectionStringsProvider = connectionStringsProvider;
        _databaseNameBuilder = databaseNameBuilder;

        CommandTimeout = int.TryParse(_configuration.GetSection("SandboxAdminSQLCommandTimeout").Value, out int timeout)
            ? timeout
            : 30;

        ConnectionString = _connectionStringsProvider.GetConnectionString("EdFi_Master");
    }

    protected int CommandTimeout { get; }

    protected string ConnectionString { get; }

    public void AddSandbox(string sandboxKey, SandboxType sandboxType)
        => AddSandboxAsync(sandboxKey, sandboxType).WaitSafely();

    public void DeleteSandboxes(params string[] deletedClientKeys) => DeleteSandboxesAsync(deletedClientKeys).WaitSafely();

    public void RenameSandbox(string oldName, string newName) => RenameSandboxAsync(oldName, newName).WaitSafely();

    public SandboxStatus GetSandboxStatus(string clientKey) => GetSandboxStatusAsync(clientKey).GetResultSafely();

    public async Task AddSandboxAsync(string database, SandboxType sandboxType)
    {
        await DeleteSandboxesAsync(database).ConfigureAwait(false);

        switch (sandboxType)
        {
            case SandboxType.Minimal:
                await CopySandboxAsync(
                        _databaseNameBuilder.MinimalDatabase,
                        database)
                    .ConfigureAwait(false);

                break;
            case SandboxType.Sample:
                await CopySandboxAsync(
                        _databaseNameBuilder.SampleDatabase,
                        database)
                    .ConfigureAwait(false);

                break;
            default:
                throw new System.ArgumentOutOfRangeException(nameof(sandboxType), sandboxType, "Unhandled SandboxType provided.");
        }
    }

    public abstract Task DeleteSandboxesAsync(params string[] deletedClientKeys);

    public abstract Task<SandboxStatus> GetSandboxStatusAsync(string clientKey);

    public async Task ResetDemoSandboxAsync()
    {
        var tmpName = Guid.NewGuid().ToString("N");
        await CopySandboxAsync(_databaseNameBuilder.SampleDatabase, tmpName).ConfigureAwait(false);
        await DeleteSandboxesAsync(_databaseNameBuilder.DemoSandboxDatabase).ConfigureAwait(false);
        await RenameSandboxAsync(tmpName, _databaseNameBuilder.DemoSandboxDatabase).ConfigureAwait(false);
    }

    public abstract Task RenameSandboxAsync(string oldName, string newName);

    public abstract Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName);

    protected abstract DbConnection CreateConnection();
}
