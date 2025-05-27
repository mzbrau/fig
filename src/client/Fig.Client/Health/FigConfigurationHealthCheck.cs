using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Validation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Reflection;
using Fig.Client.Attributes;
using System.Collections.Generic;

namespace Fig.Client.Health;

public class FigConfigurationHealthCheck<T> : IHealthCheck where T : SettingsBase
{
    private readonly IOptionsMonitor<T> _settings;
    private static readonly ConcurrentDictionary<string, HealthCheckResult?> _cachedResult = new();
    private readonly string cacheKey = typeof(T).Name;

    public FigConfigurationHealthCheck(IOptionsMonitor<T> settings)
    {
        _settings = settings;

        _cachedResult.TryAdd(cacheKey, null);
        
        _settings.OnChange(a =>
        {
            _cachedResult[cacheKey] = null;
        });
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        if (_cachedResult.ContainsKey(cacheKey) && _cachedResult[cacheKey] is null && ValidationBridge.GetConfigurationErrors != null)
        {
            var errors = ValidationBridge.GetConfigurationErrors().ToList();
            var validationErrors = GetValidationErrorsRecursive(_settings.CurrentValue, typeof(T));

            if (validationErrors.Any())
                errors.AddRange(validationErrors);

            _cachedResult[cacheKey] = !errors.Any()
                ? HealthCheckResult.Healthy("Configuration is valid.")
                : HealthCheckResult.Unhealthy($"Configuration is invalid. {Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        return Task.FromResult(_cachedResult[cacheKey] ?? HealthCheckResult.Healthy("No configuration available."));
    }

    private static List<string> GetValidationErrorsRecursive(object? instance, Type objectType, string? parentPath = null)
    {
        var validationErrors = new List<string>();
        if (instance == null)
            return validationErrors;

        var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var classValidationAttributes = objectType.GetCustomAttributes(typeof(IValidatableAttribute), true)
            .Cast<IValidatableAttribute>()
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

    private static IValidatableAttribute? GetEffectiveValidationAttribute(PropertyInfo property, List<IValidatableAttribute> classValidationAttributes)
    {
        var propertyValidationAttributes = property.GetCustomAttributes(typeof(IValidatableAttribute), true)
            .Cast<IValidatableAttribute>()
            .ToList();

        if (propertyValidationAttributes.Count > 0)
        {
            return propertyValidationAttributes.FirstOrDefault();
        }

        return classValidationAttributes.FirstOrDefault(a =>
            a.ApplyToTypes != null &&
            a.ApplyToTypes.Any(t => t.IsAssignableFrom(property.PropertyType)));
    }

    private static string? ValidateProperty(PropertyInfo property, object instance, IValidatableAttribute effectiveAttribute, string propertyPath)
    {
        // Get value and validate
        var value = property.GetValue(instance);
        var (isValid, explanation) = effectiveAttribute.IsValid(value);

        return !isValid ? $"[{propertyPath}] {explanation}" : null;
    }
}