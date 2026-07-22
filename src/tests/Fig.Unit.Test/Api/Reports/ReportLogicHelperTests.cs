using Fig.Api.Reports;
using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering;
using Fig.Api.Reports.Rendering.Components;
using Fig.Common.Constants;
using Fig.Contracts.SettingGroups;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.Unit.Test.Api.Reports;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReportLogicHelperTests
{
    [Test]
    public void WebhookMatchesSend_RequiresTypePrefixAndOptionalUri()
    {
        Assert.That(WebhookDeliveryReport.MatchesSend(null, "HealthStatusChanged", ""), Is.False);
        Assert.That(WebhookDeliveryReport.MatchesSend("HealthStatusChanged ok", "HealthStatusChanged", ""), Is.True);
        Assert.That(WebhookDeliveryReport.MatchesSend("HealthStatusChanged http://hook", "HealthStatusChanged", "http://hook"), Is.True);
        Assert.That(WebhookDeliveryReport.MatchesSend("HealthStatusChanged other", "HealthStatusChanged", "http://hook"), Is.False);
        Assert.That(WebhookDeliveryReport.MatchesSend("OtherType", "HealthStatusChanged", ""), Is.False);
    }

    [Test]
    public void WebhookParseWebHookType_ParsesKnownPrefixes()
    {
        Assert.That(WebhookDeliveryReport.ParseWebHookType(null), Is.Null);
        Assert.That(WebhookDeliveryReport.ParseWebHookType("  "), Is.Null);
        Assert.That(WebhookDeliveryReport.ParseWebHookType("SettingValueChanged to x"), Is.EqualTo(nameof(WebHookType.SettingValueChanged)));
        Assert.That(WebhookDeliveryReport.ParseWebHookType("unknown"), Is.Null);
    }

    [Test]
    public void SettingGroupsAppendDivergences_IgnoresMatchingValues()
    {
        var clients = new Dictionary<string, IReadOnlyList<SettingClientBusinessEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = [ReportTestFixtures.CreateClient("A", null, ReportTestFixtures.CreateSetting("S", "same"))],
            ["B"] = [ReportTestFixtures.CreateClient("B", null, ReportTestFixtures.CreateSetting("S", "same"))]
        };
        var grouped = new GroupedSettingDataContract("G", null, "string",
        [
            new SourceSettingDataContract("A", "S"),
            new SourceSettingDataContract("B", "S")
        ]);
        var divergences = new List<SettingGroupDivergenceRow>();

        SettingGroupsCoverageReport.AppendDivergencesCore("Group1", grouped, clients, divergences);
        Assert.That(divergences, Is.Empty);
    }

    [Test]
    public void SettingGroupsAppendDivergences_FlagsMissingAndMasksSecrets()
    {
        var clients = new Dictionary<string, IReadOnlyList<SettingClientBusinessEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = [ReportTestFixtures.CreateClient("A", null, ReportTestFixtures.CreateSetting("Secret", "alpha", isSecret: true))],
            ["B"] = [ReportTestFixtures.CreateClient("B", null, ReportTestFixtures.CreateSetting("Secret", "beta", isSecret: true))]
        };
        var grouped = new GroupedSettingDataContract("G", null, "string",
        [
            new SourceSettingDataContract("A", "Secret"),
            new SourceSettingDataContract("B", "Secret"),
            new SourceSettingDataContract("Missing", "Secret"),
            new SourceSettingDataContract("A", "NotThere")
        ]);
        var divergences = new List<SettingGroupDivergenceRow>();

        SettingGroupsCoverageReport.AppendDivergencesCore("Group1", grouped, clients, divergences);

        Assert.That(divergences, Is.Not.Empty);
        Assert.That(divergences.Any(d => d.Value == ReportDataGridHtml.SecretMask), Is.True);
        Assert.That(divergences.Any(d => d.Value == "(missing client)"), Is.True);
        Assert.That(divergences.Any(d => d.Value == "(missing setting)"), Is.True);
        Assert.That(divergences.All(d => d.Value != "alpha" && d.Value != "beta"), Is.True);
    }

    [Test]
    public void InstanceMatrixBuildRows_DetectsDivergenceAndMasksSecrets()
    {
        var snapshots = new List<InstanceEnvironmentMatrixReport.InstanceSnapshot>
        {
            new("(default)",
            [
                ReportTestFixtures.CreateSetting("Pub", "one"),
                ReportTestFixtures.CreateSetting("Sec", "secret-a", isSecret: true)
            ]),
            new("prod",
            [
                ReportTestFixtures.CreateSetting("Pub", "two"),
                ReportTestFixtures.CreateSetting("Sec", "secret-a", isSecret: true)
            ])
        };

        var rows = InstanceEnvironmentMatrixReport.BuildRows(snapshots);
        var pub = rows.Single(r => r.SettingName == "Pub");
        var sec = rows.Single(r => r.SettingName == "Sec");

        Assert.That(pub.Diverges, Is.True);
        Assert.That(sec.Diverges, Is.False);
        Assert.That(sec.CellValues.All(v => v == ReportDataGridHtml.SecretMask), Is.True);
    }

    [Test]
    public void StaleConfig_BuildStaleSettingsAndClassifySilent()
    {
        var now = DateTime.UtcNow;
        var inventory = new[]
        {
            new SettingInventoryRow("A", null, "A", "Old", "General", "Technical", false, false, false, false, true, null, null, now.AddDays(-120)),
            new SettingInventoryRow("A", null, "A", "Fresh", "General", "Technical", false, false, false, false, true, null, null, now.AddDays(-1)),
            new SettingInventoryRow("B", null, "B", "Never", "General", "Technical", false, false, false, false, true, null, null, null)
        };

        var stale = StaleConfigReport.BuildStaleSettings(inventory, now.AddDays(-90));
        Assert.That(stale.Select(s => s.SettingName), Is.EquivalentTo(new[] { "Old", "Never" }));
        Assert.That(stale.First(s => s.SettingName == "Never").AgeDays, Is.EqualTo("Never"));

        var clients = new[]
        {
            ReportTestFixtures.CreateClient("Silent"),
            ReportTestFixtures.CreateClient("Active")
        };
        var statuses = new Dictionary<string, ClientStatusBusinessEntity>(StringComparer.OrdinalIgnoreCase)
        {
            ["Active"] = ReportTestFixtures.CreateStatus("Active", null, ReportTestFixtures.CreateSession())
        };

        var (noRead, orphaned) = StaleConfigReport.ClassifySilentClients(
            clients,
            statuses,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Active" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Active" });

        Assert.That(noRead.Select(r => r.ClientDisplay), Does.Contain("Silent"));
        Assert.That(noRead.Select(r => r.ClientDisplay), Does.Not.Contain("Active"));
        Assert.That(orphaned.Select(r => r.ClientDisplay), Does.Contain("Silent"));
        Assert.That(orphaned.Select(r => r.ClientDisplay), Does.Not.Contain("Active"));
    }

    [Test]
    public void AnomalyQuietPeriod_IdentifyQuietClients()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var quietClient = ReportTestFixtures.CreateClient("Quiet");
        quietClient.LastRegistration = from.AddDays(-10);
        var activeClient = ReportTestFixtures.CreateClient("StillActive");
        activeClient.LastRegistration = from.AddDays(-10);
        var clients = new[] { quietClient, activeClient };

        var statuses = new Dictionary<string, ClientStatusBusinessEntity>(StringComparer.OrdinalIgnoreCase)
        {
            ["StillActive"] = ReportTestFixtures.CreateStatus("StillActive", null, ReportTestFixtures.CreateSession())
        };
        var baseline = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Quiet"] = 3,
            ["StillActive"] = 2
        };
        var periodSessions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var quiet = AnomalyQuietPeriodReport.IdentifyQuietClients(clients, statuses, baseline, periodSessions, from);
        Assert.That(quiet.Select(q => q.ClientDisplay), Is.EquivalentTo(new[] { "Quiet" }));
    }

    [Test]
    public void BlastRadius_ExpandAndBuildAffectedRows()
    {
        var members = new List<SourceSettingDataContract>
        {
            new("Other", "SharedSetting"),
            new("App", "Target")
        };

        var withoutMembers = BlastRadiusReport.ExpandAffectedKeys("App", "Target", false, [members]);
        Assert.That(withoutMembers, Has.Count.EqualTo(1));

        var withMembers = BlastRadiusReport.ExpandAffectedKeys("App", "Target", true, [members]);
        Assert.That(withMembers, Has.Count.EqualTo(2));

        var clients = new[]
        {
            ReportTestFixtures.CreateClient("App", null, ReportTestFixtures.CreateSetting("Target", supportsLiveUpdate: false)),
            ReportTestFixtures.CreateClient("Other", null, ReportTestFixtures.CreateSetting("SharedSetting"))
        };
        var statuses = new Dictionary<string, ClientStatusBusinessEntity>(StringComparer.OrdinalIgnoreCase)
        {
            ["App"] = ReportTestFixtures.CreateStatus("App", null, ReportTestFixtures.CreateSession(restartRequired: true))
        };

        var rows = BlastRadiusReport.BuildAffectedRows(withMembers, clients, statuses);
        Assert.That(rows.Any(r => r.ClientDisplay == "App" && r.RestartRequiredSessions == 1), Is.True);
        Assert.That(rows.Any(r => r.ClientDisplay == "Missing" || r.SupportsLiveUpdate == "Unknown"), Is.False);

        var missing = BlastRadiusReport.BuildAffectedRows([("Ghost", "X")], [], statuses);
        Assert.That(missing.Single().SupportsLiveUpdate, Is.EqualTo("Unknown"));
        Assert.That(missing.Single().LiveSessions, Is.EqualTo(0));
    }

    [Test]
    public void SecretHygiene_FormatAgeAndRotationCards()
    {
        var now = new DateTime(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);
        Assert.That(SecretHygieneReport.FormatAge(null, now), Is.EqualTo("Never changed"));
        Assert.That(SecretHygieneReport.FormatAge(now.AddDays(-3), now), Is.EqualTo("3d"));
        Assert.That(SecretHygieneReport.FormatAge(now.AddHours(-5), now), Is.EqualTo("5h"));
        Assert.That(SecretHygieneReport.FormatAge(now.AddMinutes(-2), now), Is.EqualTo("2m"));
        Assert.That(SecretHygieneReport.FormatAge(now.AddSeconds(-10), now), Is.EqualTo("<1m"));

        var empty = SecretHygieneReport.BuildApiRotationCardsCore(null);
        Assert.That(empty.Single().Value, Is.EqualTo("No rotation recorded"));

        var cards = SecretHygieneReport.BuildApiRotationCardsCore(new ApiSecretRotationStateBusinessEntity
        {
            Status = "Completed",
            LastCompletedStage = "Finalize",
            ProcessedRecords = 12,
            UpdatedAtUtc = now,
            CompletedAtUtc = now,
            LastError = null
        });
        Assert.That(cards.First(c => c.Label == "Status").Value, Is.EqualTo("Completed"));
        Assert.That(cards.First(c => c.Label == "Last Error").Value, Is.EqualTo("None"));
    }

    [Test]
    public void ClientStatus_FormatValueHtmlMasksSecrets()
    {
        var secret = ReportTestFixtures.CreateSetting("Password", "cat", isSecret: true);
        var visible = ReportTestFixtures.CreateSetting("Public", "dog");

        Assert.That(ClientStatusReport.FormatValueHtmlCore(secret), Is.EqualTo(ReportDataGridHtml.SecretMask));
        Assert.That(ClientStatusReport.FormatValueHtmlCore(visible), Is.EqualTo("dog"));
    }

    [Test]
    public void SettingHistory_FormatValueHtmlMasksSecrets()
    {
        var html = SettingHistoryReport.FormatValueHtmlCore(
            new Fig.Datalayer.BusinessEntities.SettingValues.StringSettingBusinessEntity("secret"),
            isSecret: true,
            asDataGrid: false,
            definition: null);

        Assert.That(html, Does.Contain(System.Net.WebUtility.HtmlEncode(SecretConstants.SecretPlaceholder)));
        Assert.That(html, Does.Not.Contain("secret"));
    }
}
