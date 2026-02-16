using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Test.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class KeycloakAuthenticationIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallReturnNotFoundForAuthenticateEndpointInKeycloakMode()
    {
        EnableKeycloakModeForTests();

        var authContract = new AuthenticateRequestDataContract("any-user", "any-password");
        var json = JsonConvert.SerializeObject(authContract);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpClient = GetHttpClient();

        var response = await httpClient.PostAsync("/users/authenticate", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ShallReturnNotFoundForUsersEndpointWithValidAdminKeycloakToken()
    {
        EnableKeycloakModeForTests();
        var token = CreateKeycloakToken(Role.Administrator);

        using var response = await ApiClient.GetRaw("/users", token);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), responseBody);
    }

    [Test]
    public async Task ShallRejectTokenWithNoMappedRole()
    {
        EnableKeycloakModeForTests();
        var token = CreateKeycloakToken(null);

        using var response = await ApiClient.GetRaw("/clients", token);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), responseBody);
    }

    [Test]
    public async Task ShallRejectNonAdminTokenWithMissingAllowedClassifications()
    {
        EnableKeycloakModeForTests();
        var token = CreateKeycloakToken(Role.User, includeAllowedClassificationsClaim: false);

        using var response = await ApiClient.GetRaw("/clients", token);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), responseBody);
    }

    [Test]
    public async Task ShallAllowAdminTokenWithMissingAllowedClassifications()
    {
        EnableKeycloakModeForTests();
        var token = CreateKeycloakToken(Role.Administrator, includeAllowedClassificationsClaim: false);

        using var response = await ApiClient.GetRaw("/clients", token);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), responseBody);
    }

    [Test]
    public async Task ShallRejectTokenWithInvalidClientFilterRegex()
    {
        EnableKeycloakModeForTests();
        var token = CreateKeycloakToken(Role.Administrator, clientFilter: "[");

        using var response = await ApiClient.GetRaw("/clients", token);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), responseBody);
    }
}
