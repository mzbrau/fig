using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Fig.Client.ClientSecret;
using Fig.Client.Factories;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fig.Client.ConfigurationProvider;

public class FigConfigurationSource : IFigConfigurationSource
{
    public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();

    public List<string>? ApiUris { get; set; }

    public double PollIntervalMs { get; set; } = 30000;

    public bool LiveReload { get; set; } = true;

    public string? Instance { get; set; }

    public string ClientName { get; set; } = default!;

    public string? VersionOverride { get; set; }

    public bool AllowOfflineSettings { get; set; } = true;

    public Type SettingsType { get; set; } = default!;

    public bool SupportsRestart { get; set; }

    public HttpClient? HttpClient { get; set; }

    public string? ClientSecretOverride { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var logger = LoggerFactory.CreateLogger<FigConfigurationProvider>();

        var settings = (SettingsBase)Activator.CreateInstance(SettingsType);
        var clientSecretProvider = CreateClientSecretProvider();
        var ipAddressResolver = new IpAddressResolver();
        var offlineSettingsManager = CreateOfflineSettingsManager(clientSecretProvider);
        var httpClient = CreateHttpClient();
        var statusMonitor = CreateStatusMonitor(ipAddressResolver, clientSecretProvider, httpClient);
        var communicationHandler = CreateCommunicationHandler(httpClient, ipAddressResolver, clientSecretProvider);

        return new FigConfigurationProvider(this, logger, ipAddressResolver, offlineSettingsManager, statusMonitor, settings, communicationHandler);
    }

    private IClientSecretProvider CreateClientSecretProvider()
    {
        if (ClientSecretOverride is null)
            return new ClientSecretProvider();

        var secretProviderLogger = LoggerFactory.CreateLogger<InCodeClientSecretProvider>();
        return new InCodeClientSecretProvider(secretProviderLogger, ClientSecretOverride);
    }

    protected virtual IApiCommunicationHandler CreateCommunicationHandler(HttpClient httpClient, IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider)
    {
        var communicationHandlerLogger = LoggerFactory.CreateLogger<ApiCommunicationHandler>();
        return new ApiCommunicationHandler(
            httpClient,
            communicationHandlerLogger,
            ipAddressResolver,
            clientSecretProvider);
    }

    protected virtual ISettingStatusMonitor CreateStatusMonitor(IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider, HttpClient httpClient)
    {
        var statusMonitorLogger = LoggerFactory.CreateLogger<SettingStatusMonitor>();
        var statusMonitor = new SettingStatusMonitor(
            ipAddressResolver,
            new VersionProvider(),
            new Diagnostics(),
            new SimpleHttpClientFactory(new Dictionary<string, HttpClient>()
            {
                { HttpClientNames.FigApi, httpClient }
            }),
            this,
            clientSecretProvider,
            statusMonitorLogger,
            SupportsRestart);

        return statusMonitor;
    }

    protected virtual HttpClient CreateHttpClient()
    {
        if (HttpClient is not null)
            return HttpClient;
        
        var clientFactoryLogger = LoggerFactory.CreateLogger<ValidatedHttpClientFactory>();
        var factory = new ValidatedHttpClientFactory(clientFactoryLogger);
        return factory.CreateClient(ApiUris).GetAwaiter().GetResult();
    }

    private IOfflineSettingsManager CreateOfflineSettingsManager(IClientSecretProvider clientSecretProvider)
    {
        var offlineSettingsManagerLogger = LoggerFactory.CreateLogger<OfflineSettingsManager>();
        return new OfflineSettingsManager(
            new Cryptography(),
            new BinaryFile(),
            clientSecretProvider,
            offlineSettingsManagerLogger);
    }
}