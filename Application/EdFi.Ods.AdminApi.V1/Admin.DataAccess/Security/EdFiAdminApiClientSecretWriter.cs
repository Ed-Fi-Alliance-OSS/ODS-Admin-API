// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Common.Security;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Repositories;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Security
{
    public class EdFiAdminApiClientSecretWriter : IApiClientSecretWriter
    {
        private readonly IApiClientDetailsProvider _apiClientDetailsProvider;
        private readonly IClientAppRepo _clientAppRepo;

        public EdFiAdminApiClientSecretWriter(
            IApiClientDetailsProvider apiClientDetailsProvider,
            IClientAppRepo clientAppRepo)
        {
            _apiClientDetailsProvider = apiClientDetailsProvider;
            _clientAppRepo = clientAppRepo;
        }

        public void SetSecret(string key, ApiClientSecret secret)
        {
            var client = GetClientByKey(key);

            client.Secret = secret.Secret;
            client.SecretIsHashed = secret.IsHashed;
            _clientAppRepo.UpdateClient(client);
        }

        private ApiClient GetClientByKey(string key)
        {
            var client = _clientAppRepo.GetClientByKey(key);

            if (client == null)
            {
                throw new ArgumentException($"Invalid key:'{key}'");
            }

            return client;
        }
    }
}
