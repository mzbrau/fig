using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering.Components;
using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api.Reports;

[TestFixture]
public class SecurityReportExecuteTests
{
    private static string SummaryValue(IReadOnlyList<SummaryCardItem> summary, string label)
        => summary.Single(s => s.Label == label).Value;

    [Test]
    public async Task SecurityAuditReport_ExecuteAsync_PartitionsSecurityEvents()
    {
        var (from, to) = ReportTestFixtures.DefaultRange(7);
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.LoginFailed, from.AddDays(1), user: "bad"),
            ReportTestFixtures.CreateEvent(EventMessage.InvalidClientSecretAttempt, from.AddDays(2), "App"),
            ReportTestFixtures.CreateEvent(EventMessage.UserCreated, from.AddDays(3), user: "admin"),
            ReportTestFixtures.CreateEvent(EventMessage.ConfigurationChanged, from.AddDays(4)),
            ReportTestFixtures.CreateEvent(EventMessage.ClientSecretChanged, from.AddDays(5), "App")
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), null, null))
            .ReturnsAsync(events);

        var report = new SecurityAuditReport(eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (SecurityAuditReportModel)await report.ExecuteAsync(new SecurityAuditParameters { From = from, To = to });

        Assert.That(SummaryValue(model.Summary, "Total Security Events"), Is.EqualTo("5"));
        Assert.That(SummaryValue(model.Summary, "Failed Logins"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Invalid Client Secrets"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "User Lifecycle"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Configuration Changes"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Client Secret Changes"), Is.EqualTo("1"));
        Assert.That(model.FailedLogins, Has.Count.EqualTo(1));
        Assert.That(model.InvalidSecrets, Has.Count.EqualTo(1));
        Assert.That(model.Breakdown, Is.Not.Empty);
    }

    [Test]
    public async Task SecretHygieneReport_ExecuteAsync_ListsSecretsAndRotationState()
    {
        var client = ReportTestFixtures.CreateClient(
            "App",
            null,
            ReportTestFixtures.CreateSetting("ApiKey", isSecret: true, lastChanged: DateTime.UtcNow.AddDays(-40)),
            ReportTestFixtures.CreateSetting("Public", isSecret: false));
        client.PreviousClientSecretExpiryUtc = DateTime.UtcNow.AddDays(5);

        var rotation = new ApiSecretRotationStateBusinessEntity
        {
            Status = "Completed",
            LastCompletedStage = "Migrate",
            ProcessedRecords = 12,
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), false, true))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client });

        var rotationRepo = new Mock<IApiSecretRotationStateRepository>();
        rotationRepo.Setup(r => r.GetLatest()).ReturnsAsync(rotation);

        var report = new SecretHygieneReport(clients.Object, rotationRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (SecretHygieneReportModel)await report.ExecuteAsync(new SecretHygieneParameters());

        Assert.That(model.ScopeDisplay, Is.EqualTo("All clients"));
        Assert.That(SummaryValue(model.Summary, "Secret Settings"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "Previous Secret Windows"), Is.EqualTo("1"));
        Assert.That(SummaryValue(model.Summary, "API Rotation Status"), Is.EqualTo("Completed"));
        Assert.That(model.SecretSettings.Single().SettingName, Is.EqualTo("ApiKey"));
        Assert.That(model.PreviousSecretWindows, Has.Count.EqualTo(1));
        Assert.That(model.ApiRotation.Any(c => c.Label == "Status" && c.Value == "Completed"), Is.True);
    }

    [Test]
    public void SecretHygieneReport_ExecuteAsync_ThrowsWhenScopedClientMissing()
    {
        var clients = new Mock<ISettingClientRepository>();
        clients.Setup(r => r.GetClient("Missing", null)).ReturnsAsync((SettingClientBusinessEntity?)null);

        var rotationRepo = new Mock<IApiSecretRotationStateRepository>();
        var report = new SecretHygieneReport(clients.Object, rotationRepo.Object);
        ReportTestFixtures.Authenticate(report);

        Assert.ThrowsAsync<KeyNotFoundException>(() => report.ExecuteAsync(new SecretHygieneParameters
        {
            ClientName = "Missing"
        }));
    }
}
