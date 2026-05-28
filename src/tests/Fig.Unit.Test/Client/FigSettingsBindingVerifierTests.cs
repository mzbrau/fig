using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Testing.Extensions;
using Fig.Client.Testing.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigSettingsBindingVerifierTests
{
    [Test]
    public void VerifyOptionsBoundShallPassWhenSettingsAreConfigured()
    {
        var settings = new BindingVerifierSettings
        {
            RootValue = "Configured",
        };

        using var services = BuildServices(settings, (collection, configuration) =>
            collection.Configure<BindingVerifierSettings>(configuration));

        FigSettingsBindingVerifier.VerifyOptionsBound(services, settings, s => s.RootValue);
    }

    [Test]
    public void VerifyOptionsBoundShallFailWhenSettingsAreNotConfigured()
    {
        var settings = new BindingVerifierSettings
        {
            RootValue = "Configured",
        };

        using var services = BuildServices(settings, (collection, _) => collection.AddOptions());

        var exception = Assert.Throws<FigSettingsBindingVerificationException>(() =>
            FigSettingsBindingVerifier.VerifyOptionsBound(services, settings, s => s.RootValue));

        Assert.That(exception?.Message, Does.Contain(nameof(BindingVerifierSettings.RootValue)));
        Assert.That(exception?.Message, Does.Contain("services.Configure"));
    }

    [Test]
    public async Task VerifyOptionsMonitorReloadsAsyncShallPassWhenSettingsAreConfigured()
    {
        var settings = new BindingVerifierSettings
        {
            RootValue = "Initial",
        };

        var reloader = new ConfigReloader<BindingVerifierSettings>();
        using var services = BuildServices(settings, (collection, configuration) =>
            collection.Configure<BindingVerifierSettings>(configuration), reloader);

        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
            services,
            reloader,
            settings,
            s => s.RootValue = "Reloaded",
            s => s.RootValue);
    }

    [Test]
    public void VerifyOptionsMonitorReloadsAsyncShallFailWhenSettingsAreNotConfigured()
    {
        var settings = new BindingVerifierSettings
        {
            RootValue = "Initial",
        };

        var reloader = new ConfigReloader<BindingVerifierSettings>();
        using var services = BuildServices(settings, (collection, _) => collection.AddOptions(), reloader);

        var exception = Assert.ThrowsAsync<FigSettingsBindingVerificationException>(async () =>
            await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
                services,
                reloader,
                settings,
                s => s.RootValue = "Reloaded",
                s => s.RootValue,
                timeout: TimeSpan.Zero));

        Assert.That(exception?.Message, Does.Contain(nameof(BindingVerifierSettings.RootValue)));
        Assert.That(exception?.Message, Does.Contain("services.Configure"));
    }

    [Test]
    public void VerifyOptionsBoundShallSupportNestedSettings()
    {
        var settings = new BindingVerifierSettings
        {
            Nested = new NestedBindingVerifierSettings
            {
                Value = "NestedConfigured",
            },
        };

        using var services = BuildServices(settings, (collection, configuration) =>
            collection.Configure<BindingVerifierSettings>(configuration));

        FigSettingsBindingVerifier.VerifyOptionsBound(services, settings, s => s.Nested.Value);
    }

    [Test]
    public async Task VerifyOptionsMonitorReloadsAsyncShallSupportConfigurationSectionOptions()
    {
        var settings = new BindingVerifierSettings
        {
            Database = new DatabaseBindingVerifierSettings
            {
                ConnectionString = "Server=initial",
            },
        };

        var reloader = new ConfigReloader<BindingVerifierSettings>();
        using var services = BuildServices(settings, (collection, configuration) =>
            collection.Configure<ConnectionStringOptions>(configuration.GetSection("ConnectionStrings")), reloader);

        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync<BindingVerifierSettings, ConnectionStringOptions, string>(
            services,
            reloader,
            settings,
            s => s.Database.ConnectionString = "Server=reloaded",
            s => s.Database.ConnectionString,
            o => o.DefaultConnection!);
    }

    [Test]
    public void VerifyOptionsBoundShallSupportNamedOptions()
    {
        const string optionsName = "named";
        var settings = new BindingVerifierSettings
        {
            RootValue = "NamedConfigured",
        };

        using var services = BuildServices(settings, (collection, configuration) =>
            collection.Configure<BindingVerifierSettings>(optionsName, configuration));

        FigSettingsBindingVerifier.VerifyOptionsBound(
            services,
            settings,
            s => s.RootValue,
            optionsName);
    }

    [Test]
    public async Task AutoMutateShallPassWhenSettingsAreBound()
    {
        var settings = new MultiTypeSettings();
        var reloader = new ConfigReloader<MultiTypeSettings>();
        using var services = BuildMultiTypeServices(settings, reloader,
            (collection, configuration) => collection.Configure<MultiTypeSettings>(configuration));

        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(services, reloader, settings);
    }

    [Test]
    public void AutoMutateShallFailWhenSettingsAreNotBound()
    {
        var settings = new MultiTypeSettings();
        var reloader = new ConfigReloader<MultiTypeSettings>();
        using var services = BuildMultiTypeServices(settings, reloader,
            (collection, _) => collection.AddOptions());

        var exception = Assert.ThrowsAsync<FigSettingsBindingVerificationException>(async () =>
            await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
                services, reloader, settings, timeout: TimeSpan.Zero));

        Assert.That(exception?.Message, Does.Contain(nameof(MultiTypeSettings.StringValue)));
        Assert.That(exception?.Message, Does.Contain("services.Configure"));
    }

    [Test]
    public async Task AutoMutateShallIncludeNestedSettings()
    {
        var settings = new MultiTypeSettings();
        var reloader = new ConfigReloader<MultiTypeSettings>();
        using var services = BuildMultiTypeServices(settings, reloader,
            (collection, configuration) => collection.Configure<MultiTypeSettings>(configuration));

        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(services, reloader, settings);
    }

    [Test]
    public void AutoMutateShallThrowInvalidOperationWhenNoMutableProperties()
    {
        var settings = new NoMutableSettings();
        var reloader = new ConfigReloader<NoMutableSettings>();
        using var services = BuildNoMutableServices(settings, reloader,
            (collection, configuration) => collection.Configure<NoMutableSettings>(configuration));

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(services, reloader, settings));
    }

    [Test]
    public async Task AutoMutateShallSupportAllScalarTypes()
    {
        var settings = new MultiTypeSettings
        {
            StringValue = "hello",
            IntValue = 10,
            BoolValue = false,
            DoubleValue = 3.14,
            GuidValue = Guid.Empty,
            TimeSpanValue = TimeSpan.FromMinutes(5),
            EnumValue = SampleEnum.First,
        };

        var reloader = new ConfigReloader<MultiTypeSettings>();
        using var services = BuildMultiTypeServices(settings, reloader,
            (collection, configuration) => collection.Configure<MultiTypeSettings>(configuration));

        await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(services, reloader, settings);
    }

    [Test]
    public void AutoMutateFailureMessageShallListAllFailingProperties()
    {
        var settings = new MultiTypeSettings();
        var reloader = new ConfigReloader<MultiTypeSettings>();
        using var services = BuildMultiTypeServices(settings, reloader,
            (collection, _) => collection.AddOptions());

        var exception = Assert.ThrowsAsync<FigSettingsBindingVerificationException>(async () =>
            await FigSettingsBindingVerifier.VerifyOptionsMonitorReloadsAsync(
                services, reloader, settings, timeout: TimeSpan.Zero));

        // All mutated properties should appear in the error message
        Assert.That(exception?.Message, Does.Contain(nameof(MultiTypeSettings.StringValue)));
        Assert.That(exception?.Message, Does.Contain(nameof(MultiTypeSettings.IntValue)));
    }

    private static ServiceProvider BuildMultiTypeServices(
        MultiTypeSettings settings,
        ConfigReloader<MultiTypeSettings> reloader,
        Action<IServiceCollection, IConfigurationRoot> configureServices)
    {
        var configuration = new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(reloader, settings)
            .Build();

        var services = new ServiceCollection();
        configureServices(services, configuration);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildNoMutableServices(
        NoMutableSettings settings,
        ConfigReloader<NoMutableSettings> reloader,
        Action<IServiceCollection, IConfigurationRoot> configureServices)
    {
        var configuration = new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(reloader, settings)
            .Build();

        var services = new ServiceCollection();
        configureServices(services, configuration);
        return services.BuildServiceProvider();
    }

    private sealed class MultiTypeSettings : SettingsBase
    {
        public override string ClientDescription => "Multi-type settings";

        [Setting("String value")]
        public string StringValue { get; set; } = "initial";

        [Setting("Int value")]
        public int IntValue { get; set; } = 42;

        [Setting("Bool value")]
        public bool BoolValue { get; set; } = true;

        [Setting("Double value")]
        public double DoubleValue { get; set; } = 1.5;

        [Setting("Guid value")]
        public Guid GuidValue { get; set; } = Guid.NewGuid();

        [Setting("TimeSpan value")]
        public TimeSpan TimeSpanValue { get; set; } = TimeSpan.FromSeconds(10);

        [Setting("Enum value")]
        public SampleEnum EnumValue { get; set; } = SampleEnum.First;

        [NestedSetting]
        public NestedMultiTypeSettings Nested { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private sealed class NestedMultiTypeSettings
    {
        [Setting("Nested string")]
        public string NestedString { get; set; } = "nested_initial";

        [Setting("Nested int")]
        public int NestedInt { get; set; } = 7;
    }

    private sealed class NoMutableSettings : SettingsBase
    {
        public override string ClientDescription => "No mutable settings";

        // No [Setting] properties - only SettingsBase infrastructure properties
        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private enum SampleEnum { First, Second, Third }

    private static ServiceProvider BuildServices(
        BindingVerifierSettings settings,
        Action<IServiceCollection, IConfigurationRoot> configureServices,
        ConfigReloader<BindingVerifierSettings>? reloader = null)
    {
        reloader ??= new ConfigReloader<BindingVerifierSettings>();
        var configuration = new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(reloader, settings)
            .Build();

        var services = new ServiceCollection();
        configureServices(services, configuration);
        return services.BuildServiceProvider();
    }

    private sealed class BindingVerifierSettings : SettingsBase
    {
        public override string ClientDescription => "Binding verifier settings";

        [Setting("Root value")]
        public string RootValue { get; set; } = "Default";

        [NestedSetting]
        public NestedBindingVerifierSettings Nested { get; set; } = new();

        [NestedSetting]
        public DatabaseBindingVerifierSettings Database { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private sealed class NestedBindingVerifierSettings
    {
        [Setting("Nested value")]
        public string Value { get; set; } = "NestedDefault";
    }

    private sealed class DatabaseBindingVerifierSettings
    {
        [Setting("Connection string")]
        [ConfigurationSectionOverride("ConnectionStrings", "DefaultConnection")]
        public string ConnectionString { get; set; } = "Server=default";
    }

    private sealed class ConnectionStringOptions
    {
        public string? DefaultConnection { get; set; }
    }
}
