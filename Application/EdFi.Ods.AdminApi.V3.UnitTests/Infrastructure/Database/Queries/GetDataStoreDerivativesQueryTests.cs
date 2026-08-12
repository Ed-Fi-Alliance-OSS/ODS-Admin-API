// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreDerivativesQueryTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";
    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreDerivatives_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
    private IOptions<AppSettings> OptionsWithEncryption() =>
        Options.Create(new AppSettings { DatabaseEngine = "SqlServer", DefaultPageSizeLimit = 25, EncryptionKey = TestEncryptionKey });

    [Test]
    public void Execute_ReturnsList()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        ctx.OdsInstanceDerivatives.Add(new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = "cs-replica" });
        ctx.SaveChanges();
        var result = new GetDataStoreDerivativesQuery(ctx, DefaultOptions(), _provider).Execute(new CommonQueryParams(0, 25));
        result.Count.ShouldBe(1);
    }

    [Test]
    public void Execute_WithUnencryptedConnectionString_EncryptsOnRead()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        ctx.OdsInstanceDerivatives.Add(new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = PlainConnectionString });
        ctx.SaveChanges();

        // Explicit OrderBy avoids the default sort column (DerivativeType), which wraps EF.Functions.Collate
        // when DatabaseEngine is SqlServer -- unsupported by the EF Core InMemory provider used in this test.
        var result = new GetDataStoreDerivativesQuery(ctx, OptionsWithEncryption(), _provider)
            .Execute(new CommonQueryParams(0, 25, SortingColumns.DefaultIdColumn, null));

        _provider.IsEncrypted(result.Single().ConnectionString).ShouldBeTrue();
    }
}
