using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.Diagnostics;
using Fig.Test.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class DiagnosticsIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task RecordWebClientLoadTiming_ReturnsNoContent()
    {
        var response = await PostWebClientLoadTiming(CreateLoadTiming());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RecordWebClientSaveTiming_ReturnsNoContent()
    {
        var response = await PostWebClientSaveTiming(CreateSaveTiming());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RecordWebClientLoadTiming_AllowsReadOnlyUser()
    {
        var user = NewUser(username: $"diag-ro-{GetNewSecret()[..8]}", role: Role.ReadOnly);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var response = await PostWebClientLoadTiming(
            CreateLoadTiming(),
            tokenOverride: $"Bearer {login.Token}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RecordWebClientSaveTiming_RejectsReadOnlyUser()
    {
        var user = NewUser(username: $"diag-ro-save-{GetNewSecret()[..8]}", role: Role.ReadOnly);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var response = await PostWebClientSaveTiming(
            CreateSaveTiming(),
            tokenOverride: $"Bearer {login.Token}",
            validateSuccess: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RecordWebClientLoadTiming_AllowsUserRole()
    {
        var user = NewUser(username: $"diag-user-{GetNewSecret()[..8]}", role: Role.User);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var response = await PostWebClientSaveTiming(
            CreateSaveTiming(),
            tokenOverride: $"Bearer {login.Token}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task RecordWebClientLoadTiming_RejectsUnauthenticated()
    {
        using var httpClient = GetHttpClient();
        var response = await httpClient.PostAsync(
            "/diagnostics/web-client-load",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static WebClientLoadTimingDataContract CreateLoadTiming()
    {
        return new WebClientLoadTimingDataContract(
            DateTime.UtcNow.AddSeconds(-2),
            totalDurationMs: 1500,
            clientCount: 3,
            settingCount: 20,
            stages: new List<WebClientLoadTimingStageDataContract>
            {
                new(WebClientLoadTimingStageNames.HttpFetchClients, 800),
                new(WebClientLoadTimingStageNames.ConvertToModels, 400),
                new(WebClientLoadTimingStageNames.InitializeModels, 300)
            });
    }

    private static WebClientSaveTimingDataContract CreateSaveTiming()
    {
        return new WebClientSaveTimingDataContract(
            DateTime.UtcNow.AddSeconds(-1),
            totalDurationMs: 600,
            clientCount: 2,
            dirtyClientCount: 1,
            settingChangeCount: 3,
            httpPutCount: 1,
            isSaveAll: false,
            stages: new List<WebClientSaveTimingStageDataContract>
            {
                new(WebClientSaveTimingStageNames.HttpPutSettings, 500),
                new(WebClientSaveTimingStageNames.Other, 100)
            });
    }
}
