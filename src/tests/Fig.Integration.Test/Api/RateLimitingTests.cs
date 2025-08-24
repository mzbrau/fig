using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Test.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

// Ensure tests in this fixture do not run in parallel due to environment variable mutation
[NonParallelizable]
public class RateLimitingTests : IntegrationTestBase
{
    [Test]
    public async Task RateLimiting_WhenDisabled_ShouldAllowAllRequests()
    {
        var (conn, path) = NewTempDb();
        var env = new Dictionary<string, string?>
        {
            ["ApiSettings__RateLimiting__GlobalPolicy__Enabled"] = "false",
            ["ApiSettings__DbConnectionString"] = conn
        };

        try
        {
            using var _ = new EnvScope(env);
            await using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            // Act - Make many requests that would exceed any small limit
            var responses = new List<HttpStatusCode>();
            for (var i = 0; i < 50; i++)
            {
                var response = await client.GetAsync("/apiversion");
                responses.Add(response.StatusCode);
            }

            // Assert - none should be 429
            Assert.That(responses.Any(s => s == HttpStatusCode.TooManyRequests), Is.False,
                "No requests should be rate limited when disabled");
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task RateLimiting_WhenEnabled_ShouldEnforceGlobalLimit()
    {
        // Use a very low limit and no queue for deterministic behavior
        var (conn, path) = NewTempDb();
        var env = new Dictionary<string, string?>
        {
            ["ApiSettings__RateLimiting__GlobalPolicy__Enabled"] = "true",
            ["ApiSettings__RateLimiting__GlobalPolicy__PermitLimit"] = "3",
            ["ApiSettings__RateLimiting__GlobalPolicy__Window"] = "00:01:00",
            ["ApiSettings__RateLimiting__GlobalPolicy__QueueLimit"] = "0",
            ["ApiSettings__DbConnectionString"] = conn
        };

        try
        {
            using var _ = new EnvScope(env);
            await using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            var statuses = new List<HttpStatusCode>();

            // Act - sequential requests to hit the window limit
            for (var i = 0; i < 10; i++)
            {
                var response = await client.GetAsync("/apiversion");
                statuses.Add(response.StatusCode);
            }

            // Assert - first 3 allowed, remainder rejected with 429
            var allowed = statuses.TakeWhile(s => s != HttpStatusCode.TooManyRequests).Count();
            var rejected = statuses.Count(s => s == HttpStatusCode.TooManyRequests);

            Assert.That(allowed, Is.EqualTo(3), "Exactly the permit limit should pass within the window");
            Assert.That(rejected, Is.EqualTo(7), "Remaining requests in the window should be rejected");
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }

    [Test]
    public async Task RateLimiting_Rejection_ReturnsHelpfulMessage()
    {
        var (conn, path) = NewTempDb();
        var env = new Dictionary<string, string?>
        {
            ["ApiSettings__RateLimiting__GlobalPolicy__Enabled"] = "true",
            ["ApiSettings__RateLimiting__GlobalPolicy__PermitLimit"] = "1",
            ["ApiSettings__RateLimiting__GlobalPolicy__Window"] = "00:01:00",
            ["ApiSettings__RateLimiting__GlobalPolicy__QueueLimit"] = "0",
            ["ApiSettings__DbConnectionString"] = conn
        };

        try
        {
            using var _ = new EnvScope(env);
            await using var factory = new WebApplicationFactory<Program>();
            using var client = factory.CreateClient();

            // First request should succeed
            var first = await client.GetAsync("/apiversion");
            Assert.That(first.IsSuccessStatusCode, Is.True);

            // Second within same window should be rejected
            var second = await client.GetAsync("/apiversion");
            Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            var body = await second.Content.ReadAsStringAsync();
            Assert.That(body, Does.Contain("Rate limit exceeded").IgnoreCase);
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }
    
    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originals = new();

        public EnvScope(IDictionary<string, string?> vars)
        {
            foreach (var kvp in vars)
            {
                _originals[kvp.Key] = Environment.GetEnvironmentVariable(kvp.Key);
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        public void Dispose()
        {
            foreach (var kvp in _originals)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
    }

    private static (string Conn, string Path) NewTempDb()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fig_rate_limit_{Guid.NewGuid():N}.db");
        return ($"Data Source={path};Version=3;New=True", path);
    }
}