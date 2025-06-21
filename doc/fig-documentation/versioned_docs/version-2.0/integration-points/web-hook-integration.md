---
sidebar_position: 1



---

# Web Hook Integrations

Fig has an integration point which allows notifications to be sent as a result of certain events occurring within the system. For details on how web hooks can be configured, see [Web Hooks](../features/8-web-hooks.md).

Web hooks are sent as a POST with each type of web hook being called on a separate route. Integrations will need to listen to POST's on at least one of these routes.

Contracts are not mandatory and can be recreated from the json documents if required but they are published as a nuget package for use by integrations. This can be found here.

## Security

Web hook integrations may be used without security but to avoid unauthorized parties calling the integration it is recommended that calls to the integration be validated. Fig has support for a pre-shared secret which will be passed with each web hook call. A hashed version of the secret is provided when the client is created in the Fig web application and this can be used to validate the call.

There are a number of ways that the secret can be checked but one possible way is provided below:

```csharp
public class FigWebHookAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISettings _settings;

    public FigWebHookAuthMiddleware(RequestDelegate next, ISettings settings)
    {
        _next = next;
        _settings = settings;
    }

    public async Task Invoke(HttpContext context)
    {
        var secret = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrWhiteSpace(secret) ||
            !BCrypt.Net.BCrypt.EnhancedVerify(secret, _settings.HashedSecret))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        else
        {
            await _next(context);
        }
    }
}
```

This is drawn from the [example integration](https://github.com/mzbrau/fig/blob/main/src/integrations/Fig.Integration.ConsoleWebHookHandler/Middleware/FigWebHookAuthMiddleware.cs) on Github.