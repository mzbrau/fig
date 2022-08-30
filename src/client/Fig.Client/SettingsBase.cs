using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Fig.Client.SettingVerification;
using Fig.Client.Utils;
using Fig.Common;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Newtonsoft.Json;

namespace Fig.Client;

public abstract class SettingsBase
{
    private readonly ISettingDefinitionFactory _settingDefinitionFactory;
    private readonly ISettingVerificationDecompiler _settingVerificationDecompiler;
    private readonly IIpAddressResolver _ipAddressResolver;
    private List<string> _configurationErrors = new();

    protected SettingsBase()
        : this(new SettingDefinitionFactory(), new SettingVerificationDecompiler(), new IpAddressResolver())
    {
    }

    protected SettingsBase(ISettingDefinitionFactory settingDefinitionFactory,
        ISettingVerificationDecompiler settingVerificationDecompiler,
        IIpAddressResolver ipAddressResolver)
    {
        _settingDefinitionFactory = settingDefinitionFactory;
        _settingVerificationDecompiler = settingVerificationDecompiler;
        _ipAddressResolver = ipAddressResolver;
    }

    public abstract string ClientName { get; }

    public bool SupportsRestart => RestartRequested != null;

    public bool HasConfigurationError { get; private set; } = false;

    public event EventHandler? SettingsChanged;

    public event EventHandler? RestartRequested;

    public void Initialize(IEnumerable<SettingDataContract>? settings)
    {
        if (settings != null)
            SetPropertiesFromSettings(settings.ToList());
        else
            SetPropertiesFromDefaultValues();
    }

    public void Update(IEnumerable<SettingDataContract> settings)
    {
        SetPropertiesFromSettings(settings.ToList());
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RequestRestart()
    {
        RestartRequested?.Invoke(this, EventArgs.Empty);
    }

    public SettingsClientDefinitionDataContract CreateDataContract()
    {
        var settings = GetSettingProperties()
            .Select(settingProperty => _settingDefinitionFactory.Create(settingProperty))
            .ToList();

        return new SettingsClientDefinitionDataContract(ClientName,
            GetInstance(),
            settings,
            GetPluginVerifications(),
            GetDynamicVerifications());
    }

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

    private string? GetInstance()
    {
        var value = Environment.GetEnvironmentVariable($"{ClientName.Replace(" ", "")}_INSTANCE");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private List<SettingDynamicVerificationDefinitionDataContract> GetDynamicVerifications()
    {
        var verificationAttributes = GetType()
            .GetCustomAttributes(typeof(VerificationAttribute), true)
            .Cast<VerificationAttribute>()
            .Where(v => v.VerificationType == VerificationType.Dynamic);

        var verifications = new List<SettingDynamicVerificationDefinitionDataContract>();
        foreach (var attribute in verificationAttributes.Where(a => a.ClassDoingVerification is not null))
        {
            var verificationClass = attribute.ClassDoingVerification;

            if (!verificationClass!.GetInterfaces().Contains(typeof(ISettingVerification)))
                throw new InvalidSettingVerificationException(
                    $"Verification class {verificationClass.Name} does not implement {nameof(ISettingVerification)}");

            var decompiledCode = _settingVerificationDecompiler.Decompile(verificationClass,
                nameof(ISettingVerification.PerformVerification));

            verifications.Add(new SettingDynamicVerificationDefinitionDataContract(
                attribute.Name,
                attribute.Description,
                decompiledCode,
                attribute.TargetRuntime,
                attribute.SettingNames.ToList()));
        }

        return verifications;
    }

    private List<SettingPluginVerificationDefinitionDataContract> GetPluginVerifications()
    {
        var verificationAttributes = GetType()
            .GetCustomAttributes(typeof(VerificationAttribute), true)
            .Cast<VerificationAttribute>()
            .Where(v => v.VerificationType == VerificationType.Plugin);

        return verificationAttributes.Select(attribute =>
            new SettingPluginVerificationDefinitionDataContract(attribute.Name, attribute.Description,
                attribute.SettingNames.ToList())).ToList();
    }

    private IEnumerable<PropertyInfo> GetSettingProperties()
    {
        return GetType().GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));
    }

