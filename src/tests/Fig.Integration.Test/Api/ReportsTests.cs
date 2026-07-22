using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.Reports;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
[NonParallelizable]
public class ReportsTests : IntegrationTestBase
{
    private static readonly string[] ExpectedReportIds =
    [
        "user-activity",
        "setting-history",
        "client-status",
        "client-history",
        "client-uptime",
        "client-version",
        "security-audit",
        "configuration-inventory",
        "access-privilege",
        "secret-hygiene",
        "externally-managed-overrides",
        "fleet-health",
        "registration-drift",
        "restart-live-reload-debt",
        "instance-environment-matrix",
        "change-analytics",
        "deferred-scheduled-work",
        "time-machine-activity",
        "import-export-activity",
        "webhook-delivery",
        "custom-action-outcomes",
        "setting-groups-coverage",
        "lookup-usage",
        "blast-radius",
        "anomaly-quiet-period",
        "stale-config",
        "incident-correlation",
        "fig-platform"
    ];

    [Test]
    public async Task ShallReturnReportCatalogueForAdministrator()
    {
        var reports = await ApiClient.Get<List<ReportDefinitionDataContract>>("/reports");

        Assert.That(reports, Is.Not.Null);
        Assert.That(reports!.Count, Is.EqualTo(ExpectedReportIds.Length));
        Assert.That(reports.Select(r => r.Id), Is.EquivalentTo(ExpectedReportIds));
    }

    [Test]
    public async Task ShallRejectReportCatalogueForNonAdministrator()
    {
        var user = NewUser("reportUser", role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username!, user.Password!);
        var response = await ApiClient.GetRaw("/reports", $"Bearer {loginResult.Token}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallReturnNotFoundForUnknownReport()
    {
        await ApiClient.PostAndVerify(
            "/reports/does-not-exist",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>()),
            HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallReturnBadRequestForInvalidParameters()
    {
        await ApiClient.PostAndVerify(
            "/reports/user-activity",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = DateTime.UtcNow.AddDays(-1),
                ["To"] = DateTime.UtcNow
            }),
            HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShallGenerateClientStatusHtmlWithoutSecrets()
    {
        var settings = await RegisterSettings<SecretSettings>();

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-status",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));

        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Client Status Report"));
        Assert.That(html, Does.Contain(settings.ClientName));
        Assert.That(html, Does.Contain(nameof(SecretSettings.NoSecret)));
        Assert.That(html, Does.Contain(nameof(SecretSettings.SecretWithDefault)));
        Assert.That(html, Does.Contain("******"));
        Assert.That(html, Does.Not.Contain(">cat<"));
        Assert.That(html, Does.Contain("Secret Settings"));
        Assert.That(html, Does.Not.Contain("Secret Settings Omitted"));
    }

    [Test]
    public async Task ShallGenerateUserActivityHtmlIncludingSettingChanges()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        await SetSettings(settings.ClientName,
        [
            new SettingDataContract(nameof(ThreeSettings.AStringSetting), new StringSettingDataContract("activity-report-value"))
        ]);

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/user-activity",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["Username"] = UserName,
                ["From"] = from,
                ["To"] = to
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("User Activity Report"));
        Assert.That(html, Does.Contain(UserName));
        Assert.That(html, Does.Contain(nameof(ThreeSettings.AStringSetting)));
        Assert.That(html, Does.Contain("Setting Changes"));
        Assert.That(html, Does.Contain("Restarts"));
        Assert.That(html, Does.Contain("Other Actions"));
    }

    [Test]
    public async Task ShallGenerateSettingHistoryHtml()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/setting-history",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["SettingName"] = nameof(ThreeSettings.AStringSetting)
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Setting History Report"));
        Assert.That(html, Does.Contain(nameof(ThreeSettings.AStringSetting)));
        Assert.That(html, Does.Contain("Category"));
        Assert.That(html, Does.Contain("Current Value"));
    }

