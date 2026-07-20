// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Threading.Tasks;
using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreContexts;
[TestFixture] public class DeleteDataStoreContextTests {
    [Test] public async Task Handle_ExecutesDeleteAndReturnsNoContent() {
        var fakeDelete = A.Fake<IDeleteDataStoreContextCommand>();
        var result = await DeleteDataStoreContext.Handle(fakeDelete, 1);
        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        A.CallTo(() => fakeDelete.Execute(1)).MustHaveHappenedOnceExactly();
    }
}
