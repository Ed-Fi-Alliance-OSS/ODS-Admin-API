// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

/// <summary>
/// E2E tests for the job status tracking feature.
/// 
/// These tests verify the complete flow of:
/// 1. Triggering education organization refresh (POST /odsInstances/edOrgs/refresh)
/// 2. Polling job status (GET /jobs/{jobId})
/// 3. Waiting for job completion
/// 4. Verifying response structure and status transitions
/// 
/// Requirements:
/// - API must be running on http://localhost:5000 (or ADMIN_API_URL environment variable)
/// - Database must be accessible and job scheduler must be running
/// - Run with: dotnet test --filter "Category=E2E"
/// 
/// To run E2E tests:
/// 1. Start API: ./build.ps1 -Command Run
/// 2. In another terminal: dotnet test --filter "Category=E2E"
/// </summary>
[TestFixture]
[Category("E2E")]
public class RefreshAndJobStatusE2ETests
{
        private HttpClient _httpClient = null!;
        private const int PollIntervalMs = 500;
        private const int MaxPollAttempts = 120; // 60 seconds max wait time
        private static bool _apiAvailable = false;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var apiUrl = Environment.GetEnvironmentVariable("ADMIN_API_URL") ?? "http://localhost:5000";
            _httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
        
            // Verify API is accessible
            try
            {
                var healthTask = _httpClient.GetAsync("/health", HttpCompletionOption.ResponseHeadersRead);
                if (healthTask.Wait(TimeSpan.FromSeconds(2)))
                {
                    _apiAvailable = true;
                }
            }
            catch
            {
                // API not available
                _apiAvailable = false;
            }
        
