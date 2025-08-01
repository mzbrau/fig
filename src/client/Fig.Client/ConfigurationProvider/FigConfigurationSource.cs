﻿using Fig.Client.ClientSecret;
using Fig.Client.Contracts;
using Fig.Client.Enums;
using Fig.Client.Exceptions;
using Fig.Client.Factories;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Fig.Client.ConfigurationProvider;

public class FigConfigurationSource : IFigConfigurationSource
{
    public ILoggerFactory? LoggerFactory { get; set; }

    public List<string>? ApiUris { get; set; }

    public double PollIntervalMs { get; set; } = 30000;

    public bool LiveReload { get; set; } = true;

    public string? Instance { get; set; }

    public string ClientName { get; set; } = null!;

    public string? VersionOverride { get; set; }

    public bool AllowOfflineSettings { get; set; } = true;

    public Type SettingsType { get; set; } = null!;

    public HttpClient? HttpClient { get; set; }
    
    public IEnumerable<IClientSecretProvider>? ClientSecretProviders { get; set; }

    public string? ClientSecretOverride { get; set; }
    
    public bool LogAppConfigConfiguration { get; set; }

    public VersionType VersionType { get; set; } = VersionType.Assembly;

    public bool AutomaticallyGenerateHeadings { get; set; } = true;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (RegisteredProviders.TryGet(ClientName, out var provider))
        {
            return provider!;
        }

        var logger = (LoggerFactory ?? new NullLoggerFactory()).CreateLogger<FigConfigurationProvider>();

        var settings = (SettingsBase)Activator.CreateInstance(SettingsType);
        var clientSecretProvider = GetFirstValidSecretProvider(logger, LoggerFactory ?? new NullLoggerFactory());
        var ipAddressResolver = new IpAddressResolver();
        var offlineSettingsManager = CreateOfflineSettingsManager(clientSecretProvider);
        var httpClient = CreateHttpClient();
        var statusMonitor = CreateStatusMonitor(ipAddressResolver, clientSecretProvider, httpClient);
        var communicationHandler = CreateCommunicationHandler(httpClient, ipAddressResolver, clientSecretProvider);

        return new FigConfigurationProvider(this, logger, ipAddressResolver, offlineSettingsManager, statusMonitor, settings, communicationHandler);
    }

    private IClientSecretProvider GetFirstValidSecretProvider(ILogger logger, ILoggerFactory loggerFactory)
    {
        if (ClientSecretProviders?.Any() != true && ClientSecretOverride == null)
        {
            throw new NoSecretProviderException();
        }

        if (ClientSecretOverride is not null)
        {
            var secretProviderLogger = (LoggerFactory ?? new NullLoggerFactory()).CreateLogger<InCodeClientSecretProvider>();
            return new InCodeClientSecretProvider(secretProviderLogger, ClientSecretOverride);
        }

        foreach (var provider in ClientSecretProviders!.Where(a => a.IsEnabled))
        {
            try
            {
                provider.AddLogger(loggerFactory);
                
                // Attempt to get the secret
                provider.GetSecret(ClientName);
                return provider;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Client Secret provider {SecretProviderName} failed to get secret. Trying next provider...", provider.Name);
            }
        }

        logger.LogError("No valid client secret provider found. Application cannot start");
        throw new NoSecretProviderException();
    }

    protected virtual IApiCommunicationHandler CreateCommunicationHandler(HttpClient httpClient, IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider)
    {
        var communicationHandlerLogger = (LoggerFactory ?? new NullLoggerFactory()).CreateLogger<ApiCommunicationHandler>();
        return new ApiCommunicationHandler(
            ClientName,
            Instance,
            httpClient,
            communicationHandlerLogger,
            ipAddressResolver,
            clientSecretProvider);
    }

    protected virtual ISettingStatusMonitor CreateStatusMonitor(IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider, HttpClient httpClient)
    {
        var statusMonitorLogger = (LoggerFactory ?? new NullLoggerFactory()).CreateLogger<SettingStatusMonitor>();
        var statusMonitor = new SettingStatusMonitor(
            ipAddressResolver,
            new VersionProvider(this),
            new Diagnostics(),
            new SimpleHttpClientFactory(new Dictionary<string, HttpClient>()
            {
                { HttpClientNames.FigApi, httpClient }
            }),
            this,
            clientSecretProvider,
            statusMonitorLogger);

        return statusMonitor;
    }

    protected virtual HttpClient CreateHttpClient()
    {
        if (HttpClient is not null)
            return HttpClient;
        
        var clientFactoryLogger = (LoggerFactory ?? new NullLoggerFactory()).CreateLogger<ValidatedHttpClientFactory>();
        var factory = new ValidatedHttpClientFactory(clientFactoryLogger);
        
        return factory.CreateClient(ApiUris).GetAwaiter().GetResult();
    }

    private IOfflineSettingsManager CreateOfflineSettingsManager(IClientSecretProvider clientSecretProvider)
    {
        var offlineSettingsManagerLogger = (LoggerFactory ?? new NullLoggerFactory()).CreateLogger<OfflineSettingsManager>();
        return new OfflineSettingsManager(
            new Cryptography(),
            new BinaryFile(),
            clientSecretProvider,
            offlineSettingsManagerLogger);
    }    public void Dispose()
    {
        LoggerFactory?.Dispose();
        HttpClient?.Dispose();
    }
}