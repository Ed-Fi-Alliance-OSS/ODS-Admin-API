// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Infrastructure.Helpers;

public interface IAppSettingsFileProvider
{
    string ReadAllText();
    void WriteAllText(string content);
}

public class FileSystemAppSettingsFileProvider(string filePath) : IAppSettingsFileProvider
{
    public string ReadAllText()
    {
        return File.ReadAllText(filePath);
    }

    public void WriteAllText(string content)
    {
        File.WriteAllText(filePath, content);
    }
}