    [Test]
    public async Task ShallGenerateClientHistoryAndUptimeHtml()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        var historyResponse = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-history",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["From"] = from,
                ["To"] = to
            }));

        var uptimeResponse = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-uptime",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["From"] = from,
                ["To"] = to
            }));

        Assert.That(historyResponse!.IsSuccessStatusCode, Is.True);
        Assert.That(uptimeResponse!.IsSuccessStatusCode, Is.True);

        var historyHtml = await historyResponse.Content.ReadAsStringAsync();
        var uptimeHtml = await uptimeResponse.Content.ReadAsStringAsync();

        Assert.That(historyHtml, Does.Contain("Client History Report"));
        Assert.That(uptimeHtml, Does.Contain("Client Uptime Report"));
        Assert.That(uptimeHtml, Does.Contain("Run Session Log"));
        Assert.That(uptimeHtml, Does.Contain("at least one run session is active"));
        Assert.That(uptimeHtml, Does.Contain("Peak Concurrent Sessions"));
        Assert.That(uptimeHtml, Does.Contain("#38a169"));
        Assert.That(uptimeHtml, Does.Contain("#e53e3e"));
    }

    [Test]
    public async Task ShallGenerateNewPhaseReportsHtml()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);
        var range = new Dictionary<string, object?>
        {
            ["From"] = from,
            ["To"] = to
        };

        await AssertReportHtml("security-audit", range, "Security Audit");
        await AssertReportHtml("configuration-inventory", new Dictionary<string, object?>(), "Configuration Inventory");
        await AssertReportHtml("access-privilege", range, "Access");
        await AssertReportHtml("secret-hygiene", new Dictionary<string, object?>(), "Secret Hygiene");
        await AssertReportHtml("externally-managed-overrides", range, "Externally Managed");
        await AssertReportHtml("fleet-health", range, "Fleet Health");
        await AssertReportHtml("registration-drift", range, "Registration Drift");
        await AssertReportHtml("restart-live-reload-debt", range, "Restart");
        await AssertReportHtml("client-version", range, "Client Version");
        await AssertReportHtml("instance-environment-matrix",
            new Dictionary<string, object?> { ["ClientName"] = settings.ClientName },
            "Instance");
        await AssertReportHtml("change-analytics", range, "Change Analytics");
        await AssertReportHtml("deferred-scheduled-work", range, "Deferred");
        await AssertReportHtml("time-machine-activity", range, "Time Machine");
        await AssertReportHtml("import-export-activity", range, "Import");
        await AssertReportHtml("webhook-delivery", range, "Webhook");
        await AssertReportHtml("custom-action-outcomes", range, "Custom Action");
        await AssertReportHtml("setting-groups-coverage", new Dictionary<string, object?>(), "Setting Group");
        await AssertReportHtml("lookup-usage", new Dictionary<string, object?>(), "Lookup");
        await AssertReportHtml("blast-radius",
            new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["SettingName"] = nameof(ThreeSettings.AStringSetting)
            },
            "Blast Radius");
        await AssertReportHtml("anomaly-quiet-period", range, "Anomaly");
        await AssertReportHtml("stale-config", range, "Stale");
        await AssertReportHtml("incident-correlation",
            new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["From"] = from,
                ["To"] = to
            },
            "Incident");
        await AssertReportHtml("fig-platform", new Dictionary<string, object?>(), "Fig Platform");
    }

    [Test]
    public async Task ShallIncludeRegisteredClientsInStaleConfigReport()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/stale-config",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = from,
                ["To"] = to,
                ["StaleDays"] = 1
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Stale Config Report"));
        Assert.That(html, Does.Contain("Total Clients"));
        // Summary card value for Total Clients must not be zero when clients exist.
        Assert.That(html, Does.Not.Match(@"Total Clients</div>\s*<div class=""value"">0</div>"));
        Assert.That(html, Does.Contain(settings.ClientName).Or.Contain("Stale Settings"));
    }

    [Test]
    public async Task ShallRenderMultiSelectDataGridCellsWithoutPhantomRowsInClientHistory()
    {
        var settings = await RegisterSettings<ClientWithCollections>();
        await SetSettings(settings.ClientName,
        [
            new SettingDataContract(nameof(ClientWithCollections.AnimalDetails), new DataGridSettingDataContract(
            [
                new Dictionary<string, object?>
                {
                    ["Name"] = "Cow",
                    ["Category"] = "Farm",
                    ["HeightCm"] = 150,
                    ["FavouriteFoods"] = new List<string> { "Meat", "Cheese" }
                }
            ]))
        ]);

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-history",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["From"] = from,
                ["To"] = to
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Client History Report"));
        Assert.That(html, Does.Contain(nameof(ClientWithCollections.AnimalDetails)));
        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<td>Cow</td>"));
        Assert.That(html, Does.Contain("Meat").Or.Contain("Cheese"));
        Assert.That(html, Does.Not.Contain("<td>[</td>"));
        Assert.That(html, Does.Not.Contain("<td>]</td>"));
        Assert.That(html, Does.Not.Contain("$type"));
    }

    [Test]
    public async Task ShallNotMarkMatchingSecretsAsDivergentInInstanceMatrix()
    {
        var settings = await RegisterSettings<SecretSettings>();
        await SetSettings(settings.ClientName,
        [
            new SettingDataContract(nameof(SecretSettings.NoSecret), new StringSettingDataContract("shared-non-secret"))
        ], instance: "prod");

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/instance-environment-matrix",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Instance / Environment Matrix"));
        Assert.That(html, Does.Contain(nameof(SecretSettings.SecretWithDefault)));
        Assert.That(html, Does.Contain("******"));
        // Matching cloned secrets should not be flagged as divergent.
        Assert.That(html, Does.Not.Match(
            $@"{nameof(SecretSettings.SecretWithDefault)}[^<]*· diverges"));
    }

    [Test]
    public async Task ShallPersistFailedLoginInSecurityAuditReport()
    {
        var from = DateTime.UtcNow.AddMinutes(-1);
        await ApiClient.Login("definitely-not-a-user", "wrong-password", checkSuccess: false);

        var to = DateTime.UtcNow.AddMinutes(1);
        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/security-audit",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = from,
                ["To"] = to
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Security Audit"));
        Assert.That(html, Does.Contain("Failed Logins"));
        Assert.That(html, Does.Contain("definitely-not-a-user"));
        Assert.That(html, Does.Not.Contain("No failed logins in the selected range."));
    }

    [Test]
    public async Task ShallIncludeConnectedClientVersionsInClientVersionReport()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);
        var start = DateTime.UtcNow.AddMinutes(-5);
        var status = CreateStatusRequest(start, DateTime.UtcNow, 5000, true,
            appVersion: "2.1.0", figVersion: "3.5.0");
        await GetStatus(settings.ClientName, secret, status);

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);
        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-version",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = from,
                ["To"] = to
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Client Version Report"));
        Assert.That(html, Does.Contain(settings.ClientName));
        Assert.That(html, Does.Contain("2.1.0"));
        Assert.That(html, Does.Contain("3.5.0"));
        Assert.That(html, Does.Contain("Fig Client Version Breakdown"));
    }

    [Test]
    public async Task ShallRespectClientFilterInConfigurationInventoryReport()
    {
        var allowed = await RegisterSettings<ClientA>();
        var denied = await RegisterSettings<ClientBString>();

        var user = NewUser(
            username: "reportFilterAdmin",
            role: Role.Administrator,
            clientFilter: $"^{allowed.ClientName}$");
        await CreateUser(user);
        var loginResult = await Login(user.Username!, user.Password!);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/configuration-inventory",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>()),
            tokenOverride: $"Bearer {loginResult.Token}");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Configuration Inventory"));
        Assert.That(html, Does.Contain(allowed.ClientName));
        Assert.That(html, Does.Not.Contain(denied.ClientName));
    }

    [Test]
    public async Task ShallRejectSingleClientReportOutsideClientFilter()
    {
        var allowed = await RegisterSettings<ClientA>();
        var denied = await RegisterSettings<ClientBString>();

        var user = NewUser(
            username: "reportDenyAdmin",
            role: Role.Administrator,
            clientFilter: $"^{allowed.ClientName}$");
        await CreateUser(user);
        var loginResult = await Login(user.Username!, user.Password!);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-status",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = denied.ClientName
            }),
            tokenOverride: $"Bearer {loginResult.Token}",
            validateSuccess: false);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallRejectReportExecutionForNonAdministrator()
    {
        var user = NewUser("reportExecUser", role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username!, user.Password!);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/fig-platform",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>()),
            tokenOverride: $"Bearer {loginResult.Token}",
            validateSuccess: false);

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallRejectUnauthenticatedReportAccess()
    {
        await ApiClient.GetAndVerify("/reports", HttpStatusCode.Unauthorized, authenticate: false);
        await ApiClient.PostAndVerify(
            "/reports/fig-platform",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>()),
            HttpStatusCode.Unauthorized,
            authenticate: false);
    }

    [Test]
    public async Task ShallReturnBadRequestWhenDateRangeInvalid()
    {
        await ApiClient.PostAndVerify(
            "/reports/security-audit",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = DateTime.UtcNow,
                ["To"] = DateTime.UtcNow.AddDays(-1)
            }),
            HttpStatusCode.BadRequest);

        await ApiClient.PostAndVerify(
            "/reports/security-audit",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = DateTime.UtcNow.AddDays(-400),
                ["To"] = DateTime.UtcNow
            }),
            HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShallAllowSingleClientReportInsideClientFilter()
    {
        var allowed = await RegisterSettings<ClientA>();
        await RegisterSettings<ClientBString>();

        var user = NewUser(
            username: "reportAllowAdmin",
            role: Role.Administrator,
            clientFilter: $"^{allowed.ClientName}$");
        await CreateUser(user);
        var loginResult = await Login(user.Username!, user.Password!);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-status",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = allowed.ClientName
            }),
            tokenOverride: $"Bearer {loginResult.Token}");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Client Status Report"));
        Assert.That(html, Does.Contain(allowed.ClientName));
    }

    [Test]
    public async Task ShallFilterClientTaggedEventsByClientFilter()
    {
        var allowed = await RegisterSettings<ClientA>();
        var denied = await RegisterSettings<ClientBString>();

        await SetSettings(allowed.ClientName,
        [
            new SettingDataContract(nameof(ClientA.AnotherAddress), new StringSettingDataContract("allowed-value"))
        ]);
        await SetSettings(denied.ClientName,
        [
            new SettingDataContract(nameof(ClientBString.Animals), new StringSettingDataContract("denied-value"))
        ]);

        var user = NewUser(
            username: "reportEventFilterAdmin",
            role: Role.Administrator,
            clientFilter: $"^{allowed.ClientName}$");
        await CreateUser(user);
        var loginResult = await Login(user.Username!, user.Password!);

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);
        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/change-analytics",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["From"] = from,
                ["To"] = to
            }),
            tokenOverride: $"Bearer {loginResult.Token}");

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Change Analytics"));
        Assert.That(html, Does.Contain(allowed.ClientName));
        Assert.That(html, Does.Not.Contain(denied.ClientName));
    }

    [Test]
    public async Task ShallReturnCatalogueParameterMetadata()
    {
        var reports = await ApiClient.Get<List<ReportDefinitionDataContract>>("/reports");
        Assert.That(reports, Is.Not.Null);

        var userActivity = reports!.Single(r => r.Id == "user-activity");
        var username = userActivity.Parameters.Single(p => p.Name == "Username");
        Assert.That(username.Required, Is.True);
        Assert.That(username.LookupKind, Is.EqualTo(ReportParameterLookupKind.Users));

        var blastRadius = reports!.Single(r => r.Id == "blast-radius");
        Assert.That(blastRadius.Parameters.Single(p => p.Name == "ClientName").LookupKind,
            Is.EqualTo(ReportParameterLookupKind.Clients));
        Assert.That(blastRadius.Parameters.Single(p => p.Name == "SettingName").LookupKind,
            Is.EqualTo(ReportParameterLookupKind.ClientSettings));
    }

    private async Task AssertReportHtml(string reportId, Dictionary<string, object?> parameters, string titleFragment)
    {
        var response = await ApiClient.Post<HttpResponseMessage>(
            $"/reports/{reportId}",
            new ReportExecutionRequestDataContract(parameters));

        Assert.That(response, Is.Not.Null, reportId);
        Assert.That(response!.IsSuccessStatusCode, Is.True, reportId);
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain(titleFragment), reportId);
    }

    [Test]
    public async Task ShallRenderDataGridChangesAsNestedTablesInClientHistory()
    {
        var settings = await RegisterSettings<ClientWithCollections>();
        await SetSettings(settings.ClientName,
        [
            new SettingDataContract(nameof(ClientWithCollections.CityDetails), new DataGridSettingDataContract(
            [
                new Dictionary<string, object?>
                {
                    ["Name"] = "Paris",
                    ["Country"] = "France",
                    ["Size"] = "Medium"
                }
            ]))
        ]);

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddMinutes(1);

        var response = await ApiClient.Post<HttpResponseMessage>(
            "/reports/client-history",
            new ReportExecutionRequestDataContract(new Dictionary<string, object?>
            {
                ["ClientName"] = settings.ClientName,
                ["From"] = from,
                ["To"] = to
            }));

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True);
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("Client History Report"));
        Assert.That(html, Does.Contain(nameof(ClientWithCollections.CityDetails)));
        Assert.That(html, Does.Contain("report-table-nested"));
        Assert.That(html, Does.Contain("<td>Paris</td>"));
        Assert.That(html, Does.Contain("<td>France</td>"));
        Assert.That(html, Does.Not.Contain("$type"));
    }
}