    private void SetPropertiesFromDefaultValues()
    {
        foreach (var property in GetSettingProperties()) 
            SetDefaultValue(property);
    }

    private void SetDefaultValue(PropertyInfo property)
    {
        if (property.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType() == typeof(SettingAttribute)) is SettingAttribute settingAttribute)
            if (settingAttribute.DefaultValue != null)
                property.SetValue(this, property.PropertyType == typeof(SecureString)
                    ? settingAttribute.DefaultValue.ToString().ToSecureString()
                    : ReplaceConstants(settingAttribute.DefaultValue));
    }

    private object ReplaceConstants(object originalValue)
    {
        if (originalValue is string originalString)
        {
            return originalString
                .Replace(SettingConstants.MachineName, Environment.MachineName)
                .Replace(SettingConstants.User, Environment.UserName)
                .Replace(SettingConstants.Domain, Environment.UserDomainName)
                .Replace(SettingConstants.IpAddress, _ipAddressResolver.Resolve())
                .Replace(SettingConstants.ProcessorCount, $"{Environment.ProcessorCount}")
                .Replace(SettingConstants.OsVersion, Environment.OSVersion.VersionString);
        }

        return originalValue;
    }

    private void SetPropertiesFromSettings(List<SettingDataContract> settings)
    {
        foreach (var property in GetSettingProperties())
        {
            var definition = settings.FirstOrDefault(a => a.Name == property.Name);

            if (definition?.Value != null)
            {
                if (property.PropertyType.IsEnum)
                    SetEnumValue(property, this, definition.Value);
                else if (property.PropertyType.IsSecureString())
                    property.SetValue(this, ((string) definition.Value.ToString()).ToSecureString());
                else if (property.PropertyType.IsSupportedBaseType())
                    property.SetValue(this, ReplaceConstants(definition.Value));
                else if (property.PropertyType.IsSupportedDataGridType())
                    SetDataGridValue(property, definition.Value);
                else
                    SetJsonValue(property, definition.Value);
            }
            else
            {
                SetDefaultValue(property);
            }
        }
    }

    private void SetEnumValue(PropertyInfo property, object target, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var enumValue = Enum.Parse(property.PropertyType, value);
            property.SetValue(target, enumValue);
        }
    }

    private void SetDataGridValue(PropertyInfo property, List<Dictionary<string, object>> dataGridRows)
    {
        if (!ListUtilities.TryGetGenericListType(property.PropertyType, out var genericType))
            return;

        var list = (IList) Activator.CreateInstance(property.PropertyType);
        foreach (var dataGridRow in dataGridRows)
        {
            // If the row is a basic type, we don't need to create and populate it.
            // We just get the value and add it to the collection.
            if (genericType!.IsSupportedBaseType())
            {
                list.Add(ConvertToType(dataGridRow.Single().Value, genericType!));
                continue;
            }

            var listItem = Activator.CreateInstance(genericType!);

            foreach (var column in dataGridRow)
            {
                var prop = genericType!.GetProperty(column.Key);
                if (prop?.PropertyType == typeof(int) && column.Value is long longValue)
                    prop.SetValue(listItem, (int?) longValue);
                else if (prop?.PropertyType.IsEnum == true && column.Value is string strValue)
                    SetEnumValue(prop, listItem, strValue);
                else if (prop?.PropertyType == typeof(TimeSpan))
                    prop.SetValue(listItem, TimeSpan.Parse((string) column.Value));
                else
                    prop?.SetValue(listItem, ReplaceConstants(column.Value));
            }

            list.Add(listItem);
        }

        property.SetValue(this, list);
    }

    private object ConvertToType(object value, Type type)
    {
        if (value.GetType() == type)
            return value;

        return Convert.ChangeType(value, type);
    }

    private void SetJsonValue(PropertyInfo property, string value)
    {
        var deserializedValue = JsonConvert.DeserializeObject(value, property.PropertyType);
        property.SetValue(this, deserializedValue);
    }
}