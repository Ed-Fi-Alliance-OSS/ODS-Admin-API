// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using log4net;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class DeletedEntityAuditCapture(IDeletedEntitySnapshotRegistry registry) : IDeletedEntityAuditCapture
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(DeletedEntityAuditCapture));
    private bool _recorded;

    public string? CapturedJson { get; private set; }

    public void Record(object? entity)
    {
        if (entity is null || _recorded)
        {
            return;
        }

        _recorded = true;

        try
        {
            var snapshot = registry.Project(entity);
            if (snapshot is not null)
            {
                CapturedJson = JsonSerializer.Serialize(snapshot);
            }
        }
        catch (Exception ex)
        {
            // Capturing a deleted-object snapshot must never fail the underlying delete (fail-open).
            _logger.Warn($"Failed to capture deleted-object audit snapshot for entity type {entity.GetType().Name}.", ex);
        }
    }
}
