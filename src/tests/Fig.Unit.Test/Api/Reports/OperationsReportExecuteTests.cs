using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Secrets;
using Fig.Client.Abstractions.Data;
using Fig.Common.Constants;
using Fig.Contracts.Authentication;
using Fig.Contracts.Reports;
using Fig.Contracts.SettingGroups;
using Fig.Contracts.Settings;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Api.Reports;

[TestFixture]
public class OperationsReportExecuteTests
{
    private static readonly JsonSerializerSettings GroupSettingsJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None
    };

    [Test]
    public async Task ConfigurationInventoryReport_ExecuteAsync_ProjectsAllClients()
    {
        var secret = ReportTestFixtures.CreateSetting("ApiKey", isSecret: true);
        var plain = ReportTestFixtures.CreateSetting("Endpoint", "https://example");
        plain.Classification = Classification.Functional;
        var clients = new List<SettingClientBusinessEntity>
        {
            ReportTestFixtures.CreateClient("Alpha", null, secret, plain)
        };

        var repo = new Mock<ISettingClientRepository>();
        repo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(clients);

        var report = new ConfigurationInventoryReport(repo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ConfigurationInventoryReportModel)await report.ExecuteAsync(new ConfigurationInventoryParameters());

        Assert.That(model.ScopeDisplay, Is.EqualTo("All clients"));
        Assert.That(model.Rows, Has.Count.EqualTo(2));
        Assert.That(model.Summary.Single(s => s.Label == "Total Settings").Value, Is.EqualTo("2"));
        Assert.That(model.Summary.Single(s => s.Label == "Secrets").Value, Is.EqualTo("1"));
        Assert.That(model.Summary.Single(s => s.Label == "Classified").Value, Is.EqualTo("1"));
    }

    [Test]
    public async Task ConfigurationInventoryReport_ExecuteAsync_SecretsOnlyFilter()
    {
        var clients = new List<SettingClientBusinessEntity>
        {
            ReportTestFixtures.CreateClient(
                "Alpha",
                null,
                ReportTestFixtures.CreateSetting("ApiKey", isSecret: true),
                ReportTestFixtures.CreateSetting("Endpoint", "https://example"))
        };

        var repo = new Mock<ISettingClientRepository>();
        repo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(clients);

        var report = new ConfigurationInventoryReport(repo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ConfigurationInventoryReportModel)await report.ExecuteAsync(new ConfigurationInventoryParameters
        {
            SecretsOnly = true
        });

        Assert.That(model.Rows, Has.Count.EqualTo(1));
        Assert.That(model.Rows[0].SettingName, Is.EqualTo("ApiKey"));
        Assert.That(model.Summary.Single(s => s.Label == "Total Settings").Value, Is.EqualTo("1"));
    }

    [Test]
    public async Task InstanceEnvironmentMatrixReport_ExecuteAsync_SingleInstanceNotice()
    {
        const string clientName = "MatrixClient";
        var instance = ReportTestFixtures.CreateClient(
            clientName,
            null,
            ReportTestFixtures.CreateSetting("SettingA", "value"));

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetAllInstancesOfClient(clientName, false))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { instance });

        var secrets = new Mock<ISecretStoreHandler>();
        secrets.Setup(s => s.HydrateSecrets(It.IsAny<SettingClientBusinessEntity>()))
            .Returns(Task.CompletedTask);

        var report = new InstanceEnvironmentMatrixReport(clientRepo.Object, secrets.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (InstanceEnvironmentMatrixReportModel)await report.ExecuteAsync(new InstanceEnvironmentMatrixParameters
        {
            ClientName = clientName
        });

        Assert.That(model.Notice, Does.Contain("Only one instance"));
        Assert.That(model.InstanceColumns, Is.EqualTo(new[] { "(default)" }));
        Assert.That(model.Rows, Has.Count.EqualTo(1));
        Assert.That(model.Rows[0].Diverges, Is.False);
        secrets.Verify(s => s.HydrateSecrets(instance), Times.Once);
    }

    [Test]
    public async Task InstanceEnvironmentMatrixReport_ExecuteAsync_HighlightsDivergenceAcrossInstances()
    {
        const string clientName = "MatrixClient";
        var instances = new List<SettingClientBusinessEntity>
        {
            ReportTestFixtures.CreateClient(clientName, null, ReportTestFixtures.CreateSetting("Shared", "one")),
            ReportTestFixtures.CreateClient(clientName, "prod", ReportTestFixtures.CreateSetting("Shared", "two"))
        };

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetAllInstancesOfClient(clientName, false))
            .ReturnsAsync(instances);

        var secrets = new Mock<ISecretStoreHandler>();
        secrets.Setup(s => s.HydrateSecrets(It.IsAny<SettingClientBusinessEntity>()))
            .Returns(Task.CompletedTask);

        var report = new InstanceEnvironmentMatrixReport(clientRepo.Object, secrets.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (InstanceEnvironmentMatrixReportModel)await report.ExecuteAsync(new InstanceEnvironmentMatrixParameters
        {
            ClientName = clientName
        });

        Assert.That(model.Notice, Is.Null);
        Assert.That(model.Summary.Single(s => s.Label == "Instances").Value, Is.EqualTo("2"));
        Assert.That(model.Rows.Single(r => r.SettingName == "Shared").Diverges, Is.True);
    }

    [Test]
    public async Task SettingGroupsCoverageReport_ExecuteAsync_BuildsMembershipAndDivergences()
    {
        var groupJson = JsonConvert.SerializeObject(new List<GroupedSettingDataContract>
        {
            new("Grouped", null, "string",
            [
                new SourceSettingDataContract("ClientA", "Setting"),
                new SourceSettingDataContract("ClientB", "Setting")
            ])
        }, GroupSettingsJsonSettings);

        var groups = new List<SettingGroupBusinessEntity>
        {
            new() { Name = "OpsGroup", GroupSettingsJson = groupJson }
        };

        var clients = new List<SettingClientBusinessEntity>
        {
            ReportTestFixtures.CreateClient("ClientA", null, ReportTestFixtures.CreateSetting("Setting", "alpha")),
            ReportTestFixtures.CreateClient("ClientB", null, ReportTestFixtures.CreateSetting("Setting", "beta"))
        };

        var groupRepo = new Mock<ISettingGroupRepository>();
        groupRepo.Setup(r => r.GetAllGroups()).ReturnsAsync(groups);

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(clients);

        var secrets = new Mock<ISecretStoreHandler>();
        secrets.Setup(s => s.HydrateSecrets(It.IsAny<SettingClientBusinessEntity>()))
            .Returns(Task.CompletedTask);

        var report = new SettingGroupsCoverageReport(groupRepo.Object, clientRepo.Object, secrets.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (SettingGroupsCoverageReportModel)await report.ExecuteAsync(new SettingGroupsCoverageParameters());

        Assert.That(model.Membership, Has.Count.EqualTo(2));
        Assert.That(model.Divergences, Is.Not.Empty);
        Assert.That(model.Summary.Single(s => s.Label == "Groups").Value, Is.EqualTo("1"));
    }

    [Test]
    public async Task SettingGroupsCoverageReport_ExecuteAsync_FiltersByGroupName()
    {
        var includedJson = JsonConvert.SerializeObject(new List<GroupedSettingDataContract>
        {
            new("Included", null, "string", [new SourceSettingDataContract("ClientA", "S")])
        }, GroupSettingsJsonSettings);
        var excludedJson = JsonConvert.SerializeObject(new List<GroupedSettingDataContract>
        {
            new("Excluded", null, "string", [new SourceSettingDataContract("ClientB", "S")])
        }, GroupSettingsJsonSettings);

        var groups = new List<SettingGroupBusinessEntity>
        {
            new() { Name = "Keep", GroupSettingsJson = includedJson },
            new() { Name = "Skip", GroupSettingsJson = excludedJson }
        };

        var groupRepo = new Mock<ISettingGroupRepository>();
        groupRepo.Setup(r => r.GetAllGroups()).ReturnsAsync(groups);

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingClientBusinessEntity>
            {
                ReportTestFixtures.CreateClient("ClientA", null, ReportTestFixtures.CreateSetting("S", "v"))
            });

        var secrets = new Mock<ISecretStoreHandler>();
        secrets.Setup(s => s.HydrateSecrets(It.IsAny<SettingClientBusinessEntity>()))
            .Returns(Task.CompletedTask);

        var report = new SettingGroupsCoverageReport(groupRepo.Object, clientRepo.Object, secrets.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (SettingGroupsCoverageReportModel)await report.ExecuteAsync(new SettingGroupsCoverageParameters
        {
            GroupName = "Keep"
        });

        Assert.That(model.ScopeDisplay, Is.EqualTo("Keep"));
        Assert.That(model.Membership, Has.Count.EqualTo(1));
        Assert.That(model.Membership[0].GroupName, Is.EqualTo("Keep"));
    }

    [Test]
    public async Task LookupUsageReport_ExecuteAsync_ClassifiesUsedUnusedAndOrphans()
    {
        var usedLookup = new LookupTableBusinessEntity
        {
            Name = "Countries",
            IsClientDefined = false,
            LookupTable = new Dictionary<string, string?> { ["US"] = "United States" }
        };
        var unusedLookup = new LookupTableBusinessEntity
        {
            Name = "UnusedTable",
            IsClientDefined = true,
            LookupTable = new Dictionary<string, string?> { ["x"] = "y" }
        };

        var consumer = ReportTestFixtures.CreateSetting("Region", lookupTableKey: "Countries");
        var orphan = ReportTestFixtures.CreateSetting("BadRef", lookupTableKey: "MissingLookup");
        var keyRef = ReportTestFixtures.CreateSetting("KeyHolder", lookupTableKey: "Countries");
        keyRef.LookupKeySettingName = "LookupKeySetting";

        var lookupRepo = new Mock<ILookupTablesRepository>();
        lookupRepo.Setup(r => r.GetAllItems()).ReturnsAsync(new List<LookupTableBusinessEntity> { usedLookup, unusedLookup });

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingClientBusinessEntity>
            {
                ReportTestFixtures.CreateClient("App", null, consumer, orphan, keyRef)
            });

        var report = new LookupUsageReport(lookupRepo.Object, clientRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (LookupUsageReportModel)await report.ExecuteAsync(new LookupUsageParameters());

        Assert.That(model.UsedLookups.Single().LookupName, Is.EqualTo("Countries"));
        Assert.That(model.UnusedLookups.Single().LookupName, Is.EqualTo("UnusedTable"));
        Assert.That(model.KeySettingReferences, Has.Count.EqualTo(1));
        Assert.That(model.Summary.Single(s => s.Label == "Missing Lookup Refs").Value, Is.EqualTo("1"));
    }

    [Test]
    public async Task DeferredScheduledWorkReport_ExecuteAsync_AggregatesPendingOverdueAndEvents()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var now = DateTime.UtcNow;

        var pendingFuture = new DeferredChangeBusinessEntity
        {
            ExecuteAtUtc = now.AddDays(1),
            RequestingUser = "planner",
            ClientName = "DeferredClient",
            Instance = "prod",
            ChangeSet = new SettingValueUpdatesDataContract([], "future", null)
        };
        var pendingOverdue = new DeferredChangeBusinessEntity
        {
            ExecuteAtUtc = now.AddDays(-2),
            RequestingUser = "planner",
            ClientName = "DeferredClient",
            ChangeSet = new SettingValueUpdatesDataContract(
                [new SettingDataContract("A", null), new SettingDataContract("B", null)],
                "overdue",
                null)
        };

        var deferredRepo = new Mock<IDeferredChangeRepository>();
        deferredRepo.Setup(r => r.GetAllChanges())
            .ReturnsAsync(new[] { pendingFuture, pendingOverdue });

        var scheduleEvents = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.ChangesScheduled, now.AddHours(-1)),
            ReportTestFixtures.CreateEvent(EventMessage.ScheduledChangesDeleted, now.AddHours(-2))
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.Is<IReadOnlyCollection<string>>(t => t.Contains(EventMessage.ChangesScheduled)),
                It.IsAny<UserDataContract>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(scheduleEvents);

        var imports = new List<DeferredClientImportBusinessEntity>
        {
            new()
            {
                Name = "ImportClient",
                Instance = null,
                ImportTime = now.AddHours(-3),
                SettingCount = 4,
                AuthenticatedUser = "importer"
            }
        };

        var importRepo = new Mock<IDeferredClientImportRepository>();
        importRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>())).ReturnsAsync(imports);

        var report = new DeferredScheduledWorkReport(deferredRepo.Object, importRepo.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (DeferredScheduledWorkReportModel)await report.ExecuteAsync(new DeferredScheduledWorkParameters
        {
            From = from,
            To = to
        });

        Assert.That(model.PendingChanges, Has.Count.EqualTo(2));
        Assert.That(model.OverdueChanges, Has.Count.EqualTo(1));
        Assert.That(model.OverdueChanges[0].SettingCount, Is.EqualTo(2));
        Assert.That(model.ScheduleEvents, Has.Count.EqualTo(2));
        Assert.That(model.DeferredImports, Has.Count.EqualTo(1));
        Assert.That(model.Summary.Single(s => s.Label == "Scheduled In Range").Value, Is.EqualTo("1"));
    }

    [Test]
    public async Task TimeMachineActivityReport_ExecuteAsync_SplitsEventsAndCheckpoints()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var ts = DateTime.UtcNow.AddHours(-1);

        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.CheckPointCreated, ts, message: "created"),
            ReportTestFixtures.CreateEvent(EventMessage.CheckPointApplied, ts.AddMinutes(-5), message: "applied"),
            ReportTestFixtures.CreateEvent(EventMessage.NoteAddedToCheckPoint, ts.AddMinutes(-10), message: "note")
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.Is<IReadOnlyCollection<string>>(t => t.Contains(EventMessage.CheckPointCreated)),
                It.IsAny<UserDataContract>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(events);

        var checkpoints = new List<CheckPointBusinessEntity>
        {
            new()
            {
                Timestamp = ts,
                User = "admin",
                AfterEvent = EventMessage.CheckPointCreated,
                NumberOfClients = 2,
                NumberOfSettings = 5,
                Note = "snapshot"
            }
        };

        var checkpointRepo = new Mock<ICheckPointRepository>();
        checkpointRepo.Setup(r => r.GetCheckPoints(from, to)).ReturnsAsync(checkpoints);

        var report = new TimeMachineActivityReport(eventLog.Object, checkpointRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (TimeMachineActivityReportModel)await report.ExecuteAsync(new TimeMachineActivityParameters
        {
            From = from,
            To = to
        });

        Assert.That(model.CheckPoints, Has.Count.EqualTo(1));
        Assert.That(model.CreatedEvents, Has.Count.EqualTo(1));
        Assert.That(model.AppliedEvents, Has.Count.EqualTo(1));
        Assert.That(model.NoteEvents, Has.Count.EqualTo(1));
        Assert.That(model.Summary.Single(s => s.Label == "Total Events").Value, Is.EqualTo("3"));
    }

    [Test]
    public async Task ImportExportActivityReport_ExecuteAsync_BuildsSuccessFailBreakdown()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var events = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(EventMessage.DataExported, DateTime.UtcNow.AddHours(-1)),
            ReportTestFixtures.CreateEvent(EventMessage.DataImported, DateTime.UtcNow.AddHours(-2)),
            ReportTestFixtures.CreateEvent(EventMessage.DataImportFailed, DateTime.UtcNow.AddHours(-3))
        };

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<UserDataContract>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(events);

        var report = new ImportExportActivityReport(eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (ImportExportActivityReportModel)await report.ExecuteAsync(new ImportExportActivityParameters
        {
            From = from,
            To = to
        });

        Assert.That(model.Rows, Has.Count.EqualTo(3));
        Assert.That(model.SuccessFailBreakdown.Single(s => s.Label == "Success").Value, Is.EqualTo(2));
        Assert.That(model.SuccessFailBreakdown.Single(s => s.Label == "Failed").Value, Is.EqualTo(1));
    }

    [Test]
    public async Task CustomActionOutcomesReport_ExecuteAsync_SummarizesFailuresAndSlowest()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var requested = DateTime.UtcNow.AddHours(-2);

        var executions = new List<CustomActionExecutionBusinessEntity>
        {
            new()
            {
                ClientName = "ActionClient",
                CustomActionName = "Restart",
                RequestedAt = requested,
                ExecutedAt = requested.AddSeconds(30),
                Succeeded = true,
                HandlingInstance = "host-a"
            },
            new()
            {
                ClientName = "ActionClient",
                CustomActionName = "Deploy",
                RequestedAt = requested.AddMinutes(-5),
                ExecutedAt = null,
                Succeeded = false,
                HandlingInstance = "host-b"
            }
        };

        var repo = new Mock<ICustomActionExecutionRepository>();
        repo.Setup(r => r.GetHistory(from, to, null)).ReturnsAsync(executions);

        var report = new CustomActionOutcomesReport(repo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (CustomActionOutcomesReportModel)await report.ExecuteAsync(new CustomActionOutcomesParameters
        {
            From = from,
            To = to
        });

        Assert.That(model.ScopeDisplay, Is.EqualTo("All clients"));
        Assert.That(model.Summary.Single(s => s.Label == "Succeeded").Value, Is.EqualTo("1"));
        Assert.That(model.Summary.Single(s => s.Label == "Failed").Value, Is.EqualTo("1"));
        Assert.That(model.Failures, Has.Count.EqualTo(1));
        Assert.That(model.Failures[0].ActionName, Is.EqualTo("Deploy"));
        Assert.That(model.Slowest, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CustomActionOutcomesReport_ExecuteAsync_NoExecutionsReportsNaSuccessRate()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();

        var repo = new Mock<ICustomActionExecutionRepository>();
        repo.Setup(r => r.GetHistory(from, to, "FilteredClient")).ReturnsAsync(Array.Empty<CustomActionExecutionBusinessEntity>());

        var report = new CustomActionOutcomesReport(repo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (CustomActionOutcomesReportModel)await report.ExecuteAsync(new CustomActionOutcomesParameters
        {
            From = from,
            To = to,
            ClientName = "FilteredClient"
        });

        Assert.That(model.Summary.Single(s => s.Label == "Success Rate").Value, Is.EqualTo("n/a"));
        Assert.That(model.Failures, Is.Empty);
        Assert.That(model.Slowest, Is.Empty);
    }

    [Test]
    public async Task FigPlatformReport_ExecuteAsync_AggregatesInventoryAndConfigFlags()
    {
        var apiNodes = new List<ApiStatusBusinessEntity>
        {
            new()
            {
                Hostname = "api-1",
                IpAddress = "10.0.0.1",
                Version = "3.0.0",
                StartTimeUtc = DateTime.UtcNow.AddDays(-1),
                LastSeen = DateTime.UtcNow,
                MemoryUsageBytes = 256 * 1024 * 1024,
                TotalRequests = 1000,
                RequestsPerMinute = 12.5,
                RunningUser = "fig",
                ConfigurationErrorDetected = false
            }
        };

        var configuration = new FigConfigurationBusinessEntity
        {
            AllowNewRegistrations = true,
            EnableTimeMachine = true,
            TimelineDurationDays = 30
        };

        var apiStatusRepo = new Mock<IApiStatusRepository>();
        apiStatusRepo.Setup(r => r.GetAllActive()).ReturnsAsync(apiNodes);

        var configRepo = new Mock<IConfigurationRepository>();
        configRepo.Setup(r => r.GetConfiguration()).ReturnsAsync(configuration);

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventLogCount()).ReturnsAsync(42_000);

        var clientRepo = new Mock<ISettingClientRepository>();
        clientRepo.Setup(r => r.GetAllClients(It.IsAny<UserDataContract>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { ReportTestFixtures.CreateClient("C1") });

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAllUsers()).ReturnsAsync(new List<UserBusinessEntity> { new() { Username = "admin" } });

        var webhookRepo = new Mock<IWebHookRepository>();
        webhookRepo.Setup(r => r.GetWebHooks()).ReturnsAsync(new List<WebHookBusinessEntity>());

        var groupRepo = new Mock<ISettingGroupRepository>();
        groupRepo.Setup(r => r.GetAllGroups()).ReturnsAsync(new List<SettingGroupBusinessEntity>());

        var report = new FigPlatformReport(
            apiStatusRepo.Object,
            configRepo.Object,
            eventLog.Object,
            clientRepo.Object,
            userRepo.Object,
            webhookRepo.Object,
            groupRepo.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (FigPlatformReportModel)await report.ExecuteAsync(new FigPlatformParameters());

        Assert.That(model.Summary.Single(s => s.Label == "Active API Nodes").Value, Is.EqualTo("1"));
        Assert.That(model.Summary.Single(s => s.Label == "Clients").Value, Is.EqualTo("1"));
        Assert.That(model.Summary.Single(s => s.Label == "Event Log Rows").Value, Is.EqualTo(42_000.ToString("N0")));
        Assert.That(model.ConfigFlags.Any(f => f.Flag == "Enable Time Machine" && f.Value == "Enabled"), Is.True);
        Assert.That(model.ApiNodes.Single().Hostname, Is.EqualTo("api-1"));
    }

    [Test]
    public async Task WebhookDeliveryReport_ExecuteAsync_ZeroSendsAndMatchingSends()
    {
        var (from, to) = ReportTestFixtures.DefaultRange();
        var clientId = Guid.NewGuid();
        const string baseUri = "https://hooks.example/send";

        var hooks = new List<WebHookBusinessEntity>
        {
            new()
            {
                ClientId = clientId,
                WebHookType = WebHookType.SettingValueChanged,
                ClientNameRegex = ".*",
                MinSessions = 1
            },
            new()
            {
                ClientId = Guid.NewGuid(),
                WebHookType = WebHookType.HealthStatusChanged,
                ClientNameRegex = ".*",
                MinSessions = 0
            }
        };

        var webhookClients = new List<WebHookClientBusinessEntity>
        {
            new() { Id = clientId, Name = "HookClient", BaseUri = baseUri, Secret = "s", SecretEncrypted = "e" }
        };

        var sendEvents = new List<EventLogBusinessEntity>
        {
            ReportTestFixtures.CreateEvent(
                EventMessage.WebHookSent,
                DateTime.UtcNow.AddMinutes(-10),
                message: $"{WebHookType.SettingValueChanged} delivered to {baseUri}")
        };

        var webhookRepo = new Mock<IWebHookRepository>();
        webhookRepo.Setup(r => r.GetWebHooks()).ReturnsAsync(hooks);

        var webhookClientRepo = new Mock<IWebHookClientRepository>();
        webhookClientRepo.Setup(r => r.GetClients(false)).ReturnsAsync(webhookClients);

        var eventLog = new Mock<IEventLogRepository>();
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.Is<IReadOnlyCollection<string>>(t => t.Contains(EventMessage.WebHookSent)),
                It.IsAny<UserDataContract>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(sendEvents);
        eventLog.Setup(r => r.GetEventsByTypes(
                from,
                to,
                It.Is<IReadOnlyCollection<string>>(t => t.Contains(EventMessage.NewSession)),
                It.IsAny<UserDataContract>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new List<EventLogBusinessEntity>());

        var report = new WebhookDeliveryReport(webhookRepo.Object, webhookClientRepo.Object, eventLog.Object);
        ReportTestFixtures.Authenticate(report);

        var model = (WebhookDeliveryReportModel)await report.ExecuteAsync(new WebhookDeliveryParameters
        {
            From = from,
            To = to
        });

        var matched = model.Definitions.Single(d => d.BaseUri == baseUri);
        Assert.That(matched.SendCount, Is.EqualTo(1));
        Assert.That(model.ZeroSends, Has.Count.EqualTo(1));
        Assert.That(model.ZeroSends[0].WebHookType, Is.EqualTo(nameof(WebHookType.HealthStatusChanged)));
        Assert.That(model.Summary.Single(s => s.Label == "Webhook Sends").Value, Is.EqualTo("1"));
        Assert.That(model.SendsByType.Single().Label, Is.EqualTo(nameof(WebHookType.SettingValueChanged)));
    }
}
