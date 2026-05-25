// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Providers;

[TestFixture]
public class Aes256SymmetricStringEncryptionProviderTests
{
    private readonly Aes256SymmetricStringEncryptionProvider _provider = new();
    private static readonly byte[] TestKey = new byte[32];

    [Test]
    public void IsEncrypted_WithNull_ReturnsFalse()
    {
        _provider.IsEncrypted(null).ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithEmptyString_ReturnsFalse()
    {
        _provider.IsEncrypted(string.Empty).ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithWhitespace_ReturnsFalse()
    {
        _provider.IsEncrypted("   ").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithPlainConnectionString_ReturnsFalse()
    {
        _provider.IsEncrypted("Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False")
            .ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithTwoPipeSegments_ReturnsFalse()
    {
        var part = Convert.ToBase64String(new byte[16]);
        _provider.IsEncrypted($"{part}|{part}").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithFourPipeSegments_ReturnsFalse()
    {
        var part = Convert.ToBase64String(new byte[16]);
        _provider.IsEncrypted($"{part}|{part}|{part}|{part}").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithNonBase64FirstSegment_ReturnsFalse()
    {
        var validPart = Convert.ToBase64String(new byte[16]);
        _provider.IsEncrypted($"not-valid-base64!!|{validPart}|{validPart}").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithNonBase64SecondSegment_ReturnsFalse()
    {
        var validIv = Convert.ToBase64String(new byte[16]);
        var validHmac = Convert.ToBase64String(new byte[32]);
        _provider.IsEncrypted($"{validIv}|not-valid!!|{validHmac}").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithNonBase64ThirdSegment_ReturnsFalse()
    {
        var validIv = Convert.ToBase64String(new byte[16]);
        var validBody = Convert.ToBase64String(new byte[32]);
        _provider.IsEncrypted($"{validIv}|{validBody}|not-valid!!").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithIvNotSixteenBytes_ReturnsFalse()
    {
        var shortIv = Convert.ToBase64String(new byte[8]);
        var validBody = Convert.ToBase64String(new byte[32]);
        var validHmac = Convert.ToBase64String(new byte[32]);
        _provider.IsEncrypted($"{shortIv}|{validBody}|{validHmac}").ShouldBeFalse();
    }

    [Test]
    public void IsEncrypted_WithActualEncryptedValue_ReturnsTrue()
    {
        var encrypted = _provider.Encrypt("Data Source=(local);Initial Catalog=EdFi_Ods", TestKey);

        _provider.IsEncrypted(encrypted).ShouldBeTrue();
    }

    [Test]
    public void IsEncrypted_CalledTwiceOnSameValue_ReturnsTrueBothTimes()
    {
        var encrypted = _provider.Encrypt("Server=myServer;Database=myDb;", TestKey);

        _provider.IsEncrypted(encrypted).ShouldBeTrue();
        _provider.IsEncrypted(encrypted).ShouldBeTrue();
    }

    [Test]
    public void IsEncrypted_WithDifferentKeysProducingDifferentCipherText_BothReturnTrue()
    {
        var key1 = new byte[32];
        var key2 = new byte[32];
        key2[0] = 0xFF;

        var encrypted1 = _provider.Encrypt("connection string", key1);
        var encrypted2 = _provider.Encrypt("connection string", key2);

        _provider.IsEncrypted(encrypted1).ShouldBeTrue();
        _provider.IsEncrypted(encrypted2).ShouldBeTrue();
    }
}
