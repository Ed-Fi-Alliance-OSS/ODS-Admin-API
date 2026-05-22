// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class OdsInstanceEncryptionHelperTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithMixedStrings_OnlyEncryptsPlaintextAndCallsSaveChangesAsync()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        var plaintextInstance = new OdsInstance { Name = "Plain", ConnectionString = PlainConnectionString };
        var encryptedInstance = new OdsInstance { Name = "Encrypted", ConnectionString = encrypted };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { plaintextInstance, encryptedInstance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        _provider.IsEncrypted(plaintextInstance.ConnectionString).ShouldBeTrue();
        encryptedInstance.ConnectionString.ShouldBe(encrypted);
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithInvalidConnectionString_SkipsEncryptionAndDoesNotCallSaveChangesAsync()
    {
        // PlainConnectionString is SqlServer format; using PostgreSql engine makes it invalid
        var instance = new OdsInstance { Name = "I1", ConnectionString = PlainConnectionString };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "PostgreSql");

        instance.ConnectionString.ShouldBe(PlainConnectionString);
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithEmptyConnectionString_SkipsAndDoesNotCallSaveChangesAsync()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = string.Empty };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        instance.ConnectionString.ShouldBe(string.Empty);
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithNullConnectionString_SkipsAndDoesNotCallSaveChangesAsync()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = null };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        instance.ConnectionString.ShouldBeNull();
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithEmptyList_DoesNotCallSaveChangesAsync()
    {
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance>(), usersContext, _provider, TestEncryptionKey, "SqlServer");

        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithEmptyEngine_SkipsEncryptionAndDoesNotCallSaveChangesAsync()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = PlainConnectionString };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "");

        instance.ConnectionString.ShouldBe(PlainConnectionString);
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithPlaintextString_EncryptsAndCallsSaveChangesAsync()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = PlainConnectionString };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        _provider.IsEncrypted(instance.ConnectionString).ShouldBeTrue();
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithAlreadyEncryptedString_DoesNotCallSaveChangesAsync()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        var instance = new OdsInstance { Name = "I1", ConnectionString = encrypted };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        instance.ConnectionString.ShouldBe(encrypted);
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EncryptConnectionStringsIfNeededAsync_WithUnknownEngine_SkipsEncryptionAndDoesNotCallSaveChangesAsync()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = PlainConnectionString };
        var usersContext = A.Fake<IUsersContext>();

        await OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeededAsync(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "UnknownEngine");

        instance.ConnectionString.ShouldBe(PlainConnectionString);
        A.CallTo(() => usersContext.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }
}
