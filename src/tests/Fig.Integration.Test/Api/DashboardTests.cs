using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.Dashboards;
using Fig.Test.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class DashboardTests : IntegrationTestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await DeleteAllDashboards();
    }

    private static DashboardDataContract CreateTestDashboard(
        string name,
        string? description = null,
        bool adminOnly = false)
    {
        return new DashboardDataContract
        {
            Name = name,
            Description = description,
            AdminOnly = adminOnly,
            Definition = new DashboardDefinitionDataContract
            {
                SchemaVersion = 1,
                Refresh = new DashboardRefreshDataContract
                {
                    SettingsSeconds = 600,
                    StatusSeconds = 60
                }
            }
        };
    }

    [Test]
    public async Task ShallCreateDashboard()
    {
        var dashboard = CreateTestDashboard("Production Overview", "Wallboard");

        var result = await CreateDashboard(dashboard);

        Assert.That(result.Id, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Production Overview"));
        Assert.That(result.Description, Is.EqualTo("Wallboard"));
        Assert.That(result.AdminOnly, Is.False);
        Assert.That(result.Definition.SchemaVersion, Is.EqualTo(1));
        Assert.That(result.Definition.Refresh.SettingsSeconds, Is.EqualTo(600));
        Assert.That(result.Definition.Refresh.StatusSeconds, Is.EqualTo(60));
        Assert.That(result.CreatedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10))));
        Assert.That(result.LastModifiedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10))));
    }

    [Test]
    public async Task ShallClampRefreshIntervalsOnCreate()
    {
        var dashboard = CreateTestDashboard("Clamped");
        dashboard.Definition.Refresh.SettingsSeconds = 10;
        dashboard.Definition.Refresh.StatusSeconds = 5;

        var result = await CreateDashboard(dashboard);

        Assert.That(result.Definition.Refresh.SettingsSeconds, Is.EqualTo(600));
        Assert.That(result.Definition.Refresh.StatusSeconds, Is.EqualTo(60));
    }

    [Test]
    public async Task ShallGetAllDashboards()
    {
        await CreateDashboard(CreateTestDashboard("One"));
        await CreateDashboard(CreateTestDashboard("Two"));

        var all = await GetAllDashboards();

        Assert.That(all.Count, Is.EqualTo(2));
        Assert.That(all.Select(d => d.Name), Is.EquivalentTo(new[] { "One", "Two" }));
    }

    [Test]
    public async Task ShallGetSingleDashboardById()
    {
        var created = await CreateDashboard(CreateTestDashboard("ById", "desc"));

        var fetched = await GetDashboard(created.Id!.Value);

        Assert.That(fetched, Is.Not.Null);
        Assert.That(fetched!.Name, Is.EqualTo("ById"));
        Assert.That(fetched.Description, Is.EqualTo("desc"));
    }

    [Test]
    public async Task ShallUpdateDashboard()
    {
        var created = await CreateDashboard(CreateTestDashboard("Original"));
        created.Name = "Renamed";
        created.Description = "Updated";
        created.Definition.Components.Add(new DashboardComponentInstanceDataContract
        {
            Id = "kpi-1",
            Type = "kpi"
        });

        var updated = await UpdateDashboard(created.Id!.Value, created);

        Assert.That(updated.Name, Is.EqualTo("Renamed"));
        Assert.That(updated.Description, Is.EqualTo("Updated"));
        Assert.That(updated.Definition.Components.Count, Is.EqualTo(1));
        Assert.That(updated.LastModifiedAt, Is.GreaterThanOrEqualTo(created.LastModifiedAt));
    }

    [Test]
    public async Task ShallReturnConflictOnStaleUpdate()
    {
        var created = await CreateDashboard(CreateTestDashboard("Concurrency"));
        var stale = await GetDashboard(created.Id!.Value);
        Assert.That(stale, Is.Not.Null);

        // First update succeeds and advances LastModifiedAt
        stale!.Name = "FirstWrite";
        var first = await UpdateDashboard(created.Id.Value, stale);

        // Second update with old timestamp conflicts
        stale.Name = "StaleWrite";
        stale.LastModifiedAt = created.LastModifiedAt;
        var response = await UpdateDashboardRaw(created.Id.Value, stale);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("FirstWrite"));

        // Force overwrite succeeds
        stale.LastModifiedAt = created.LastModifiedAt;
        stale.Name = "Forced";
        var forced = await UpdateDashboard(created.Id.Value, stale, forceOverwrite: true);
        Assert.That(forced.Name, Is.EqualTo("Forced"));
        Assert.That(forced.LastModifiedAt, Is.GreaterThanOrEqualTo(first.LastModifiedAt));
    }

    [Test]
    public async Task ShallDeleteDashboard()
    {
        var one = await CreateDashboard(CreateTestDashboard("Keep"));
        var two = await CreateDashboard(CreateTestDashboard("DeleteMe"));

        await DeleteDashboard(two.Id!.Value);

        var remaining = await GetAllDashboards();
        Assert.That(remaining.Count, Is.EqualTo(1));
        Assert.That(remaining[0].Id, Is.EqualTo(one.Id));
    }

    [Test]
    public async Task ShallReturnBadRequestForDuplicateName()
    {
        await CreateDashboard(CreateTestDashboard("UniqueName"));
        var response = await CreateDashboardRaw(CreateTestDashboard("UniqueName"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ShallAllowCaseOnlyRename()
    {
        var created = await CreateDashboard(CreateTestDashboard("Overview"));
        created.Name = "OVERVIEW";
        var updated = await UpdateDashboard(created.Id!.Value, created);
        Assert.That(updated.Name, Is.EqualTo("OVERVIEW"));
    }

    [Test]
    public async Task ShallReturnBadRequestForEmptyName()
    {
        var response = await CreateDashboardRaw(CreateTestDashboard("  "));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ShallReturnNotFoundForMissingDashboard()
    {
        await ApiClient.GetAndVerify($"/dashboards/{Guid.NewGuid()}", HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallHideAdminOnlyDashboardsFromNonAdministrators()
    {
        await CreateDashboard(CreateTestDashboard("Public"));
        await CreateDashboard(CreateTestDashboard("Secret", adminOnly: true));

        var user = NewUser(username: "dashUser", role: Role.User);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var visible = await GetAllDashboards(login.Token);
        Assert.That(visible.Select(d => d.Name), Is.EquivalentTo(new[] { "Public" }));

        var adminOnly = (await GetAllDashboards()).Single(d => d.AdminOnly);
        await ApiClient.GetAndVerify($"/dashboards/{adminOnly.Id}", HttpStatusCode.NotFound, tokenOverride: login.Token);
    }

    [Test]
    public async Task ShallAllowDashboardRoleToReadNonAdminDashboards()
    {
        var created = await CreateDashboard(CreateTestDashboard("Wallboard"));

        var user = NewUser(username: "wallboard", role: Role.Dashboard);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var visible = await GetAllDashboards(login.Token);
        Assert.That(visible.Count, Is.EqualTo(1));
        Assert.That(visible[0].Id, Is.EqualTo(created.Id));

        var fetched = await GetDashboard(created.Id!.Value, login.Token);
        Assert.That(fetched, Is.Not.Null);
    }

    [Test]
    public async Task ShallDenyLookupServiceDashboardAccess()
    {
        await CreateDashboard(CreateTestDashboard("Nope"));

        var user = NewUser(username: "lookup", role: Role.LookupService);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        await ApiClient.GetAndVerify("/dashboards", HttpStatusCode.Unauthorized, tokenOverride: login.Token);
    }

    [Test]
    public async Task ShallDenyNonAdminMutations()
    {
        var created = await CreateDashboard(CreateTestDashboard("Locked"));

        var user = NewUser(username: "readonlyDash", role: Role.ReadOnly);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var createResponse = await CreateDashboardRaw(CreateTestDashboard("Hack"), login.Token);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var putResponse = await ApiClient.Put<HttpResponseMessage>(
            $"/dashboards/{created.Id}", created, authenticate: true, tokenOverride: login.Token, validateSuccess: false);
        Assert.That(putResponse!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallAllowDashboardRoleToGetClientsAndStatuses()
    {
        var user = NewUser(username: "dashData", role: Role.Dashboard);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        await ApiClient.GetAndVerify("/clients", HttpStatusCode.OK, tokenOverride: login.Token);
        await ApiClient.GetAndVerify("/statuses", HttpStatusCode.OK, tokenOverride: login.Token);
    }

    [Test]
    public async Task ShallAllowDashboardRoleToGetSettingGroups()
    {
        var user = NewUser(username: "dashGroups", role: Role.Dashboard);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        await ApiClient.GetAndVerify("/settinggroups", HttpStatusCode.OK, tokenOverride: login.Token);
    }
}
