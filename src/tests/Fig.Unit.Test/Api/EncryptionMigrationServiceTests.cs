using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class EncryptionMigrationServiceTests
{
    [Test]
    public void ValidateActiveApiHosts_Throws_WhenMultipleHostsReportConfigurationError()
    {
        var apiStatusRepository = new Mock<IApiStatusRepository>();
        apiStatusRepository
            .Setup(r => r.GetAllActive())
            .ReturnsAsync(new List<ApiStatusBusinessEntity>
            {
                new()
                {
                    Hostname = "api-1.example",
                    ConfigurationErrorDetected = true,
                    Version = "1.0",
                    RunningUser = "fig",
                    SecretHash = "hash1"
                },
                new()
                {
                    Hostname = "api-2.example",
                    ConfigurationErrorDetected = true,
                    Version = "1.0",
                    RunningUser = "fig",
                    SecretHash = "hash2"
                },
                new()
                {
                    Hostname = "api-ok.example",
                    ConfigurationErrorDetected = false,
                    Version = "1.0",
                    RunningUser = "fig",
                    SecretHash = "hash3"
                }
            });

        var sut = CreateService(apiStatusRepository.Object);

        Assert.That(
            async () => await sut.ValidateActiveApiHosts(),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("api-1.example")
                .And.Message.Contains("api-2.example")
                .And.Message.Contains("Cannot run API secret migration"));
    }

    [Test]
    public async Task ValidateActiveApiHosts_Succeeds_WhenNoHostsHaveConfigurationError()
    {
        var apiStatusRepository = new Mock<IApiStatusRepository>();
        apiStatusRepository
            .Setup(r => r.GetAllActive())
            .ReturnsAsync(new List<ApiStatusBusinessEntity>
            {
                new()
                {
                    Hostname = "api-ok.example",
                    ConfigurationErrorDetected = false,
                    Version = "1.0",
                    RunningUser = "fig",
                    SecretHash = "hash"
                }
            });

        var sut = CreateService(apiStatusRepository.Object);

        Assert.DoesNotThrowAsync(async () => await sut.ValidateActiveApiHosts());
    }

    private static EncryptionMigrationService CreateService(IApiStatusRepository apiStatusRepository)
    {
        var settings = new Mock<IOptionsMonitor<ApiSettings>>();
        settings.SetupGet(s => s.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "Data Source=:memory:",
            Secret = "secret"
        });

        return new EncryptionMigrationService(
            Mock.Of<IEventLogRepository>(),
            Mock.Of<ISettingClientRepository>(),
            Mock.Of<ISettingHistoryRepository>(),
            Mock.Of<IWebHookClientRepository>(),
            Mock.Of<ICheckPointDataRepository>(),
            Mock.Of<IDeferredChangeRepository>(),
            Mock.Of<IConfigurationRepository>(),
            Mock.Of<IEncryptionService>(),
            Mock.Of<IApiSecretRotationStateService>(),
            apiStatusRepository,
            settings.Object,
            NullLogger<EncryptionMigrationService>.Instance);
    }
}
