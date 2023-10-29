using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.EnvironmentVariables;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Microsoft.Extensions.Logging;

namespace Fig.Client;

public abstract class SettingsBase
{
    private readonly IDescriptionProvider _descriptionProvider;
    private readonly IEnvironmentVariableReader _environmentVariableReader;
    private readonly ISettingDefinitionFactory _settingDefinitionFactory;
    private readonly IIpAddressResolver _ipAddressResolver;
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
            new IpAddressResolver(),
            descriptionProvider,
            environmentVariableReader)
    {
        _descriptionProvider = descriptionProvider;
    }

    internal SettingsBase(ISettingDefinitionFactory settingDefinitionFactory,
        IIpAddressResolver ipAddressResolver,
        IDescriptionProvider descriptionProvider,
        IEnvironmentVariableReader environmentVariableReader)
    {
        _settingDefinitionFactory = settingDefinitionFactory;
        _ipAddressResolver = ipAddressResolver;
        _descriptionProvider = descriptionProvider;
        _environmentVariableReader = environmentVariableReader;
    }

    public abstract string ClientDescription { get; }

    public bool HasConfigurationError { get; private set; }

    public bool RestartRequested { get; set; }

    public SettingsClientDefinitionDataContract CreateDataContract(bool liveReload, string clientName)
    {
        var settings = GetSettingProperties()
            .Select(settingProperty => _settingDefinitionFactory.Create(settingProperty, liveReload, this))
            .ToList();

        var clientSettingOverrides = _environmentVariableReader.ReadSettingOverrides(clientName, settings);

        _environmentVariableReader.ApplyConfigurationOverrides(settings);
        
        return new SettingsClientDefinitionDataContract(clientName,
            _descriptionProvider.GetDescription(ClientDescription),
            GetInstance(clientName),
            settings,
            GetVerifications(),
            clientSettingOverrides);
    }

    public Dictionary<string, string> GetConfigurationSections()
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

    internal List<string> GetConfigurationErrors()
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