using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Fig.Api.ExtensionMethods;

public static class ServiceCollectionExtensions
{
    public static void ConfigureForwardHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure forwarded headers (X-Forwarded-For, X-Forwarded-Proto) with trusted proxies/networks
        var forwardedHeaderSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>();
        if (forwardedHeaderSettings?.TrustForwardedHeaders == true)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                // Only trust explicitly configured proxies/networks
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();

                if (forwardedHeaderSettings.KnownProxies is not null)
                {
                    foreach (var proxy in forwardedHeaderSettings.KnownProxies)
                    {
                        if (!string.IsNullOrWhiteSpace(proxy) && IPAddress.TryParse(proxy, out var ip))
                        {
                            options.KnownProxies.Add(ip);
                        }
                    }
                }

                if (forwardedHeaderSettings.KnownNetworks is not null)
                {
                    foreach (var network in forwardedHeaderSettings.KnownNetworks)
                    {
                        if (string.IsNullOrWhiteSpace(network)) continue;
                        var parts = network.Split('/');
                        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var prefixLen))
                        {
                            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLen));
                        }
                    }
                }
            });
        }
    }
}