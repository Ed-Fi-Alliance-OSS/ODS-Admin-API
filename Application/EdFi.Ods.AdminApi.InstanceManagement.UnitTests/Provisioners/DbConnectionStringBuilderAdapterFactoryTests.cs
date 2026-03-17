// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using FakeItEasy;
using Shouldly;

namespace EdFi.Ods.AdminApi.InstanceManagement.UnitTests.Provisioners;

[TestFixture]
public class DbConnectionStringBuilderAdapterFactoryTests
{
    [Test]
    public void Get_ShouldReturnTheAdapterProvidedInConstructor()
    {
        var adapter = A.Fake<IDbConnectionStringBuilderAdapter>();
        var sut = new DbConnectionStringBuilderAdapterFactory(adapter);

        var result = sut.Get();

        result.ShouldBeSameAs(adapter);
    }

    [Test]
    public void Get_CalledMultipleTimes_ShouldReturnSameAdapterReference()
    {
        var adapter = A.Fake<IDbConnectionStringBuilderAdapter>();
        var sut = new DbConnectionStringBuilderAdapterFactory(adapter);

        var firstResult = sut.Get();
        var secondResult = sut.Get();

        firstResult.ShouldBeSameAs(adapter);
        secondResult.ShouldBeSameAs(adapter);
        secondResult.ShouldBeSameAs(firstResult);
    }
}
