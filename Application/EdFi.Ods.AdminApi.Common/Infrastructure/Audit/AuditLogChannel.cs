// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Channels;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLogChannel
{
    private readonly Channel<AuditEvent> _channel = Channel.CreateBounded<AuditEvent>(
        new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<AuditEvent> Writer => _channel.Writer;

    public ChannelReader<AuditEvent> Reader => _channel.Reader;
}
