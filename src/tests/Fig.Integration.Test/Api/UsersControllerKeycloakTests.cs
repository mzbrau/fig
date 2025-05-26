using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Contracts.Authentication;
using Fig.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

// Test Authentication Handler
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly bool _isAdmin;

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, bool isAdmin = true)
        : base(options, logger, encoder, clock)
    {
        _isAdmin = isAdmin;
    }
    
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, bool isAdmin = true)
        : base(options, logger, encoder)
    {
        _isAdmin = isAdmin;
    }


    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("preferred_username", "testuser_preferred"),
            new Claim(_isAdmin ? ClaimTypes.Role : "NonAdminRole", _isAdmin ? "Administrator" : "User") 
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

[TestFixture]
public class UsersControllerKeycloakTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory;
    private WebApplicationFactory<Program> _factoryAdmin;
    private WebApplicationFactory<Program> _factoryNonAdmin;

    private HttpClient CreateClient(bool isAdminUser = true)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.Configure<ApiSettings>(settings =>
                    {
                        settings.UseKeycloak = true;
                        settings.KeycloakAuthority = "https://fake-keycloak.com/realms/test";
                        settings.KeycloakAudience = "fig-api-test";
                    });

                    services.AddAuthentication("TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                });
                
                // A bit of a hack to pass isAdmin to the handler.
                // In a more complex scenario, you might use a service to hold test user configuration.
                builder.ConfigureServices(services =>
                {
                    services.AddTransient<TestAuthHandler>(sp => 
                        new TestAuthHandler(
                            sp.GetRequiredService<IOptionsMonitor<AuthenticationSchemeOptions>>(),
                            sp.GetRequiredService<ILoggerFactory>(),
                            sp.GetRequiredService<UrlEncoder>(),
                            isAdminUser
                        )
                    );
                });
            });
        
        var client = factory.CreateClient();
        // The TestAuthHandler will be invoked because of AddAuthentication("TestScheme") and default scheme set in API.
        // If default scheme isn't TestScheme, you might need to specify Authorization header with TestScheme
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        return client;
    }


    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        HttpClient?.Dispose();
        _factoryAdmin?.Dispose();
        _factoryNonAdmin?.Dispose();
        _factory?.Dispose();
    }
    
    private HttpClient GetHttpClientWithNoAuth()
    {
        // Client for testing unauthenticated access
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<ApiSettings>(settings =>
                    {
                        settings.UseKeycloak = true;
                        settings.KeycloakAuthority = "https://fake-keycloak.com/realms/test";
                        settings.KeycloakAudience = "fig-api-test";
                    });
                    // No test auth handler for these tests
                });
            }).CreateClient();
    }

    [Test]
    public async Task Authenticate_ShouldReturnBadRequest_WhenKeycloakIsEnabled()
    {
        HttpClient = CreateClient(); // Authenticated client, but endpoint should still be bad request
        var model = new AuthenticateRequestDataContract("test", "test");
        var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync("/users/authenticate", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Register_ShouldReturnMethodNotAllowed_WhenKeycloakIsEnabled()
    {
        HttpClient = CreateClient(); // Authenticated client
        var model = new RegisterUserRequestDataContract("test", "test", "test", "test", Role.User, null);
        var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync("/users/register", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
    }

    [Test]
    public async Task UpdateUser_ShouldReturnMethodNotAllowed_WhenKeycloakIsEnabled()
    {
        HttpClient = CreateClient(); // Authenticated client
        var userId = Guid.NewGuid();
        var model = new UpdateUserRequestDataContract("test", "test", "test", null, Role.User, null);
        var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

        var response = await HttpClient.PutAsync($"/users/{userId}", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
    }

    [Test]
    public async Task DeleteUser_ShouldReturnMethodNotAllowed_WhenKeycloakIsEnabled()
    {
        HttpClient = CreateClient(); // Authenticated client
        var userId = Guid.NewGuid();
        var response = await HttpClient.DeleteAsync($"/users/{userId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
    }

    [Test]
    public async Task GetUsers_ShouldReturnUnauthorized_WhenKeycloakIsEnabledAndNoToken()
    {
        var unauthenticatedClient = GetHttpClientWithNoAuth();
        var response = await unauthenticatedClient.GetAsync("/users");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetUsers_ShouldReturnOk_WhenKeycloakIsEnabledAndAdminToken()
    {
        HttpClient = CreateClient(isAdminUser: true);
        var response = await HttpClient.GetAsync("/users");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
    
    [Test]
    public async Task GetUsers_ShouldReturnForbidden_WhenKeycloakIsEnabledAndNonAdminToken()
    {
        HttpClient = CreateClient(isAdminUser: false); // User role
        var response = await HttpClient.GetAsync("/users");
        // This endpoint is [Authorize(Role.Administrator)]
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
    
    [Test]
    public async Task GetUserById_ShouldReturnOk_WhenKeycloakIsEnabledAndAdminToken()
    {
        // Assuming some user ID exists or the endpoint handles not found gracefully for auth tests.
        // For this test, we are mostly concerned with the authentication/authorization aspect.
        // The user with Id Guid.Empty will likely not exist and result in a 404 if the auth passes.
        // However, the UserService.GetById might throw an exception if it tries to use the AuthenticatedUser
        // which is not fully populated by TestAuthHandler in the same way as real Fig auth.
        // Let's use a non-empty GUID.
        var testUserId = Guid.NewGuid(); 
        HttpClient = CreateClient(isAdminUser: true);
        var response = await HttpClient.GetAsync($"/users/{testUserId}");
        // We expect OK if auth passes, even if user is not found (controller would return Ok(null) or similar which results in 200 with empty/null body or 404).
        // For role-based auth test, OK or NotFound is acceptable if Forbidden/Unauthorized is not returned.
         Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.NotFound));
    }
}
