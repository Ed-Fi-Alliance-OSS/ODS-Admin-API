// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdFi.Ods.AdminApi.Infrastructure.Documentation;

/// <summary>
/// Swashbuckle builds the document-level "tags" list from the default per-endpoint tags (the
/// feature class names, such as ReadTenants) before <see cref="TagByResourceUrlFilter"/> replaces
/// the operation tags with the resource name. Those class names are not used by any operation,
/// so drop the list. Operation-level tags are unaffected.
/// </summary>
public class RemoveDocumentTagsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context) => swaggerDoc.Tags = new HashSet<OpenApiTag>();
}
