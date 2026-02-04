using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.ClientRegistrationHistory;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ClientRegistrationHistoryTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRecordHistoryOnInitialRegistration()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var history = await GetClientRegistrationHistory();

        Assert.That(history.Registrations.Count, Is.EqualTo(1));
        Assert.That(history.Registrations[0].ClientName, Is.EqualTo(settings.ClientName));
        Assert.That(history.Registrations[0].Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallRecordHistoryOnUpdatedRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithTwoSettings>(secret);
        await RegisterSettings<ClientXWithThreeSettings>(secret);

        var history = await GetClientRegistrationHistory();

        Assert.That(history.Registrations.Count, Is.EqualTo(2));
        Assert.That(history.Registrations.All(r => r.ClientName == "ClientX"), Is.True);
        
        // First registration should have 2 settings, second should have 3
        var orderedByDate = history.Registrations.OrderBy(r => r.RegistrationDateUtc).ToList();
        Assert.That(orderedByDate[0].Settings.Count, Is.EqualTo(2));
        Assert.That(orderedByDate[1].Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallNotRecordHistoryOnIdenticalRegistration()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ThreeSettings>(secret);
        await RegisterSettings<ThreeSettings>(secret);

        var history = await GetClientRegistrationHistory();

        Assert.That(history.Registrations.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallRecordHistoryForMultipleClients()
    {
        await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        await RegisterSettings<AllSettingsAndTypes>();

        var history = await GetClientRegistrationHistory();

        Assert.That(history.Registrations.Count, Is.EqualTo(3));
        var clientNames = history.Registrations.Select(r => r.ClientName).OrderBy(n => n).ToList();
        Assert.That(clientNames, Is.EqualTo(new List<string> { "AllSettingsAndTypes", "ClientA", "ThreeSettings" }));
    }

    [Test]
    public async Task ShallClearHistory()
    {
        await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();

        var historyBefore = await GetClientRegistrationHistory();
        Assert.That(historyBefore.Registrations.Count, Is.EqualTo(2));

        await ClearClientRegistrationHistory();

        var historyAfter = await GetClientRegistrationHistory();
        Assert.That(historyAfter.Registrations.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallCaptureSettingDefaultValues()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var history = await GetClientRegistrationHistory();

        Assert.That(history.Registrations.Count, Is.EqualTo(1));
        var registration = history.Registrations[0];
        
        var stringSetting = registration.Settings.FirstOrDefault(s => s.Name == nameof(settings.AStringSetting));
        Assert.That(stringSetting, Is.Not.Null);
        Assert.That(stringSetting!.DefaultValue, Is.Not.Null);
    }

    [Test]
    public async Task ShallCaptureRegistrationDateTime()
    {
        var beforeRegistration = DateTime.UtcNow;
        await RegisterSettings<ThreeSettings>();
        var afterRegistration = DateTime.UtcNow;

        var history = await GetClientRegistrationHistory();

        Assert.That(history.Registrations.Count, Is.EqualTo(1));
        var registration = history.Registrations[0];
        Assert.That(registration.RegistrationDateUtc, Is.GreaterThanOrEqualTo(beforeRegistration));
        Assert.That(registration.RegistrationDateUtc, Is.LessThanOrEqualTo(afterRegistration));
    }

    [Test]
    public async Task ShallRequireAdminRoleToGetHistory()
    {
        var user = NewUser(role: Contracts.Authentication.Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);

        await RegisterSettings<ThreeSettings>();

        var response = await TryGetClientRegistrationHistory(loginResult.Token);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallRequireAdminRoleToClearHistory()
    {
        var user = NewUser(role: Contracts.Authentication.Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);

        await RegisterSettings<ThreeSettings>();

        var response = await TryClearClientRegistrationHistory(loginResult.Token);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
    }

    private async Task<ClientRegistrationHistoryCollectionDataContract> GetClientRegistrationHistory()
    {
        const string uri = "/clientregistrationhistory";
        var result = await ApiClient.Get<ClientRegistrationHistoryCollectionDataContract>(uri);
        return result ?? new ClientRegistrationHistoryCollectionDataContract();
    }

    private async Task<System.Net.Http.HttpResponseMessage> TryGetClientRegistrationHistory(string token)
    {
        const string uri = "/clientregistrationhistory";
        return await ApiClient.GetRaw(uri, token);
    }

    private async Task<System.Net.Http.HttpResponseMessage> TryClearClientRegistrationHistory(string token)
    {
        const string uri = "/clientregistrationhistory";
        return await ApiClient.DeleteRaw(uri, token);
    }
}
