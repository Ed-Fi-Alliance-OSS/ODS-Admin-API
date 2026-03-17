// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

public class DatabaseNameBuilder : IDatabaseNameBuilder
{
    private const string TemplatePrefix = "Ods_";

    private const string TemplateEmptyDatabase = TemplatePrefix + "Empty_Template";
    private const string TemplateMinimalDatabase = TemplatePrefix + "Minimal_Template";
    private const string TemplateSampleDatabase = TemplatePrefix + "Populated_Template";

    public string DemoSandboxDatabase
    {
        get => "EdFi_Ods";
    }

    public string EmptyDatabase
    {
        get => TemplateEmptyDatabase;
    }

    public string MinimalDatabase
    {
        get => TemplateMinimalDatabase;
    }

    public string SampleDatabase
    {
        get => TemplateSampleDatabase;
    }
}
