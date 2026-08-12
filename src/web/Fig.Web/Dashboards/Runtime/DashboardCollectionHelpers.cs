using System.Collections;
using System.Globalization;

namespace Fig.Web.Dashboards.Runtime;

/// <summary>
/// Fluent array wrapper injected for <c>fig.clients</c> / <c>fig.runSessions</c>
/// so scripts can use <c>.groupBy(...).map(...)</c>.
/// </summary>
public sealed class DashboardJsArray : IEnumerable<object?>
{
    public static DashboardJsArray Empty { get; } = new(Array.Empty<object?>());

    private readonly List<object?> _items;

    public DashboardJsArray(IEnumerable<object?>? items)
    {
        _items = items?.ToList() ?? new List<object?>();
    }

    public int length => _items.Count;

    public int Count => _items.Count;

    public object? this[int index] => _items[index];

    public DashboardJsArray filter(Func<object?, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new DashboardJsArray(_items.Where(predicate));
    }

    public DashboardJsArray map(Func<object?, object?> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new DashboardJsArray(_items.Select(selector));
    }

    public DashboardJsArray groupBy(Func<object?, object?> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        var groups = _items
            .GroupBy(keySelector)
            .Select(g => (object?)new DashboardJsGroup
            {
                key = g.Key,
                items = new DashboardJsArray(g)
            });
        return new DashboardJsArray(groups);
    }

    public DashboardJsArray sort(Func<object?, object?>? keySelector = null)
    {
        if (keySelector is null)
        {
            return new DashboardJsArray(_items.OrderBy(i => i?.ToString(), StringComparer.OrdinalIgnoreCase));
        }

        return new DashboardJsArray(_items.OrderBy(i => Convert.ToString(keySelector(i), CultureInfo.InvariantCulture),
            StringComparer.OrdinalIgnoreCase));
    }

    public DashboardJsArray take(int count)
    {
        if (count <= 0)
            return Empty;
        return new DashboardJsArray(_items.Take(count));
    }

    public DashboardJsArray distinct(Func<object?, object?>? keySelector = null)
    {
        if (keySelector is null)
            return new DashboardJsArray(_items.Distinct());

        return new DashboardJsArray(_items.GroupBy(keySelector).Select(g => g.First()));
    }

    public int count(Func<object?, bool>? predicate = null)
    {
        return predicate is null ? _items.Count : _items.Count(predicate);
    }

    public double sum(Func<object?, object?>? selector = null)
    {
        return _items.Select(i => ToDouble(selector is null ? i : selector(i))).Sum();
    }

    public double average(Func<object?, object?>? selector = null)
    {
        if (_items.Count == 0)
            return 0;
        return sum(selector) / _items.Count;
    }

    public object? min(Func<object?, object?>? selector = null)
    {
        if (_items.Count == 0)
            return null;

