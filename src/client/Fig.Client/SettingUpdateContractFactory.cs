using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.DefaultValue;
using Fig.Client.Exceptions;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.Settings;
using Newtonsoft.Json;

namespace Fig.Client;

internal static class SettingUpdateContractFactory
{
    public static SettingDataContract Create<TSettings, TValue>(
        Expression<Func<TSettings, TValue>> expression,
        TValue value)
        where TSettings : SettingsBase
    {
        var properties = GetPropertyPath(expression).ToList();
        var settingProperty = properties.Last();
        var settingName = string.Join(Constants.SettingPathSeparator, properties.Select(a => a.Name));
        var isSecret = properties.Any(a => Attribute.IsDefined(a, typeof(SecretAttribute)));
        var valueContract = CreateValueContract(value, settingProperty.PropertyType);

        return new SettingDataContract(settingName, valueContract, isSecret);
    }

    private static SettingValueBaseDataContract? CreateValueContract(object? value, Type propertyType)
    {
        if (propertyType.IsEnum())
            return new StringSettingDataContract(value?.ToString());

        if (propertyType.IsSupportedDataGridType())
            return new DataGridSettingDataContract(DataGridValueConverter.Convert(value));

        if (propertyType.IsSupportedBaseType())
            return ValueDataContractFactory.CreateContract(value, propertyType);

        var json = value is null
            ? null
            : JsonConvert.SerializeObject(value, JsonSettings.FigMinimalUserFacing);
        return new JsonSettingDataContract(json);
    }

    private static IEnumerable<PropertyInfo> GetPropertyPath<TSettings, TValue>(
        Expression<Func<TSettings, TValue>> expression)
        where TSettings : SettingsBase
    {
        var body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unaryExpression)
            body = unaryExpression.Operand;

        var properties = new Stack<PropertyInfo>();
        while (body is MemberExpression memberExpression)
        {
            if (memberExpression.Member is not PropertyInfo propertyInfo)
                throw new ConfigurationException("Setting update expressions must reference properties.");

            properties.Push(propertyInfo);
            body = memberExpression.Expression!;
        }

        if (body is not ParameterExpression)
            throw new ConfigurationException("Setting update expressions must reference a setting property.");

        var propertyPath = properties.ToList();
        if (!propertyPath.Any())
            throw new ConfigurationException("Setting update expressions must reference a setting property.");

        foreach (var parentProperty in propertyPath.Take(propertyPath.Count - 1))
        {
            if (!Attribute.IsDefined(parentProperty, typeof(NestedSettingAttribute)))
            {
                throw new ConfigurationException(
                    $"Property '{parentProperty.Name}' must be marked with [NestedSetting] to be used in a nested Fig setting path.");
            }
        }

        var settingProperty = propertyPath.Last();
        if (!Attribute.IsDefined(settingProperty, typeof(SettingAttribute)))
        {
            throw new ConfigurationException(
                $"Property '{settingProperty.Name}' must be marked with [Setting] to be updated through Fig.");
        }

        return propertyPath;
    }
}
