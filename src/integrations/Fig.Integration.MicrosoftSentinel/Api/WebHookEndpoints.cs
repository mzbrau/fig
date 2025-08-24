using Fig.Integration.MicrosoftSentinel.Handlers;
using Fig.WebHooks.Contracts;

namespace Fig.Integration.MicrosoftSentinel.Api;

public static class WebHookEndpoints
{
    public static void MapWebHookEndpoints(this WebApplication app)
    {
        var logger = app.Logger;
        
        // Map webhook endpoints
        app.MapPost("/SecurityEvent", async (SecurityEventDataContract securityEvent, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleSecurityEventAsync(securityEvent);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process security event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/SettingValueChanged", async (SettingValueChangedDataContract settingValueChanged, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleSettingValueChangedAsync(settingValueChanged);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process setting value changed event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/ClientRegistration", async (ClientRegistrationDataContract clientRegistration, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleClientRegistrationAsync(clientRegistration);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process client registration event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/NewClientRegistration", async (ClientRegistrationDataContract clientRegistration, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleClientRegistrationAsync(clientRegistration);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process new client registration event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/UpdatedClientRegistration", async (ClientRegistrationDataContract clientRegistration, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleClientRegistrationAsync(clientRegistration);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process updated client registration event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/ClientStatusChanged", async (ClientStatusChangedDataContract clientStatusChanged, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleClientStatusChangedAsync(clientStatusChanged);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process client status changed event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/ClientHealthChanged", async (ClientHealthChangedDataContract clientHealthChanged, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleClientHealthChangedAsync(clientHealthChanged);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process client health changed event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/HealthStatusChanged", async (ClientHealthChangedDataContract clientHealthChanged, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleClientHealthChangedAsync(clientHealthChanged);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process health status changed event");
                return Results.StatusCode(500);
            }
        });

        app.MapPost("/MinRunSessions", async (MinRunSessionsDataContract minRunSessions, IWebHookHandler handler) =>
        {
            try
            {
                var success = await handler.HandleMinRunSessionsAsync(minRunSessions);
                return success ? Results.Ok() : Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process min run sessions event");
                return Results.StatusCode(500);
            }
        });

        // Health check endpoint
        app.MapHealthChecks("/health");

        // Root endpoint for basic info
        app.MapGet("/", () => new
        {
            Service = "Fig Microsoft Sentinel Integration",
            Status = "Running",
            Description = "Forwards Fig webhook events to Microsoft Sentinel for SIEM monitoring"
        });
    }
}