        return _items
            .Select(i => selector is null ? i : selector(i))
            .OrderBy(i => Convert.ToString(i, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public object? max(Func<object?, object?>? selector = null)
    {
        if (_items.Count == 0)
            return null;

        return _items
            .Select(i => selector is null ? i : selector(i))
            .OrderByDescending(i => Convert.ToString(i, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public object? first(Func<object?, bool>? predicate = null)
    {
        return predicate is null ? _items.FirstOrDefault() : _items.FirstOrDefault(predicate);
    }

    public object? last(Func<object?, bool>? predicate = null)
    {
        return predicate is null ? _items.LastOrDefault() : _items.LastOrDefault(predicate);
    }

    public object?[] toArray() => _items.ToArray();

    public IEnumerator<object?> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static double ToDouble(object? value)
    {
        if (value is null)
            return 0;
        if (value is double d)
            return d;
        if (value is float f)
            return f;
        if (value is decimal m)
            return (double)m;
        if (value is int i)
            return i;
        if (value is long l)
            return l;
        if (value is short s)
            return s;
        if (double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return 0;
    }
}

public sealed class DashboardJsGroup
{
    public object? key { get; set; }

    public DashboardJsArray items { get; set; } = DashboardJsArray.Empty;
}

/// <summary>
/// Injectable as <c>helpers</c> / <c>DashboardJsLinq</c> for functional-style scripts.
/// </summary>
public sealed class DashboardJsLinq
{
    public DashboardJsArray from(object? source) => Wrap(source);

    public DashboardJsArray filter(object? source, Func<object?, bool> predicate) =>
        Wrap(source).filter(predicate);

    public DashboardJsArray map(object? source, Func<object?, object?> selector) =>
        Wrap(source).map(selector);

    public DashboardJsArray groupBy(object? source, Func<object?, object?> keySelector) =>
        Wrap(source).groupBy(keySelector);

    public DashboardJsArray sort(object? source, Func<object?, object?>? keySelector = null) =>
        Wrap(source).sort(keySelector);

    public DashboardJsArray take(object? source, int count) =>
        Wrap(source).take(count);

    public DashboardJsArray distinct(object? source, Func<object?, object?>? keySelector = null) =>
        Wrap(source).distinct(keySelector);

    public int count(object? source, Func<object?, bool>? predicate = null) =>
        Wrap(source).count(predicate);

    public double sum(object? source, Func<object?, object?>? selector = null) =>
        Wrap(source).sum(selector);

    public double average(object? source, Func<object?, object?>? selector = null) =>
        Wrap(source).average(selector);

    public object? min(object? source, Func<object?, object?>? selector = null) =>
        Wrap(source).min(selector);

    public object? max(object? source, Func<object?, object?>? selector = null) =>
        Wrap(source).max(selector);

    public object? first(object? source, Func<object?, bool>? predicate = null) =>
        Wrap(source).first(predicate);

    public object? last(object? source, Func<object?, bool>? predicate = null) =>
        Wrap(source).last(predicate);

    public static DashboardJsArray Wrap(object? source)
    {
        if (source is DashboardJsArray array)
            return array;

        if (source is IEnumerable<object?> typed)
            return new DashboardJsArray(typed);

        if (source is IEnumerable enumerable)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(item);
            return new DashboardJsArray(list);
        }

        if (source is null)
            return DashboardJsArray.Empty;

        return new DashboardJsArray(new[] { source });
    }
}

/// <summary>Static helpers for unit tests and non-Jint callers.</summary>
public static class DashboardCollectionHelpers
{
    public static DashboardJsArray Filter(object? source, Func<object?, bool> predicate) =>
        DashboardJsLinq.Wrap(source).filter(predicate);

    public static DashboardJsArray Map(object? source, Func<object?, object?> selector) =>
        DashboardJsLinq.Wrap(source).map(selector);

    public static DashboardJsArray GroupBy(object? source, Func<object?, object?> keySelector) =>
        DashboardJsLinq.Wrap(source).groupBy(keySelector);

    public static DashboardJsArray Sort(object? source, Func<object?, object?>? keySelector = null) =>
        DashboardJsLinq.Wrap(source).sort(keySelector);

    public static DashboardJsArray Take(object? source, int count) =>
        DashboardJsLinq.Wrap(source).take(count);

    public static DashboardJsArray Distinct(object? source, Func<object?, object?>? keySelector = null) =>
        DashboardJsLinq.Wrap(source).distinct(keySelector);

    public static int Count(object? source, Func<object?, bool>? predicate = null) =>
        DashboardJsLinq.Wrap(source).count(predicate);

    public static double Sum(object? source, Func<object?, object?>? selector = null) =>
        DashboardJsLinq.Wrap(source).sum(selector);

    public static double Average(object? source, Func<object?, object?>? selector = null) =>
        DashboardJsLinq.Wrap(source).average(selector);

    public static object? Min(object? source, Func<object?, object?>? selector = null) =>
        DashboardJsLinq.Wrap(source).min(selector);

    public static object? Max(object? source, Func<object?, object?>? selector = null) =>
        DashboardJsLinq.Wrap(source).max(selector);

    public static object? First(object? source, Func<object?, bool>? predicate = null) =>
        DashboardJsLinq.Wrap(source).first(predicate);

    public static object? Last(object? source, Func<object?, bool>? predicate = null) =>
        DashboardJsLinq.Wrap(source).last(predicate);
}
