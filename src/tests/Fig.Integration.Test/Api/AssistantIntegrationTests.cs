using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Contracts.Assistant;
using Fig.Contracts.Authentication;
using Fig.Test.Common;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class AssistantIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetStatus_ReturnsDisabled_WhenAssistantNotEnabled()
    {
        await SetConfiguration(CreateConfiguration(enableFigAssistant: false));

        var status = await GetAssistantStatus();

        Assert.That(status.Enabled, Is.False);
    }

    [Test]
    public async Task GetStatus_ReturnsEnabled_WhenAssistantEnabled()
    {
        await SetConfiguration(CreateConfiguration(
            enableFigAssistant: true,
            figAssistantEndpoint: "https://example.test/v1",
            figAssistantModel: "test-model",
            figAssistantAccessToken: "test-token"));

        var status = await GetAssistantStatus();

        Assert.That(status.Enabled, Is.True);
    }

    [Test]
    public async Task GetStatus_RejectsNonAdministrator()
    {
        var user = NewUser(username: $"assistant-user-{GetNewSecret()[..8]}", role: Role.User);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        await ApiClient.GetAndVerify(
            "/assistant/status",
            HttpStatusCode.Unauthorized,
            tokenOverride: $"Bearer {login.Token}");
    }

    [Test]
    public async Task Chat_ReturnsForbidden_WhenAssistantDisabled()
    {
        await SetConfiguration(CreateConfiguration(enableFigAssistant: false));

        var response = await PostAssistantChat(CreateChatRequest(), validateSuccess: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("disabled"));
    }

    [Test]
    public async Task Chat_ReturnsBadRequest_WhenNotFullyConfigured()
    {
        await SetConfiguration(CreateConfiguration(
            enableFigAssistant: true,
            figAssistantEndpoint: null,
            figAssistantModel: null,
            figAssistantAccessToken: null));

        var response = await PostAssistantChat(CreateChatRequest(), validateSuccess: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("not fully configured"));
    }

    [Test]
    public async Task Chat_StreamsEvents_WhenConfigured()
    {
        await SetConfiguration(CreateConfiguration(
            enableFigAssistant: true,
            figAssistantEndpoint: "https://example.test/v1",
            figAssistantModel: "test-model",
            figAssistantAccessToken: "test-token"));

        var response = await PostAssistantChat(CreateChatRequest());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("event: progress"));
        Assert.That(body, Does.Contain("event: token"));
        Assert.That(body, Does.Contain("Hello from test assistant."));
        Assert.That(body, Does.Contain("event: done"));
    }

    [Test]
    public async Task Chat_RejectsNonAdministrator()
    {
        await SetConfiguration(CreateConfiguration(
            enableFigAssistant: true,
            figAssistantEndpoint: "https://example.test/v1",
            figAssistantModel: "test-model",
            figAssistantAccessToken: "test-token"));

        var user = NewUser(username: $"assistant-chat-{GetNewSecret()[..8]}", role: Role.User);
        await CreateUser(user);
        var login = await Login(user.Username, user.Password!);

        var response = await PostAssistantChat(
            CreateChatRequest(),
            tokenOverride: $"Bearer {login.Token}",
            validateSuccess: false);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static AssistantChatRequestDataContract CreateChatRequest()
    {
        return new AssistantChatRequestDataContract
        {
            Messages =
            [
                new AssistantChatMessageDataContract
                {
                    Role = "user",
                    Content = "Hello"
                }
            ]
        };
    }
}
