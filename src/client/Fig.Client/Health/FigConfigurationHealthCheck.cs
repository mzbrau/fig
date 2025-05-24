using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Validation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.RegularExpressions;
using Fig.Client.Attributes;
using Fig.Client.ExtensionMethods;
using System.Collections.Generic;

namespace Fig.Client.Health;

public class FigConfigurationHealthCheck<T> : IHealthCheck where T : SettingsBase
{
    private readonly IOptionsMonitor<T> _settings;
    private HealthCheckResult? _cachedResult;
    
    public FigConfigurationHealthCheck(IOptionsMonitor<T> settings)
    {
        _settings = settings;
        _settings.OnChange(a =>
        {
            _cachedResult = null;
        });
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        if (_cachedResult is null && ValidationBridge.GetConfigurationErrors != null)
        {
            var errors = ValidationBridge.GetConfigurationErrors().ToList();
            var validationErrors = GetValidationErrorsRecursive(_settings.CurrentValue, typeof(T));

            if (validationErrors.Any())
                errors.AddRange(validationErrors);

            _cachedResult = !errors.Any()
                ? HealthCheckResult.Healthy("Configuration is valid.")
                : HealthCheckResult.Unhealthy($"Configuration is invalid. {string.Join(", ", errors)}");
        }

        return Task.FromResult(_cachedResult ?? HealthCheckResult.Healthy("No configuration available."));
    }

    private static List<string> GetValidationErrorsRecursive(object? instance, Type objectType, string? parentPath = null)
    {
        var validationErrors = new List<string>();
        if (instance == null)
            return validationErrors;

        var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var classValidationAttributes = objectType.GetCustomAttributes(typeof(ValidationAttribute), true)
            .Cast<ValidationAttribute>()
            .ToList();

        foreach (var property in properties)
        {
            var propertyPath = parentPath == null ? property.Name : $"{parentPath}.{property.Name}";

            // Recursively process nested settings
            var nestedSettingAttr = property.GetCustomAttribute<NestedSettingAttribute>(true);
            if (nestedSettingAttr != null)
            {
                var nestedValue = property.GetValue(instance);
                if (nestedValue != null)
                {
                    validationErrors.AddRange(GetValidationErrorsRecursive(nestedValue, property.PropertyType, propertyPath));
                }
                continue;
            }

            var effectiveAttribute = GetEffectiveValidationAttribute(property, classValidationAttributes);
            if (effectiveAttribute == null)
                continue;

            var error = ValidateProperty(property, instance, effectiveAttribute, propertyPath);
            if (error != null)
                validationErrors.Add(error);
        }
        return validationErrors;
    }

    private static ValidationAttribute? GetEffectiveValidationAttribute(PropertyInfo property, List<ValidationAttribute> classValidationAttributes)
    {
        var propertyValidationAttributes = property.GetCustomAttributes(typeof(ValidationAttribute), true)
            .Cast<ValidationAttribute>()
            .ToList();

        if (propertyValidationAttributes.Count > 0)
        {
            return propertyValidationAttributes.FirstOrDefault(a => a.IncludeInHealthCheck && a.ValidationType != ValidationType.None);
        }
        else
        {
            return classValidationAttributes.FirstOrDefault(a =>
                a.IncludeInHealthCheck &&
                a.ValidationType != ValidationType.None &&
                a.ApplyToTypes != null &&
                a.ApplyToTypes.Any(t => t.IsAssignableFrom(property.PropertyType)));
        }
    }

    private static string? ValidateProperty(PropertyInfo property, object instance, ValidationAttribute effectiveAttribute, string propertyPath)
    {
        // Get regex and explanation
        string? regex = effectiveAttribute.ValidationRegex;
        string? explanation = effectiveAttribute.Explanation;
        if (effectiveAttribute.ValidationType != ValidationType.Custom)
        {
            var def = effectiveAttribute.ValidationType.GetDefinition();
            regex ??= def.Regex;
            explanation ??= def.Explanation;
        }

        if (string.IsNullOrWhiteSpace(regex))
            return null;

        // Get value and validate
        var value = property.GetValue(instance);
        string? valueString = value?.ToString();
        if (valueString != null && !Regex.IsMatch(valueString, regex))
        {
            return $"[{propertyPath}] {explanation}";
        }
        return null;
    }
}