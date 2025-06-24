using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Client.Attributes;
using Fig.Client.Configuration;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.Enums;
using Fig.Client.EnvironmentVariables;
using Fig.Client.Exceptions;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client;

public abstract class SettingsBase
{
    private readonly IDescriptionProvider _descriptionProvider;
    private readonly IEnvironmentVariableReader _environmentVariableReader;
    private readonly ISettingDefinitionFactory _settingDefinitionFactory;

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

    public bool RestartRequested { get; set; }
    
    // Internal property to store ticks from configuration binding
    public long? LastFigUpdateUtcTicks { get; set; }
    
    // Public property that converts ticks to UTC DateTime
    public DateTime? LastFigUpdateUtc 
    { 
        get => LastFigUpdateUtcTicks.HasValue ? new DateTime(LastFigUpdateUtcTicks.Value, DateTimeKind.Utc) : null;
        set => LastFigUpdateUtcTicks = value?.Ticks;
    }
    
    public LoadType FigSettingLoadType { get; set; } = LoadType.None;

    public SettingsClientDefinitionDataContract CreateDataContract(string clientName)
    {
        var displayOrder = 1;
        var exceptions = new List<Exception>();
        var settings = GetSettingProperties(this)
            .Select(settingProperty =>
            {
                try
                {
                    return _settingDefinitionFactory.Create(settingProperty, displayOrder++);
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
            clientSettingOverrides);
    }

    public Dictionary<string, List<CustomConfigurationSection>> GetConfigurationSections()
    {
        return GetSettingProperties(this).ToDictionary(
            a => a.Name,
            b => _settingDefinitionFactory.GetConfigurationSections(b));
    }

    public abstract IEnumerable<string> GetValidationErrors();

    public virtual IEnumerable<string> GetValidationWarnings() => [];

    private string? GetInstance(string clientName)
    {
        var value = Environment.GetEnvironmentVariable($"{clientName.Replace(" ", "")}_INSTANCE");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    
    private IEnumerable<SettingDetails> GetSettingProperties(object parentInstance, string prefix = "")
    {
        var properties = parentInstance.GetType().GetProperties();
        var result = new List<SettingDetails>();

        foreach (var prop in properties)
        {
            var instance = prop.GetValue(parentInstance);

            if (Attribute.IsDefined(prop, typeof(SettingAttribute)))
            {
                var name = prop.Name;
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    name = $"{prefix.Replace(":", Constants.SettingPathSeparator)}{Constants.SettingPathSeparator}{name}";
                }
                result.Add(new SettingDetails(prefix, prop, instance, name, parentInstance));
            }
            else if (Attribute.IsDefined(prop, typeof(NestedSettingAttribute)))
            {
                instance ??= Activator.CreateInstance(prop.PropertyType);
                var propertyName = string.IsNullOrEmpty(prefix) ? prop.Name: $"{prefix}:{prop.Name}";

                // Recursively extract properties from the nested class instance
                var nestedProperties = GetSettingProperties(instance, propertyName);
                result.AddRange(nestedProperties);
            }
        }

        return result;
    }
}