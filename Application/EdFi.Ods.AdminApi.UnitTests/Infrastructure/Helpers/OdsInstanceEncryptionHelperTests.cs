// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class OdsInstanceEncryptionHelperTests
{
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private const string PlainConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithPlaintextString_EncryptsAndCallsSaveChanges()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = PlainConnectionString };
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        _provider.IsEncrypted(instance.ConnectionString).ShouldBeTrue();
        A.CallTo(() => usersContext.SaveChanges()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithAlreadyEncryptedString_DoesNotCallSaveChanges()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        var instance = new OdsInstance { Name = "I1", ConnectionString = encrypted };
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        instance.ConnectionString.ShouldBe(encrypted);
        A.CallTo(() => usersContext.SaveChanges()).MustNotHaveHappened();
    }

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithMixedStrings_OnlyEncryptsPlaintextAndCallsSaveChanges()
    {
        var encrypted = _provider.Encrypt(PlainConnectionString, new byte[32]);
        var plaintextInstance = new OdsInstance { Name = "Plain", ConnectionString = PlainConnectionString };
        var encryptedInstance = new OdsInstance { Name = "Encrypted", ConnectionString = encrypted };
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance> { plaintextInstance, encryptedInstance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        _provider.IsEncrypted(plaintextInstance.ConnectionString).ShouldBeTrue();
        encryptedInstance.ConnectionString.ShouldBe(encrypted);
        A.CallTo(() => usersContext.SaveChanges()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithInvalidConnectionString_SkipsEncryptionAndDoesNotCallSaveChanges()
    {
        // PlainConnectionString is SqlServer format; using PostgreSql engine makes it invalid
        var instance = new OdsInstance { Name = "I1", ConnectionString = PlainConnectionString };
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "PostgreSql");

        instance.ConnectionString.ShouldBe(PlainConnectionString);
        A.CallTo(() => usersContext.SaveChanges()).MustNotHaveHappened();
    }

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithEmptyConnectionString_SkipsAndDoesNotCallSaveChanges()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = string.Empty };
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        instance.ConnectionString.ShouldBe(string.Empty);
        A.CallTo(() => usersContext.SaveChanges()).MustNotHaveHappened();
    }

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithNullConnectionString_SkipsAndDoesNotCallSaveChanges()
    {
        var instance = new OdsInstance { Name = "I1", ConnectionString = null };
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance> { instance }, usersContext, _provider, TestEncryptionKey, "SqlServer");

        instance.ConnectionString.ShouldBeNull();
        A.CallTo(() => usersContext.SaveChanges()).MustNotHaveHappened();
    }

    [Test]
    public void EncryptConnectionStringsIfNeeded_WithEmptyList_DoesNotCallSaveChanges()
    {
        var usersContext = A.Fake<IUsersContext>();

        OdsInstanceEncryptionHelper.EncryptConnectionStringsIfNeeded(
            new List<OdsInstance>(), usersContext, _provider, TestEncryptionKey, "SqlServer");

        A.CallTo(() => usersContext.SaveChanges()).MustNotHaveHappened();
    }
}