            if (!_apiAvailable)
            {
                Assert.Ignore($"Admin API not accessible at {apiUrl}. " +
                    "E2E tests require a running API instance. " +
                    "Start API with: ./build.ps1 -Command Run");
            }
        }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// Test Scenario 1: Refresh all education organizations and poll status
    /// 
    /// Flow:
    /// 1. POST /v2/odsInstances/edOrgs/refresh → 201 Created
    /// 2. GET /v2/jobs/{jobId} → 200 OK with Pending status
    /// 3. Poll until completion
    /// 4. Verify final status is Completed or Error
    /// </summary>
    [Test]
    public async Task RefreshAllEdOrgs_AndPollStatus_CompleteFlow_V2()
    {
        // Step 1: Trigger refresh of all education organizations
        var refreshResponse = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);
        
        // Verify 201 Created response
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        // Parse response body
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshJson = JsonDocument.Parse(refreshContent);
        var jobId = refreshJson.RootElement.GetProperty("jobId").GetString();
        jobId.ShouldNotBeNullOrEmpty();
        
        // Verify response structure
        var status = refreshJson.RootElement.GetProperty("status").GetString();
        status.ShouldBe("Pending");
        refreshJson.RootElement.GetProperty("createdAt").GetDateTime().ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        
        // Step 2: Verify Location header points to GET endpoint
        refreshResponse.Headers.Location.ShouldNotBeNull();
        refreshResponse.Headers.Location!.OriginalString.ShouldBe($"/v2/jobs/{jobId}");
        
        // Step 3: Poll for job completion
        var completedStatus = await PollJobStatus(jobId, "/v2/jobs/{0}");
        
        // Step 4: Verify final status
        completedStatus.ShouldNotBeNullOrEmpty();
        var finalStatus = completedStatus switch
        {
            "Completed" or "Error" or "InProgress" or "Pending" => completedStatus,
            _ => throw new AssertionException($"Unexpected job status: {completedStatus}")
        };
        
        // For this test, we're primarily verifying the flow works
        // In a real scenario with actual job execution, status would be Completed
        TestContext.Out.WriteLine($"Final job status: {finalStatus}");
    }

    /// <summary>
    /// Test Scenario 2: Refresh specific ODS instance by instance ID
    /// 
    /// Flow:
    /// 1. POST /v2/odsInstances/{instanceId}/edOrgs/refresh → 201 Created
    /// 2. GET /v2/jobs/{jobId} → 200 OK
    /// 3. Poll until completion
    /// 4. Verify Location header and response structure
    /// </summary>
    [Test]
    public async Task RefreshEdOrgsByInstance_AndPollStatus_V2()
    {
        // Note: Using instance ID 1 which should exist in test database
        var instanceId = 1;
        
        // Step 1: Trigger refresh for specific instance
        var refreshResponse = await _httpClient.PostAsync($"/v2/odsInstances/{instanceId}/edOrgs/refresh", null);
        
        // Verify 201 Created response
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        // Parse response
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshJson = JsonDocument.Parse(refreshContent);
        var jobId = refreshJson.RootElement.GetProperty("jobId").GetString();
        jobId.ShouldNotBeNullOrEmpty();
        
        // Step 2: Verify Location header
        refreshResponse.Headers.Location.ShouldNotBeNull();
        refreshResponse.Headers.Location!.OriginalString.ShouldBe($"/v2/jobs/{jobId}");
        
        // Step 3: Poll for completion
        var completedStatus = await PollJobStatus(jobId, "/v2/jobs/{0}");
        completedStatus.ShouldNotBeNullOrEmpty();
        
        TestContext.Out.WriteLine($"Job {jobId} final status: {completedStatus}");
    }

    /// <summary>
    /// Test Scenario 3: V3 API returns correct version in Location header
    /// 
    /// Flow:
    /// 1. POST /v3/dataStores/edOrgs/refresh → 201 Created
    /// 2. Verify Location header uses /v3/jobs/{jobId} (not /v2)
    /// 3. GET /v3/jobs/{jobId} → 200 OK
    /// 4. Verify response matches expected structure
    /// </summary>
    [Test]
    public async Task RefreshAllEdOrgs_V3_ReturnsCorrectVersionInLocationHeader()
    {
        // Step 1: Trigger refresh using V3 endpoint
        var refreshResponse = await _httpClient.PostAsync("/v3/dataStores/edOrgs/refresh", null);
        
        // Verify 201 Created
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        // Parse response
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshJson = JsonDocument.Parse(refreshContent);
        var jobId = refreshJson.RootElement.GetProperty("jobId").GetString();
        jobId.ShouldNotBeNullOrEmpty();
        
        // Step 2: Verify Location header uses /v3 (not /v2)
        refreshResponse.Headers.Location.ShouldNotBeNull();
        var locationHeader = refreshResponse.Headers.Location!.OriginalString;
        locationHeader.ShouldStartWith("/v3/jobs/");
        locationHeader.ShouldBe($"/v3/jobs/{jobId}");
        
        // Step 3: Poll using V3 endpoint
        var completedStatus = await PollJobStatus(jobId, "/v3/jobs/{0}");
        completedStatus.ShouldNotBeNullOrEmpty();
        
        TestContext.Out.WriteLine($"V3 job {jobId} final status: {completedStatus}");
    }

    /// <summary>
    /// Test Scenario 4: V3 instance-specific refresh endpoint
    ///
    /// Verifies that V3 endpoints work correctly for instance-specific refresh
    /// </summary>
    [Test]
    public async Task RefreshEdOrgsByInstance_V3_ReturnsCorrectVersionInLocationHeader()
    {
        var instanceId = 1;
        
        // Step 1: Trigger refresh using V3 endpoint
        var refreshResponse = await _httpClient.PostAsync($"/v3/dataStores/{instanceId}/edOrgs/refresh", null);
        
        // Verify 201 Created
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        // Parse response
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshJson = JsonDocument.Parse(refreshContent);
        var jobId = refreshJson.RootElement.GetProperty("jobId").GetString();
        jobId.ShouldNotBeNullOrEmpty();
        
        // Step 2: Verify Location header uses /v3
        refreshResponse.Headers.Location.ShouldNotBeNull();
        refreshResponse.Headers.Location!.OriginalString.ShouldBe($"/v3/jobs/{jobId}");
        
        // Step 3: Poll using V3 endpoint
        var completedStatus = await PollJobStatus(jobId, "/v3/jobs/{0}");
        completedStatus.ShouldNotBeNullOrEmpty();
    }

    /// <summary>
    /// Test Scenario 5: Non-existent job ID returns 404
    /// 
    /// Verifies proper error handling for invalid job IDs
    /// </summary>
    [Test]
    public async Task GetJobStatus_WithNonExistentJobId_Returns404_V2()
    {
        var nonExistentJobId = "nonexistent-job-id-" + Guid.NewGuid();
        
        var response = await _httpClient.GetAsync($"/v2/jobs/{nonExistentJobId}");
        
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test Scenario 6: Non-existent job ID returns 404 (V3)
    /// </summary>
    [Test]
    public async Task GetJobStatus_WithNonExistentJobId_Returns404_V3()
    {
        var nonExistentJobId = "nonexistent-job-id-" + Guid.NewGuid();
        
        var response = await _httpClient.GetAsync($"/v3/jobs/{nonExistentJobId}");
        
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test Scenario 7: Job status response structure verification
    /// 
    /// Verifies that the GET /jobs/{jobId} response contains all expected fields
    /// with correct types and non-null values where appropriate
    /// </summary>
    [Test]
    public async Task GetJobStatus_ReturnsCorrectResponseStructure_V2()
    {
        // Step 1: Create a job
        var refreshResponse = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshJson = JsonDocument.Parse(refreshContent);
        var jobId = refreshJson.RootElement.GetProperty("jobId").GetString();
        
        // Step 2: Get job status
        var statusResponse = await _httpClient.GetAsync($"/v2/jobs/{jobId}");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        var statusJson = JsonDocument.Parse(statusContent);
        
        // Step 3: Verify all fields exist and have correct types
        statusJson.RootElement.GetProperty("jobId").GetString().ShouldBe(jobId);
        var responseStatus = statusJson.RootElement.GetProperty("status").GetString();
        responseStatus.ShouldNotBeNullOrEmpty();
        
        var createdAt = statusJson.RootElement.GetProperty("createdAt").GetDateTime();
        createdAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        
        // finishedAt may be null for pending jobs
        if (statusJson.RootElement.TryGetProperty("finishedAt", out var finishedAtElement) && 
            finishedAtElement.ValueKind != JsonValueKind.Null)
        {
            finishedAtElement.GetDateTime().ShouldBeGreaterThanOrEqualTo(createdAt);
        }
        
        // errorMessage may be null for successful jobs
        if (statusJson.RootElement.TryGetProperty("errorMessage", out var errorElement) &&
            errorElement.ValueKind == JsonValueKind.String)
        {
            // If present and not null, it should be a string
            _ = errorElement.GetString();
        }
    }

    /// <summary>
    /// Test Scenario 8: Initial job status is Pending
    /// 
    /// Verifies that a newly created job has Pending status
    /// and finishedAt is null
    /// </summary>
    [Test]
    public async Task NewJob_HasPendingStatus_V2()
    {
        // Step 1: Create a job
        var refreshResponse = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshJson = JsonDocument.Parse(refreshContent);
        var jobId = refreshJson.RootElement.GetProperty("jobId").GetString();
        
        // Step 2: Immediately check status (should be Pending)
        var statusResponse = await _httpClient.GetAsync($"/v2/jobs/{jobId}");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        var statusJson = JsonDocument.Parse(statusContent);
        
        // Step 3: Verify status is Pending
        var status = statusJson.RootElement.GetProperty("status").GetString();
        status.ShouldBe("Pending");
        
        // finishedAt should be null for pending jobs
        if (statusJson.RootElement.TryGetProperty("finishedAt", out var finishedAtElement))
        {
            finishedAtElement.ValueKind.ShouldBe(JsonValueKind.Null);
        }
    }

    /// <summary>
    /// Helper method to poll job status until completion with timeout
    /// </summary>
    /// <param name="jobId">The job ID to poll</param>
    /// <param name="endpointTemplate">Endpoint template like "/v2/jobs/{0}"</param>
    /// <returns>Final job status (Completed, Error, etc.)</returns>
    private async Task<string?> PollJobStatus(string jobId, string endpointTemplate)
    {
        for (int attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            await Task.Delay(PollIntervalMs);
            
            var response = await _httpClient.GetAsync(string.Format(endpointTemplate, jobId));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                continue;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var status = json.RootElement.GetProperty("status").GetString();
            
            // If job is completed or errored, we're done
            if (status == "Completed" || status == "Error")
            {
                // Verify finishedAt is set
                if (json.RootElement.TryGetProperty("finishedAt", out var finishedAt))
                {
                    finishedAt.ValueKind.ShouldNotBe(JsonValueKind.Null, 
                        "finishedAt should be set for completed jobs");
                }
                
                // If error, verify errorMessage
                if (status == "Error" && 
                    json.RootElement.TryGetProperty("errorMessage", out var errorMsg))
                {
                    errorMsg.ValueKind.ShouldNotBe(JsonValueKind.Null,
                        "errorMessage should be set for failed jobs");
                }
                
                return status;
            }
            
            // Continue polling
            TestContext.Out.WriteLine($"Polling job {jobId}: {status}");
        }
        
        // Timeout - job didn't complete in time
        throw new TimeoutException(
            $"Job {jobId} did not complete within {MaxPollAttempts * PollIntervalMs / 1000} seconds");
    }

    /// <summary>
    /// Test Scenario 9: Verify job headers are present
    /// </summary>
    [Test]
    public async Task RefreshEndpoint_IncludesContentTypeHeader()
    {
        var refreshResponse = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);
        
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        refreshResponse.Content.Headers.ContentType.ShouldNotBeNull();
        refreshResponse.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
    }

    /// <summary>
    /// Test Scenario 10: Verify POST response includes Location header with correct format
    /// </summary>
    [Test]
    public async Task RefreshEndpoint_LocationHeaderHasCorrectFormat()
    {
        var refreshResponse = await _httpClient.PostAsync("/v2/odsInstances/edOrgs/refresh", null);
        
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        refreshResponse.Headers.Location.ShouldNotBeNull();
        
        var location = refreshResponse.Headers.Location!.OriginalString;
        location.ShouldStartWith("/v2/jobs/");
        location.Length.ShouldBeGreaterThan("/v2/jobs/".Length);
    }
}
