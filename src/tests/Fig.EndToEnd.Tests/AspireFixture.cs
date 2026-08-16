using System.Net.Http.Headers;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Fig.EndToEnd.Tests;

[SetUpFixture]
public class AspireFixture
{
    public const string WebBaseUrl = "https://localhost:7148";
    public const string ApiBaseUrl = "https://localhost:7281";

    private static DistributedApplication? _app;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    public static IBrowser Browser =>
        _browser ?? throw new InvalidOperationException("Playwright browser is not initialized.");

    public static DistributedApplication App =>
        _app ?? throw new InvalidOperationException("Aspire application is not initialized.");

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        // Idempotent; downloads browsers when missing (local + CI).
        Microsoft.Playwright.Program.Main(["install", "chromium"]);

        var externalWebUrl = Environment.GetEnvironmentVariable("FIG_E2E_WEB_URL");
        if (string.IsNullOrWhiteSpace(externalWebUrl))
        {
            await StartAspireAsync();
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static async Task StartAspireAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Fig_E2E_AppHost>(
                ["DcpPublisher:RandomizePorts=false"]);

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("fig-api", cts.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("fig-web", cts.Token);

        await WaitForClientsRegisteredAsync(cts.Token);
        await EnableDisplayScriptsAsync(cts.Token);
    }

    private static async Task EnableDisplayScriptsAsync(CancellationToken cancellationToken)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(ApiBaseUrl) };

        var token = await TryAuthenticateAsync(http, cancellationToken)
                    ?? throw new InvalidOperationException("Could not authenticate to enable display scripts.");

        using (var getRequest = new HttpRequestMessage(HttpMethod.Get, "/configuration"))
        {
            getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var getResponse = await http.SendAsync(getRequest, cancellationToken);
            getResponse.EnsureSuccessStatusCode();

            var json = await getResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();

            using var stream = new MemoryStream();
            await using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in root.EnumerateObject())
                {
                    if (property.NameEquals("AllowDisplayScripts") || property.NameEquals("allowDisplayScripts"))
                    {
                        writer.WriteBoolean(property.Name, true);
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                if (!root.TryGetProperty("AllowDisplayScripts", out _) &&
                    !root.TryGetProperty("allowDisplayScripts", out _))
                {
                    writer.WriteBoolean("AllowDisplayScripts", true);
                }

                writer.WriteEndObject();
            }

            using var putRequest = new HttpRequestMessage(HttpMethod.Put, "/configuration")
            {
                Content = new ByteArrayContent(stream.ToArray())
            };
            putRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var putResponse = await http.SendAsync(putRequest, cancellationToken);
            putResponse.EnsureSuccessStatusCode();
        }
    }

    private static async Task WaitForClientsRegisteredAsync(CancellationToken cancellationToken)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(ApiBaseUrl) };

        string? token = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            token ??= await TryAuthenticateAsync(http, cancellationToken);
            if (token is null)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "/clients");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var names = doc.RootElement.EnumerateArray()
                    .Select(GetClientName)
                    .Where(n => n is not null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (names.Contains("AspNetApi") && names.Contains("DisplayScriptExample"))
                    return;
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for AspNetApi and DisplayScriptExample to register.");
    }

    private static async Task<string?> TryAuthenticateAsync(HttpClient http, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.PostAsync(
                "/users/authenticate",
                new StringContent(
                    """{"Username":"admin","Password":"admin"}""",
                    System.Text.Encoding.UTF8,
                    "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (doc.RootElement.TryGetProperty("Token", out var token))
                return token.GetString();
            if (doc.RootElement.TryGetProperty("token", out var camelToken))
                return camelToken.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetClientName(JsonElement element)
    {
        if (element.TryGetProperty("Name", out var name))
            return name.GetString();
        if (element.TryGetProperty("name", out var camelName))
            return camelName.GetString();
        return null;
    }
}
