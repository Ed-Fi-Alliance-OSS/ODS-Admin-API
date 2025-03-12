// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Shouldly;
using static EdFi.Ods.AdminApi.AdminConsole.Features.Instances.AddInstance;

namespace EdFi.Ods.AdminApi.AdminConsole.UnitTests.Infrastructure.DataAccess.Models;

[TestFixture]
public class InstanceTest
{
    [TestFixture]
    public class ShouldInitializeProperly
    {
        private Instance _instance;

        [SetUp]
        public void SetUp()
        {
            _instance = new Instance();
        }

        [Test]
        public void ShouldInitializeOdsInstanceContexts()
        {
            // Assert
            _instance.OdsInstanceContexts.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeOdsInstanceDerivatives()
        {
            // Assert
            _instance.OdsInstanceDerivatives.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeTenantName()
        {
            // Assert
            _instance.TenantName.ShouldBeEmpty();
        }

        [Test]
        public void ShouldInitializeInstanceName()
        {
            // Assert
            _instance.InstanceName.ShouldBeEmpty();
        }

        [Test]
        public void ShouldInitializeStatus()
        {
            // Assert
            _instance.Status.ShouldBe(InstanceStatus.Pending);
        }

        [Test]
        public void ShouldInitializeOdsInstanceId()
        {
            // Assert
            _instance.OdsInstanceId.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeInstanceType()
        {
            // Assert
            _instance.InstanceType.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeBaseUrl()
        {
            // Assert
            _instance.BaseUrl.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeResourceUrl()
        {
            // Assert
            _instance.ResourceUrl.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeOAuthUrl()
        {
            // Assert
            _instance.OAuthUrl.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeCredentials()
        {
            // Assert
            _instance.Credentials.ShouldBeNull();
        }

        [Test]
        public void ShouldInitializeCompletedAt()
        {
            // Assert
            _instance.CompletedAt.ShouldBeNull();
        }
    }

    public class From
    {
        private static AddInstanceRequest CreateDefaultRequestModel()
        {
            return new AddInstanceRequest
            {
                OdsInstanceId = 1,
                TenantId = 2,
                TenantName = "TenantName",
                Name = "InstanceName",
                InstanceType = "InstanceType",
                Credentials = [1, 2, 3],
                Status = "Completed",
                OdsInstanceContexts = [new() { ContextKey = "Key1", ContextValue = "Value1" }],
                OdsInstanceDerivatives = [new() { DerivativeType = DerivativeType.ReadReplica.ToString() }]
            };
        }

        [TestFixture]
        public class ShouldMapPropertiesCorrectly
        {
            private AddInstanceRequest _request;
            private Instance _result;

            [SetUp]
            public void SetUp()
            {
                _request = CreateDefaultRequestModel();
                _result = Instance.From(_request);
            }

            [Test]
            public void ShouldMapOdsInstanceIdCorrectly()
            {
                // Assert
                _result.OdsInstanceId.ShouldBe(_request.OdsInstanceId);
            }

            [Test]
            public void ShouldMapTenantIdCorrectly()
            {
                // Assert
                _result.TenantId.ShouldBe(_request.TenantId);
            }

            [Test]
            public void ShouldMapTenantNameCorrectly()
            {
                // Assert
                _result.TenantName.ShouldBe(_request.TenantName);
            }

            [Test]
            public void ShouldMapInstanceNameCorrectly()
            {
                // Assert
                _result.InstanceName.ShouldBe(_request.Name);
            }

            [Test]
            public void ShouldMapInstanceTypeCorrectly()
            {
                // Assert
                _result.InstanceType.ShouldBe(_request.InstanceType);
            }

            [Test]
            public void ShouldMapCredentialsCorrectly()
            {
                // Assert
                _result.Credentials.ShouldBe(_request.Credentials);
            }

            [Test]
            public void ShouldMapStatusCorrectly()
            {
                // Assert
                _result.Status.ShouldBe(InstanceStatus.Completed);
            }

            [Test]
            public void ShouldMapOdsInstanceContextsCorrectly()
            {
                // Assert
                _result.OdsInstanceContexts.ShouldNotBeNull();
                _result.OdsInstanceContexts.Count.ShouldBe(1);
                _result.OdsInstanceContexts.First().ContextKey.ShouldBe("Key1");
                _result.OdsInstanceContexts.First().ContextValue.ShouldBe("Value1");
            }

            [Test]
            public void ShouldMapOdsInstanceDerivativesCorrectly()
            {
                // Assert
                _result.OdsInstanceDerivatives.ShouldNotBeNull();
                _result.OdsInstanceDerivatives.Count.ShouldBe(1);
                _result.OdsInstanceDerivatives.First().DerivativeType.ShouldBe(DerivativeType.ReadReplica);
            }
        }

        [TestFixture]
        public class ShouldAcceptEmptyOdsInstanceArrays
        {
            private Instance _result;

            [SetUp]
            public void SetUp()
            {
                // Arrange
                var requestModel = CreateDefaultRequestModel();
                requestModel.OdsInstanceContexts = [];
                requestModel.OdsInstanceDerivatives = [];

                // Act
                _result = Instance.From(requestModel);
            }

            [Test]
            public void OdsInstanceContextsShouldBeEmpty()
            {
                // Assert
                _result.OdsInstanceContexts.ShouldNotBeNull();
                _result.OdsInstanceContexts.ShouldBeEmpty();
            }

            [Test]
            public void OdsInstanceDerivativesShouldBeEmpty()
            {
                // Assert
                _result.OdsInstanceDerivatives.ShouldNotBeNull();
                _result.OdsInstanceDerivatives.ShouldBeEmpty();
            }
        }

        [TestCase("Pending", InstanceStatus.Pending)]
        [TestCase("Completed", InstanceStatus.Completed)]
        [TestCase("InProgress", InstanceStatus.InProgress)]
        [TestCase("Pending_Delete", InstanceStatus.Pending_Delete)]
        [TestCase("Deleted", InstanceStatus.Deleted)]
        [TestCase("Delete_Failed", InstanceStatus.Delete_Failed)]
        [TestCase("Error", InstanceStatus.Error)]
        [TestCase("InvalidStatus", InstanceStatus.Pending)]
        public void From_ShouldHandleAllStatuses(string inputStatus, InstanceStatus expectedStatus)
        {
            // Arrange
            var requestModel = new AddInstanceRequest { Status = inputStatus };

            // Act
            var instance = Instance.From(requestModel);

            // Assert
            instance.Status.ShouldBe(expectedStatus);
        }
    }
}
