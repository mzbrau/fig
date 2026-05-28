using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fig.Client.Testing.Integration;

public static class FigSettingsBindingVerifier
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    public static void VerifyOptionsBound<TSettings, TValue>(
        IServiceProvider services,
        TSettings expected,
        Expression<Func<TSettings, TValue>> valueSelector,
        string? optionsName = null,
        IEqualityComparer<TValue>? comparer = null)
        where TSettings : SettingsBase
    {
        if (expected is null)
        {
            throw new ArgumentNullException(nameof(expected));
        }

        if (valueSelector is null)
        {
            throw new ArgumentNullException(nameof(valueSelector));
        }

        VerifyOptionsBound<TSettings, TValue>(
            services,
            valueSelector.Compile()(expected),
            valueSelector,
            optionsName,
            comparer);
    }

    public static void VerifyOptionsBound<TOptions, TValue>(
        IServiceProvider services,
        TValue expectedValue,
        Expression<Func<TOptions, TValue>> actualValueSelector,
        string? optionsName = null,
        IEqualityComparer<TValue>? comparer = null)
        where TOptions : class
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (actualValueSelector is null)
        {
            throw new ArgumentNullException(nameof(actualValueSelector));
        }

        var actualValue = GetBoundOptionsValue(services, actualValueSelector.Compile(), actualValueSelector.ToString(), optionsName);
        VerifyValuesMatch<TOptions, TValue>(expectedValue, actualValue, actualValueSelector.ToString(), optionsName, comparer);
    }

    public static Task VerifyOptionsMonitorReloadsAsync<TSettings, TValue>(
        IServiceProvider services,
        ConfigReloader<TSettings> reloader,
        TSettings settings,
        Action<TSettings> mutate,
        Expression<Func<TSettings, TValue>> valueSelector,
        string? optionsName = null,
        IEqualityComparer<TValue>? comparer = null,
        TimeSpan? timeout = null)
        where TSettings : SettingsBase
    {
        if (valueSelector is null)
        {
            throw new ArgumentNullException(nameof(valueSelector));
        }

        return VerifyOptionsMonitorReloadsAsync<TSettings, TSettings, TValue>(
            services,
            reloader,
            settings,
            mutate,
            valueSelector.Compile(),
            valueSelector,
            optionsName,
            comparer,
            timeout);
    }

    public static async Task VerifyOptionsMonitorReloadsAsync<TFigSettings, TOptions, TValue>(
        IServiceProvider services,
        ConfigReloader<TFigSettings> reloader,
        TFigSettings settings,
        Action<TFigSettings>    mutate,
        Func<TFigSettings, TValue> expectedValueSelector,
        Expression<Func<TOptions, TValue>> actualValueSelector,
        string? optionsName = null,
        IEqualityComparer<TValue>? comparer = null,
        TimeSpan? timeout = null)
        where TFigSettings : SettingsBase
        where TOptions : class
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (reloader is null)
        {
            throw new ArgumentNullException(nameof(reloader));
        }

        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (mutate is null)
        {
            throw new ArgumentNullException(nameof(mutate));
        }

        if (expectedValueSelector is null)
        {
            throw new ArgumentNullException(nameof(expectedValueSelector));
        }

        if (actualValueSelector is null)
        {
            throw new ArgumentNullException(nameof(actualValueSelector));
        }

        var timeoutValue = timeout ?? DefaultTimeout;
        if (timeoutValue < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout cannot be negative.");
        }

        var optionsMonitor = ResolveOptionsMonitor<TOptions>(services, actualValueSelector.ToString(), optionsName);
        var actualSelector = actualValueSelector.Compile();
        comparer ??= EqualityComparer<TValue>.Default;

        mutate(settings);
        var expectedValue = expectedValueSelector(settings);
        reloader.Reload(settings);

        var start = DateTime.UtcNow;
        TValue? actualValue;
        do
        {
            actualValue = actualSelector(optionsMonitor.Get(GetOptionsName(optionsName)));
            if (comparer.Equals(expectedValue, actualValue))
            {
                return;
            }

            if (timeoutValue == TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(PollInterval).ConfigureAwait(false);
        }
        while (DateTime.UtcNow - start < timeoutValue);

        throw new FigSettingsBindingVerificationException(
            typeof(TOptions),
            actualValueSelector.ToString(),
            expectedValue,
            actualValue,
            optionsName);
    }

    /// <summary>
    /// Automatically mutates all <see cref="SettingAttribute"/>-decorated properties on
    /// <typeparamref name="TSettings"/> (including nested settings), reloads the configuration,
    /// and verifies that <see cref="IOptionsMonitor{TSettings}"/> reflects ALL mutations.
    /// </summary>
    /// <remarks>
    /// Supports scalar types: string, bool, numeric, Guid, DateTime, DateTimeOffset, TimeSpan, and Enum,
    /// including their nullable equivalents. Collection and complex-object properties are skipped;
    /// use explicit mutation overloads for those.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no properties can be auto-mutated.</exception>
    /// <exception cref="FigSettingsBindingVerificationException">
    /// Thrown when one or more mutated properties are not reflected in the options monitor.
    /// </exception>
    public static async Task VerifyOptionsMonitorReloadsAsync<TSettings>(
        IServiceProvider services,
        ConfigReloader<TSettings> reloader,
        TSettings settings,
        string? optionsName = null,
        TimeSpan? timeout = null)
        where TSettings : SettingsBase
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (reloader is null) throw new ArgumentNullException(nameof(reloader));
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        var timeoutValue = timeout ?? DefaultTimeout;
        if (timeoutValue < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout cannot be negative.");

        var mutations = BuildMutations(settings);
        if (mutations.Count == 0)
        {
            throw new InvalidOperationException(
                $"No auto-mutable [Setting] properties were found on {typeof(TSettings).FullName}. " +
                "Only scalar properties (string, bool, numeric, Guid, DateTime, DateTimeOffset, TimeSpan, Enum) are auto-mutated. " +
                "Use an explicit mutation overload for collection or complex-object settings.");
        }

        ApplyMutations(settings, mutations);
        reloader.Reload(settings);

        var optionsMonitor = ResolveOptionsMonitor<TSettings>(services, "(auto)", optionsName);
        var optionsNameValue = GetOptionsName(optionsName);

        var start = DateTime.UtcNow;
        List<(string Selector, object? Expected, object? Actual)>? failures;
        do
        {
            var currentValue = optionsMonitor.Get(optionsNameValue);
            failures = GetFailures(currentValue, mutations);
            if (failures.Count == 0) return;

            if (timeoutValue == TimeSpan.Zero) break;
            await Task.Delay(PollInterval).ConfigureAwait(false);
        }
        while (DateTime.UtcNow - start < timeoutValue);

        throw new FigSettingsBindingVerificationException(typeof(TSettings), failures!, optionsName);
    }

    private static List<(string Selector, object? Expected, object? Actual)> GetFailures<TSettings>(
        TSettings currentValue,
        IReadOnlyList<MutationRecord> mutations)
        where TSettings : class
    {
        var failures = new List<(string, object?, object?)>();
        foreach (var mutation in mutations)
        {
            var actual = NavigatePath(currentValue, mutation.PropertyPath);
            if (!ValuesAreEqual(mutation.ExpectedValue, actual))
                failures.Add((mutation.DisplayPath, mutation.ExpectedValue, actual));
        }

        return failures;
    }

    private static IReadOnlyList<MutationRecord> BuildMutations<TSettings>(TSettings settings)
        where TSettings : SettingsBase
    {
        var result = new List<MutationRecord>();
        CollectMutations(settings, Array.Empty<PropertyInfo>(), string.Empty, result);
        return result;
    }

    private static void CollectMutations(
        object instance,
        PropertyInfo[] parentPath,
        string parentDisplayPath,
        List<MutationRecord> result)
    {
        foreach (var prop in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;

            var path = parentPath.Concat(new[] { prop }).ToArray();
            var displayPath = string.IsNullOrEmpty(parentDisplayPath)
                ? prop.Name
                : parentDisplayPath + "." + prop.Name;

            if (Attribute.IsDefined(prop, typeof(SettingAttribute)))
            {
                if (!prop.CanWrite) continue;
                var currentValue = prop.GetValue(instance);
                var (mutatedValue, canMutate) = GenerateMutatedValue(prop.PropertyType, currentValue);
                if (canMutate)
                    result.Add(new MutationRecord(path, mutatedValue, displayPath));
            }
            else if (Attribute.IsDefined(prop, typeof(NestedSettingAttribute)))
            {
                var nested = prop.GetValue(instance);
                if (nested is null)
                {
                    if (!prop.CanWrite) continue;
                    try
                    {
                        nested = Activator.CreateInstance(prop.PropertyType);
                        prop.SetValue(instance, nested);
                    }
                    catch
                    {
                        continue;
                    }
                }

                CollectMutations(nested, path, displayPath, result);
            }
        }
    }

    private static void ApplyMutations<TSettings>(TSettings settings, IReadOnlyList<MutationRecord> mutations)
        where TSettings : class
    {
        foreach (var mutation in mutations)
        {
            var parent = NavigatePath(settings, mutation.PropertyPath, upToLast: true);
            if (parent is null) continue;
            mutation.PropertyPath[mutation.PropertyPath.Length - 1].SetValue(parent, mutation.ExpectedValue);
        }
    }

    private static object? NavigatePath(object root, PropertyInfo[] path, bool upToLast = false)
    {
        object? current = root;
        var count = upToLast ? path.Length - 1 : path.Length;
        for (var i = 0; i < count; i++)
        {
            if (current is null) return null;
            current = path[i].GetValue(current);
        }

        return current;
    }

    private static (object? Value, bool CanMutate) GenerateMutatedValue(Type type, object? current)
    {
        if (type == typeof(string))
            return ((string?)current + "_fig_mutated", true);

        if (type == typeof(bool)) return (!(bool)(current ?? false), true);
        if (type == typeof(bool?)) return (current == null ? true : !(bool)current, true);

        if (type == typeof(int)) return ((int)(current ?? 0) + 1, true);
        if (type == typeof(int?)) return (current == null ? 1 : (int)current + 1, true);
        if (type == typeof(long)) return ((long)(current ?? 0L) + 1L, true);
        if (type == typeof(long?)) return (current == null ? 1L : (long)current + 1L, true);
        if (type == typeof(short)) return ((short)((short)(current ?? (short)0) + 1), true);
        if (type == typeof(short?)) return ((short)(current == null ? 1 : (short)current + 1), true);
        if (type == typeof(byte)) return ((byte)((byte)(current ?? (byte)0) + 1), true);
        if (type == typeof(byte?)) return ((byte)(current == null ? 1 : (byte)current + 1), true);
        if (type == typeof(uint)) return ((uint)(current ?? 0u) + 1u, true);
        if (type == typeof(uint?)) return (current == null ? 1u : (uint)current + 1u, true);
        if (type == typeof(ulong)) return ((ulong)(current ?? 0ul) + 1ul, true);
        if (type == typeof(ulong?)) return (current == null ? 1ul : (ulong)current + 1ul, true);
        if (type == typeof(double)) return ((double)(current ?? 0.0) + 1.0, true);
        if (type == typeof(double?)) return (current == null ? 1.0 : (double)current + 1.0, true);
        if (type == typeof(float)) return ((float)(current ?? 0f) + 1f, true);
        if (type == typeof(float?)) return ((float)(current == null ? 1f : (float)current + 1f), true);
        if (type == typeof(decimal)) return ((decimal)(current ?? 0m) + 1m, true);
        if (type == typeof(decimal?)) return (current == null ? 1m : (decimal)current + 1m, true);

        if (type == typeof(Guid) || type == typeof(Guid?))
            return (Guid.NewGuid(), true);

        if (type == typeof(DateTime))
            return (((DateTime)(current ?? DateTime.UtcNow)).AddDays(1), true);
        if (type == typeof(DateTime?))
            return (current == null ? DateTime.UtcNow.AddDays(1) : ((DateTime)current).AddDays(1), true);

        if (type == typeof(DateTimeOffset))
            return (((DateTimeOffset)(current ?? DateTimeOffset.UtcNow)).AddDays(1), true);
        if (type == typeof(DateTimeOffset?))
            return (current == null ? DateTimeOffset.UtcNow.AddDays(1) : ((DateTimeOffset)current).AddDays(1), true);

        if (type == typeof(TimeSpan))
            return (((TimeSpan)(current ?? TimeSpan.Zero)).Add(TimeSpan.FromSeconds(1)), true);
        if (type == typeof(TimeSpan?))
            return (current == null ? TimeSpan.FromSeconds(1) : ((TimeSpan)current).Add(TimeSpan.FromSeconds(1)), true);

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            if (values.Length == 0) return (null, false);
            var idx = current is null ? 0 : Array.IndexOf(values, current);
            return (values.GetValue((idx + 1) % values.Length), true);
        }

        var underlyingNullable = Nullable.GetUnderlyingType(type);
        if (underlyingNullable != null && underlyingNullable.IsEnum)
        {
            var values = Enum.GetValues(underlyingNullable);
            if (values.Length == 0) return (null, false);
            var idx = current is null ? 0 : Array.IndexOf(values, current);
            return (values.GetValue((idx + 1) % values.Length), true);
        }

        return (null, false);
    }

    private static bool ValuesAreEqual(object? expected, object? actual)
    {
        if (expected is null && actual is null) return true;
        if (expected is null || actual is null) return false;
        if (expected.Equals(actual)) return true;
        return JsonConvert.SerializeObject(expected) == JsonConvert.SerializeObject(actual);
    }

    private sealed class MutationRecord
    {
        public MutationRecord(PropertyInfo[] propertyPath, object? expectedValue, string displayPath)
        {
            PropertyPath = propertyPath;
            ExpectedValue = expectedValue;
            DisplayPath = displayPath;
        }

        public PropertyInfo[] PropertyPath { get; }
        public object? ExpectedValue { get; }
        public string DisplayPath { get; }
    }

    private static TValue GetBoundOptionsValue<TOptions, TValue>(
        IServiceProvider services,
        Func<TOptions, TValue> actualValueSelector,
        string selector,
        string? optionsName)
        where TOptions : class
    {
        if (!string.IsNullOrEmpty(optionsName))
        {
            var monitor = ResolveOptionsMonitor<TOptions>(services, selector, optionsName);
            return actualValueSelector(monitor.Get(GetOptionsName(optionsName)));
        }

        var options = services.GetService(typeof(IOptions<TOptions>)) as IOptions<TOptions>;
        if (options is not null)
        {
            return actualValueSelector(options.Value);
        }

        var optionsMonitor = ResolveOptionsMonitor<TOptions>(services, selector, optionsName);
        return actualValueSelector(optionsMonitor.CurrentValue);
    }

    private static IOptionsMonitor<TOptions> ResolveOptionsMonitor<TOptions>(
        IServiceProvider services,
        string selector,
        string? optionsName)
        where TOptions : class
    {
        var optionsMonitor = services.GetService(typeof(IOptionsMonitor<TOptions>)) as IOptionsMonitor<TOptions>;
        if (optionsMonitor is not null)
        {
            return optionsMonitor;
        }

        throw new FigSettingsBindingVerificationException(
            typeof(TOptions),
            selector,
            "No IOptionsMonitor<TOptions> service was registered.",
            optionsName);
    }

    private static void VerifyValuesMatch<TOptions, TValue>(
        TValue expectedValue,
        TValue actualValue,
        string selector,
        string? optionsName,
        IEqualityComparer<TValue>? comparer)
        where TOptions : class
    {
        comparer ??= EqualityComparer<TValue>.Default;
        if (!comparer.Equals(expectedValue, actualValue))
        {
            throw new FigSettingsBindingVerificationException(
                typeof(TOptions),
                selector,
                expectedValue,
                actualValue,
                optionsName);
        }
    }

    private static string GetOptionsName(string? optionsName)
    {
        return optionsName ?? Options.DefaultName;
    }
}
