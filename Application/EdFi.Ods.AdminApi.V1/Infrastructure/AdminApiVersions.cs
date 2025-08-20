// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using Asp.Versioning.Builder;
using Asp.Versioning.Conventions;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public class AdminApiVersions
{
    private static bool _isInitialized;

    public static AdminApiVersion V1 = new(1.1, "v1");
    private static ApiVersionSet? _versionSet;

    public static void Initialize(WebApplication app)
    {
        if (_isInitialized) throw new InvalidOperationException("Versions are already initialized");

        _versionSet = app.NewApiVersionSet()
            .HasApiVersion(V1.Version)
            .Build();

        _isInitialized = true;
    }

    public static ApiVersionSet VersionSet
    {
        get => _versionSet ?? throw new Exception(
            "Admin API Versions have not been initialized. Call Initialize() at app startup");
    }

    public static IEnumerable<AdminApiVersion> GetAllVersions()
    {
        return typeof(AdminApiVersions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AdminApiVersion))
            .Select(field => field.GetValue(null) as AdminApiVersion)
            .Where(apiVersion => apiVersion != null)
            .ToArray()!;
    }

    public static string[] GetAllVersionStrings()
    {
        return GetAllVersions()
            .Select(apiVersion => apiVersion.ToString())
            .ToArray();
    }

    public class AdminApiVersion
    {
        public double Version { get; }
        public string DisplayName { get; }
        public override string ToString() => DisplayName;

        public AdminApiVersion(double version, string displayName)
        {
            Version = version;
            DisplayName = displayName;
        }
    }
}
