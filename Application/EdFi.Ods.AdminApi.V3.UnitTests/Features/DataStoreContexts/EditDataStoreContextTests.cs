// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using FluentValidation;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreContexts
{
    [TestFixture]
    public class EditDataStoreContextTests
    {
        [Test]
        public void Handle_WithMismatchedBodyId_ThrowsValidationException()
        {
            var request = new EditDataStoreContext.EditDataStoreContextRequest
            {
                Id = 999,
                DataStoreId = 1,
                ContextKey = "key",
                ContextValue = "value"
            };

            var exception = Should.Throw<ValidationException>(() => EditDataStoreContext.Handle(null!, null!, request, 1).GetAwaiter().GetResult());

            exception.Errors.Single(x => x.PropertyName == nameof(request.Id)).ErrorMessage
                .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
        }
    }
}