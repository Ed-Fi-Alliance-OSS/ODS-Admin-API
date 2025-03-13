// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;

namespace EdFi.Ods.AdminApi.AdminConsole.Helpers;

public class OptionsEncryptionKeyResolver(IEncryptionKeySettings encryptionKeySettings) : IEncryptionKeyResolver
{
    private readonly string _encryptionKey = encryptionKeySettings.EncryptionKey;

    public string GetEncryptionKey()
    {
        return _encryptionKey;
    }
}
