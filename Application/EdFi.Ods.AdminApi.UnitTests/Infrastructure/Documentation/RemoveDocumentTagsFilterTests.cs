// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FakeItEasy;
using Microsoft.OpenApi;
using NUnit.Framework;
using Shouldly;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Documentation;

[TestFixture]
public class RemoveDocumentTagsFilterTests
{
    [Test]
    public void Should_remove_document_level_tags_and_keep_operation_tags()
    {
        var document = new OpenApiDocument
        {
            Tags = new HashSet<OpenApiTag> { new() { Name = "ReadTenants" }, new() { Name = "AddVendor" } },
            Paths = new OpenApiPaths()
        };
        var operation = new OpenApiOperation
        {
            Tags = new HashSet<OpenApiTagReference> { new("Vendors", document) }
        };
        document.Paths["/v2/vendors"] = new OpenApiPathItem
        {
            Operations = new Dictionary<HttpMethod, OpenApiOperation> { [HttpMethod.Get] = operation }
        };

        new RemoveDocumentTagsFilter().Apply(document, new DocumentFilterContext([], A.Fake<ISchemaGenerator>(), new SchemaRepository()));

        (document.Tags?.Count ?? 0).ShouldBe(0);
        operation.Tags!.Select(t => t.Name).ShouldBe(["Vendors"]);
    }
}
