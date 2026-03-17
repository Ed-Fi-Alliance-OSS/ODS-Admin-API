// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.InstanceManagement.Provisioners;

public interface ISandboxProvisioner
{
    void AddSandbox(string sandboxKey, SandboxType sandboxType);
    void DeleteSandboxes(params string[] databaseNames);
    void RenameSandbox(string oldName, string newName);
    SandboxStatus GetSandboxStatus(string databaseName);
    Task AddSandboxAsync(string sandboxKey, SandboxType sandboxType);
    Task DeleteSandboxesAsync(params string[] databaseNames);
    Task RenameSandboxAsync(string oldName, string newName);
    Task<SandboxStatus> GetSandboxStatusAsync(string databaseName);
    Task CopySandboxAsync(string originalDatabaseName, string newDatabaseName);
}
