// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services;

public interface ISimpleGetRequest
{
    public Task<string> DownloadString(string address);
}

public class SimpleGetRequest : ISimpleGetRequest
{
    private readonly IHttpClientFactory _clientFactory;

    public SimpleGetRequest(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<string> DownloadString(string address)
    {
        var httpClient = _clientFactory.CreateClient();

        using (var response = await httpClient.GetAsync(address))
        {
            await CheckResponseStatusCode(address, response);

            using (var content = response.Content)
            {
                return await content.ReadAsStringAsync();
            }
        }

        static async Task CheckResponseStatusCode(string requestUrl, HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case 0:
                    throw new HttpRequestException($"No response from {requestUrl}.");
                case HttpStatusCode.NotFound:
                    throw new HttpRequestException($"{requestUrl} not found.", null, HttpStatusCode.NotFound);
                case HttpStatusCode.ServiceUnavailable:
                    throw new HttpRequestException(
                        $"{requestUrl} is unavailable", null, HttpStatusCode.ServiceUnavailable);
                case HttpStatusCode.BadGateway:
                    throw new HttpRequestException(
                        $"{requestUrl} was acting as a gateway or proxy and received an invalid response from the upstream server.",
                        null, HttpStatusCode.BadGateway);
                case (HttpStatusCode)495:
                    throw new HttpRequestException(
                        $"Invalid SSL client certificate for {requestUrl}.", null, (HttpStatusCode)495);
                case (HttpStatusCode)496:
                    throw new HttpRequestException(
                        $"Missing SSL client certificate for {requestUrl}.", null, (HttpStatusCode)496);
                case HttpStatusCode.BadRequest:
                    throw new InvalidOperationException($"Malformed request for {requestUrl}.");
                default:
                    var message = $"Unexpected response from {requestUrl}";

                    using (var content = response.Content)
                    {
                        var details = await content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(details))
                            message += $": {details}";
                    }

                    throw new HttpRequestException(message, null, HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
