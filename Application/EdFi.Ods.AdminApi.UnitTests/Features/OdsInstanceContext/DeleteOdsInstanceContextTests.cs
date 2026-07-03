// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.OdsInstanceContext;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstanceContext;
[TestFixture] public class DeleteOdsInstanceContextTests {
    [Test] public async Task Handle_ExecutesDeleteAndReturnsOk() {
        var fakeDelete = A.Fake<IDeleteOdsInstanceContextCommand>();
        var result = await DeleteOdsInstanceContext.Handle(fakeDelete, 1);
        result.ShouldNotBeNull();
        A.CallTo(() => fakeDelete.Execute(1)).MustHaveHappenedOnceExactly();
    }
}
