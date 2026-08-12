using System.Collections;
using System.Reflection;
using Fig.Web.Dashboards.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Dashboards;

/// <summary>
/// Builds a read-only tree of dashboard JS API values for the editor data explorer.
/// Node names match the Jint script surface so they can be copied into scripts.
/// </summary>
public static class DashboardDataExplorerModel
{
    public static IReadOnlyList<DashboardDataExplorerNode> Build(
        DashboardFigRoot? fig,
        IReadOnlyDictionary<string, object?>? namedTransforms = null)
    {
        var roots = new List<DashboardDataExplorerNode>();

        var clients = EnumerateJsArray(fig?.clients);
        var runSessions = EnumerateJsArray(fig?.runSessions);
        var figChildren = new List<DashboardDataExplorerNode>
        {
            new(
                "clients",
                $"array ({clients.Count})",
                null,
                clients.Select((client, index) => ObjectOrValueNode($"[{index}]", client)).ToList()),
            new(
                "runSessions",
                $"array ({runSessions.Count})",
                null,
                runSessions.Select((session, index) => ObjectOrValueNode($"[{index}]", session)).ToList())
        };

        roots.Add(new DashboardDataExplorerNode("fig", "object", null, figChildren));

        if (namedTransforms is { Count: > 0 })
        {
            roots.Add(new DashboardDataExplorerNode(
                "transforms",
                $"object ({namedTransforms.Count})",
                null,
                namedTransforms
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => ValueNode(kv.Key, kv.Value))
                    .ToList()));
        }

        return roots;
    }

    public static string FormatValue(object? value)
    {
        if (value is null)
            return "null";

        if (value is string s)
            return s;

        if (value is bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

        if (value is DateTime dt)
            return dt.ToString("O");

        if (value is DateTimeOffset dto)
            return dto.ToString("O");

        if (value is JToken token)
            return token.ToString(Formatting.Indented);

        if (value is DashboardJsArray jsArray)
            return JsonConvert.SerializeObject(jsArray.toArray(), Formatting.Indented);

        if (value is IDictionary dictionary)
        {
            var obj = new JObject();
            foreach (DictionaryEntry entry in dictionary)
                obj[entry.Key?.ToString() ?? string.Empty] = entry.Value is null
                    ? JValue.CreateNull()
                    : JToken.FromObject(entry.Value);
            return obj.ToString(Formatting.Indented);
        }

        try
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    public static string TypeName(object? value)
    {
        if (value is null)
            return "null";

        if (value is DashboardJsArray)
            return "array";

        var type = value.GetType();
        if (type.IsPrimitive || value is string or decimal or DateTime or DateTimeOffset)
            return type.Name;

        if (value is IDictionary)
            return "object";

        if (value is IEnumerable and not string)
            return "array";

        return type.Name;
    }

    private static DashboardDataExplorerNode ObjectNode(string label, object? value)
    {
        if (value is null)
            return new DashboardDataExplorerNode(label, "null", "null", Array.Empty<DashboardDataExplorerNode>());

        var children = new List<DashboardDataExplorerNode>();
        foreach (var prop in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            object? propValue;
            try
            {
                propValue = prop.GetValue(value);
            }
            catch
            {
                continue;
            }

            children.Add(ValueNode(prop.Name, propValue));
        }

        return new DashboardDataExplorerNode(label, TypeName(value), null, children);
    }

    private static DashboardDataExplorerNode ValueNode(string name, object? value)
    {
        if (value is DashboardJsArray jsArray)
        {
            var items = EnumerateJsArray(jsArray);
            return new DashboardDataExplorerNode(
                name,
                $"array ({items.Count})",
                null,
                items.Select((item, index) => ObjectOrValueNode($"[{index}]", item)).ToList());
        }

        if (value is IDictionary dictionary)
        {
            var children = new List<DashboardDataExplorerNode>();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                children.Add(ObjectOrValueNode(key, entry.Value));
            }

            return new DashboardDataExplorerNode(name, $"object ({children.Count})", null, children);
        }

        if (value is not null and not string and IEnumerable enumerable and not JToken)
        {
            var list = enumerable.Cast<object?>().ToList();
            return new DashboardDataExplorerNode(
                name,
                $"array ({list.Count})",
                null,
                list.Select((item, index) => ObjectOrValueNode($"[{index}]", item)).ToList());
        }

        if (value is not null && !IsLeaf(value))
            return ObjectNode(name, value);

        return new DashboardDataExplorerNode(name, TypeName(value), FormatValue(value), Array.Empty<DashboardDataExplorerNode>());
    }

    private static DashboardDataExplorerNode ObjectOrValueNode(string label, object? value)
    {
        if (value is null || IsLeaf(value))
            return new DashboardDataExplorerNode(label, TypeName(value), FormatValue(value), Array.Empty<DashboardDataExplorerNode>());

        if (value is IDictionary or DashboardJsArray or (IEnumerable and not string))
            return ValueNode(label, value);

        return ObjectNode(label, value);
    }

    private static bool IsLeaf(object value) =>
        value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal or DateTime or DateTimeOffset or Guid or JValue;

    private static IReadOnlyList<object?> EnumerateJsArray(DashboardJsArray? array)
    {
        if (array is null)
            return Array.Empty<object?>();

        var items = new List<object?>();
        for (var i = 0; i < array.length; i++)
            items.Add(array[i]);
        return items;
    }
}

public sealed class DashboardDataExplorerNode
{
    public DashboardDataExplorerNode(
        string name,
        string type,
        string? value,
        IReadOnlyList<DashboardDataExplorerNode> children)
    {
        Name = name;
        Type = type;
        Value = value;
        Children = children;
    }

    public string Name { get; }

    public string Type { get; }

    public string? Value { get; }

    public IReadOnlyList<DashboardDataExplorerNode> Children { get; }

    public bool HasChildren => Children.Count > 0;
}
