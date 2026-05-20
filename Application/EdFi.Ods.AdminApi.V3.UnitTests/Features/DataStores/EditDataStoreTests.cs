// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using FluentValidation;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores
{
    [TestFixture]
    public class EditDataStoreTests
    {
        [Test]
        public void Handle_WithMismatchedBodyId_ThrowsValidationException()
        {
            var request = new EditDataStore.EditDataStoreRequest
            {
                Id = 999,
                Name = "Test Data Store",
                DataStoreType = "Ods",
                ConnectionString = "Server=(local);Database=Test;Trusted_Connection=True;Encrypt=False"
            };

            var exception = Should.Throw<ValidationException>(() => EditDataStore.Handle(null!, null!, null!, null!, request, 1).GetAwaiter().GetResult());

            exception.Errors.Single(x => x.PropertyName == nameof(request.Id)).ErrorMessage
                .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
        }
    }
}