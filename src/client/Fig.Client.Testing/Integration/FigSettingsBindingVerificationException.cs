using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Fig.Client.Testing.Integration;

public class FigSettingsBindingVerificationException : Exception
{
    private const string BindingHint =
        "Ensure the application registers options binding, for example services.Configure<TOptions>(configuration) or services.Configure<TOptions>(configuration.GetSection(...)).";

    public FigSettingsBindingVerificationException(
        Type optionsType,
        string selector,
        object? expectedValue,
        object? actualValue,
        string? optionsName = null)
        : base(BuildMessage(optionsType, selector, expectedValue, actualValue, optionsName))
    {
        OptionsType = optionsType;
        Selector = selector;
        ExpectedValue = expectedValue;
        ActualValue = actualValue;
        OptionsName = optionsName;
    }

    public FigSettingsBindingVerificationException(
        Type optionsType,
        string selector,
        string detail,
        string? optionsName = null)
        : base(BuildMessage(optionsType, selector, detail, optionsName))
    {
        OptionsType = optionsType;
        Selector = selector;
        OptionsName = optionsName;
    }

    public Type OptionsType { get; }

    public string Selector { get; }

    public object? ExpectedValue { get; }

    public object? ActualValue { get; }

    public string? OptionsName { get; }

    private static string BuildMessage(
        Type optionsType,
        string selector,
        object? expectedValue,
        object? actualValue,
        string? optionsName)
    {
        var optionNameMessage = string.IsNullOrEmpty(optionsName)
            ? "default options"
            : string.Format(CultureInfo.InvariantCulture, "named options '{0}'", optionsName);

        return string.Format(
            CultureInfo.InvariantCulture,
            "Fig settings binding verification failed for {0} ({1}) selector '{2}'. Expected {3}, but found {4}. {5}",
            optionsType.FullName,
            optionNameMessage,
            selector,
            FormatValue(expectedValue),
            FormatValue(actualValue),
            BindingHint);
    }

    public FigSettingsBindingVerificationException(
        Type optionsType,
        IReadOnlyList<(string Selector, object? Expected, object? Actual)> failures,
        string? optionsName = null)
        : base(BuildAggregateMessage(optionsType, failures, optionsName))
    {
        OptionsType = optionsType;
        Selector = string.Join(", ", failures.Select(f => f.Selector));
        OptionsName = optionsName;
    }

    private static string BuildMessage(Type optionsType, string selector, string detail, string? optionsName)
    {
        var optionNameMessage = string.IsNullOrEmpty(optionsName)
            ? "default options"
            : string.Format(CultureInfo.InvariantCulture, "named options '{0}'", optionsName);

        return string.Format(
            CultureInfo.InvariantCulture,
            "Fig settings binding verification failed for {0} ({1}) selector '{2}'. {3} {4}",
            optionsType.FullName,
            optionNameMessage,
            selector,
            detail,
            BindingHint);
    }

    private static string BuildAggregateMessage(
        Type optionsType,
        IReadOnlyList<(string Selector, object? Expected, object? Actual)> failures,
        string? optionsName)
    {
        var optionNameMessage = string.IsNullOrEmpty(optionsName)
            ? "default options"
            : string.Format(CultureInfo.InvariantCulture, "named options '{0}'", optionsName);

        var lines = new System.Text.StringBuilder();
        lines.AppendFormat(
            CultureInfo.InvariantCulture,
            "Fig settings binding verification failed for {0} ({1}). {2} properties did not reload correctly:{3}",
            optionsType.FullName,
            optionNameMessage,
            failures.Count,
            Environment.NewLine);

        foreach (var (selector, expected, actual) in failures)
        {
            lines.AppendFormat(
                CultureInfo.InvariantCulture,
                "  - '{0}': expected {1}, but found {2}{3}",
                selector,
                FormatValue(expected),
                FormatValue(actual),
                Environment.NewLine);
        }

        lines.Append(BindingHint);
        return lines.ToString();
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is string)
        {
            return JsonConvert.SerializeObject(value);
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || value is TimeSpan || type.IsEnum)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return JsonConvert.SerializeObject(value);
    }
}
