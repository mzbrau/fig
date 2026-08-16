using Fig.Api;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ImportExportServiceUnexpectedFailureTests
{
    private Mock<IEventLogRepository> _eventLogRepository = null!;
    private Mock<IEventLogFactory> _eventLogFactory = null!;
    private ImportExportService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventLogRepository = new Mock<IEventLogRepository>();
        _eventLogFactory = new Mock<IEventLogFactory>();
        _eventLogFactory
            .Setup(f => f.DataImportStarted(It.IsAny<ImportType>(), It.IsAny<ImportMode>(), It.IsAny<UserDataContract?>()))
            .Returns(new EventLogBusinessEntity());
        _eventLogFactory
            .Setup(f => f.DataImportFailed(
                It.IsAny<ImportType>(),
                It.IsAny<ImportMode>(),
                It.IsAny<UserDataContract?>(),
                It.IsAny<string>()))
            .Returns(new EventLogBusinessEntity());

        _sut = new ImportExportService(
            Mock.Of<ISettingClientRepository>(),
            Mock.Of<IClientExportConverter>(),
            _eventLogRepository.Object,
            _eventLogFactory.Object,
            Mock.Of<ISettingHistoryRepository>(),
            Mock.Of<IDeferredClientConverter>(),
            Mock.Of<IDeferredClientImportRepository>(),
            Mock.Of<ISettingApplier>(),
            Mock.Of<ISettingChangeRecorder>(),
            Mock.Of<IEncryptionService>(),
            Mock.Of<IClientOverrideService>(),
            Mock.Of<ILogger<ImportExportService>>());

        _sut.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            ".*",
            Enum.GetValues<Classification>().ToList()));
    }

    [Test]
    public void Import_RethrowsUnexpectedException_AfterLoggingFailure()
    {
        var data = new FigDataExportDataContract(
            DateTime.UtcNow,
            ImportType.UpdateValues,
            1,
            [
                new SettingClientExportDataContract(
                    "ClientA",
                    "desc",
                    Guid.NewGuid().ToString(),
                    null,
                    [])
            ]);

        Assert.That(
            async () => await _sut.Import(data, ImportMode.Api),
            Throws.TypeOf<NotSupportedException>());

        _eventLogFactory.Verify(
            f => f.DataImportFailed(
                ImportType.UpdateValues,
                ImportMode.Api,
                It.IsAny<UserDataContract?>(),
                It.Is<string>(m => m.Contains("not supported", StringComparison.OrdinalIgnoreCase))),
            Times.Once);
        _eventLogRepository.Verify(r => r.Add(It.IsAny<EventLogBusinessEntity>()), Times.AtLeastOnce);
    }

    [Test]
    public void Import_RethrowsUnauthorizedAccessException()
    {
        _sut.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "limited",
            "Limited",
            "User",
            Role.Administrator,
            "^OtherClient$",
            Enum.GetValues<Classification>().ToList()));

        var data = new FigDataExportDataContract(
            DateTime.UtcNow,
            ImportType.AddNew,
            1,
            [
                new SettingClientExportDataContract(
                    "ClientA",
                    "desc",
                    Guid.NewGuid().ToString(),
                    null,
                    [])
            ]);

        Assert.That(
            async () => await _sut.Import(data, ImportMode.Api),
            Throws.TypeOf<UnauthorizedAccessException>());
    }
}
