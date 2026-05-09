using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Configuration;
using Fig.Client.ExtensionMethods;
using Fig.Client.Health;
using Fig.Client.Workers;
using Fig.Unit.Test.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigRegistrationExtensionsTests
{
    [Test]
    public void IsFigDisabled_WithDisableFigArg_ReturnsTrue()
    {
        var args = new[] { "app.dll", "--disable-fig=true" };

        Assert.That(FigCommandLine.IsFigDisabled(args), Is.True);
    }

    [Test]
    public void IsFigDisabled_WithoutDisableFigArg_ReturnsFalse()
    {
        var args = new[] { "app.dll", "--some-other-arg" };

        Assert.That(FigCommandLine.IsFigDisabled(args), Is.False);
    }

    [Test]
    public void IsFigDisabled_WithEmptyArgs_ReturnsFalse()
    {
        var args = Array.Empty<string>();

        Assert.That(FigCommandLine.IsFigDisabled(args), Is.False);
    }

    [Test]
    public void IsFigDisabled_WithDisableFigFalse_ReturnsFalse()
    {
        var args = new[] { "app.dll", "--disable-fig=false" };

        Assert.That(FigCommandLine.IsFigDisabled(args), Is.False);
    }

    [Test]
    public void IsFigDisabled_WithNullArgs_ReturnsFalse()
    {
        Assert.That(FigCommandLine.IsFigDisabled(null), Is.False);
    }

    [Test]
    public void UseFig_WhenDisabled_DoesNotRegisterFigServices()
    {
        using var host = BuildHost(["app.dll", FigCommandLine.DisableFigArg], out var services);

        AssertHostedServices(services, shouldBeRegistered: false);
        Assert.That(HasFigHealthCheck(host.Services), Is.False);
        Assert.That(services.Any(a => a.ServiceType == typeof(ISettingUpdater)), Is.False);
        Assert.That(services.Any(a => a.ServiceType == typeof(ISettingUpdater<SimpleSettings>)), Is.False);
    }

    [Test]
    public void UseFig_WhenEnabled_RegistersFigServices()
    {
        using var host = BuildHost(["app.dll"], out var services);

        AssertHostedServices(services, shouldBeRegistered: true);
        Assert.That(HasFigHealthCheck(host.Services), Is.True);
        Assert.That(services.Any(a => a.ServiceType == typeof(ISettingUpdater)), Is.True);
        Assert.That(services.Any(a => a.ServiceType == typeof(ISettingUpdater<SimpleSettings>)), Is.True);
    }

    [Test]
    public void UseFig_WhenEnabled_RegistersConfigurationHealthCheckAsSingleton()
    {
        using var _ = BuildHost(["app.dll"], out var services);

        var healthCheckRegistration = services.SingleOrDefault(a =>
            a.ServiceType == typeof(FigConfigurationHealthCheck<SimpleSettings>));

        Assert.That(healthCheckRegistration, Is.Not.Null);
        Assert.That(healthCheckRegistration!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(healthCheckRegistration.ImplementationType, Is.EqualTo(typeof(FigConfigurationHealthCheck<SimpleSettings>)));
    }

    private static IHost BuildHost(string[]? args, out IServiceCollection services)
    {
        var originalProvider = FigCommandLine.CommandLineArgsProvider;
        IServiceCollection? capturedServices = null;

        try
        {
            FigCommandLine.CommandLineArgsProvider = () => args;

            var builder = new HostBuilder();
            builder.UseFig<SimpleSettings>();
            builder.ConfigureServices((_, serviceCollection) => capturedServices = serviceCollection);

            return builder.Build();
        }
        finally
        {
            FigCommandLine.CommandLineArgsProvider = originalProvider;
            services = capturedServices ?? throw new InvalidOperationException("Service collection was not captured.");
        }
    }

    private static bool HasFigHealthCheck(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        return options.Registrations.Any(a => a.Name == "Fig configuration");
    }

    private static void AssertHostedServices(IEnumerable<ServiceDescriptor> services, bool shouldBeRegistered)
    {
        AssertHostedService<FigRestartWorker<SimpleSettings>>(services, shouldBeRegistered);
        AssertHostedService<FigHealthWorker<SimpleSettings>>(services, shouldBeRegistered);
        AssertHostedService<FigCustomActionWorker<SimpleSettings>>(services, shouldBeRegistered);
        AssertHostedService<FigLookupWorker<SimpleSettings>>(services, shouldBeRegistered);
    }

    private static void AssertHostedService<THostedService>(IEnumerable<ServiceDescriptor> services, bool shouldBeRegistered)
    {
        var isRegistered = services.Any(a =>
            a.ServiceType == typeof(IHostedService) &&
            a.ImplementationType == typeof(THostedService));

        Assert.That(isRegistered, Is.EqualTo(shouldBeRegistered));
    }
}
