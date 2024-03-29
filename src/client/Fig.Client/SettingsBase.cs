﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.Configuration;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.EnvironmentVariables;
using Fig.Client.Exceptions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Microsoft.Extensions.Logging;

namespace Fig.Client;

public abstract class SettingsBase
{
    private readonly IDescriptionProvider _descriptionProvider;
    private readonly IEnvironmentVariableReader _environmentVariableReader;
    private readonly ISettingDefinitionFactory _settingDefinitionFactory;
    private readonly List<string> _configurationErrors = new();

    protected SettingsBase()
        : this(new DescriptionProvider(new InternalResourceProvider(),
                new MarkdownExtractor()),
            new DataGridDefaultValueProvider(),
            new EnvironmentVariableReader())
    {
    }

    private SettingsBase(IDescriptionProvider descriptionProvider, IDataGridDefaultValueProvider dataGridDefaultValueProvider, IEnvironmentVariableReader environmentVariableReader)
        : this(new SettingDefinitionFactory(descriptionProvider, dataGridDefaultValueProvider),
            descriptionProvider,
            environmentVariableReader)
    {
        _descriptionProvider = descriptionProvider;
    }

    internal SettingsBase(ISettingDefinitionFactory settingDefinitionFactory,
        IDescriptionProvider descriptionProvider,
        IEnvironmentVariableReader environmentVariableReader)
    {
        _settingDefinitionFactory = settingDefinitionFactory;
        _descriptionProvider = descriptionProvider;
        _environmentVariableReader = environmentVariableReader;
    }

    public abstract string ClientDescription { get; }

    public bool HasConfigurationError { get; private set; }

    public bool RestartRequested { get; set; }

    public SettingsClientDefinitionDataContract CreateDataContract(string clientName)
    {
        var exceptions = new List<Exception>();
        var settings = GetSettingProperties()
            .Select(settingProperty =>
            {
                try
                {
                    return _settingDefinitionFactory.Create(settingProperty, this);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

                return null;
            }).Where(a => a is not null)
            .ToList();

        var clientSettingOverrides = _environmentVariableReader.ReadSettingOverrides(clientName, settings!);

        _environmentVariableReader.ApplyConfigurationOverrides(settings!);

        var description = _descriptionProvider.GetDescription(ClientDescription);
        if (string.IsNullOrWhiteSpace(description))
        {
            var validResourceKeys = _descriptionProvider.GetAllMarkdownResourceKeys();
            exceptions.Add(new InvalidSettingException($"Client is missing a description. " +
                                              $"Valid resource keys are: {string.Join(", ", validResourceKeys)}"));
        }

        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException("Errors found while processing Fig configuration", exceptions);
        }
        
        return new SettingsClientDefinitionDataContract(clientName,
            description,
            GetInstance(clientName),
            settings.Any(a => !string.IsNullOrEmpty(a?.DisplayScript)),
            settings!,
            GetVerifications(),
            clientSettingOverrides);
    }

    public Dictionary<string, CustomConfigurationSection> GetConfigurationSections()
    {
        return GetSettingProperties().ToDictionary(
            a => a.Name,
            b => _settingDefinitionFactory.GetConfigurationSection(b));
    }

    public abstract void Validate(ILogger logger);

    public void SetConfigurationErrorStatus(bool configurationError, List<string>? configurationErrors = null)
    {
        HasConfigurationError = configurationError;
        
        if (configurationErrors != null)
            _configurationErrors.AddRange(configurationErrors);
    }

    public List<string> GetConfigurationErrors()
    {
        var result = _configurationErrors.ToList();
        _configurationErrors.Clear();
        return result;
    }

    private string? GetInstance(string clientName)
    {
        var value = Environment.GetEnvironmentVariable($"{clientName.Replace(" ", "")}_INSTANCE");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private List<SettingVerificationDefinitionDataContract> GetVerifications()
    {
        var verificationAttributes = GetType()
            .GetCustomAttributes(typeof(VerificationAttribute), true)
            .Cast<VerificationAttribute>();

        return verificationAttributes.Select(attribute =>
            new SettingVerificationDefinitionDataContract(attribute.Name, attribute.Name,
                attribute.SettingNames.ToList())).ToList();
    }

    private IEnumerable<PropertyInfo> GetSettingProperties()
    {
        return GetType().GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));
    }
}