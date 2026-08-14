using System.Collections;
using System.Globalization;
using Fig.Web.Dashboards.Components.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen.Blazor;

namespace Fig.Web.Dashboards.Components;

/// <summary>
/// Converts raw Jint/transform results into strongly typed component input models.
/// Uses Newtonsoft (not System.Text.Json).
/// </summary>
public static class DashboardComponentDataBinder
{
    public static DashboardKpiInput ToKpi(object? data)
    {
        if (data is null)
            return new DashboardKpiInput();

        if (data is DashboardKpiInput typed)
            return typed;

        if (IsPrimitive(data))
            return new DashboardKpiInput { Value = data };

        var obj = ToJObject(data);
        return new DashboardKpiInput
        {
            Value = ToClr(obj["value"] ?? obj["Value"]),
            Label = obj.Value<string>("label") ?? obj.Value<string>("Label"),
            Trend = ToClr(obj["trend"] ?? obj["Trend"]),
            Variant = obj.Value<string>("variant") ?? obj.Value<string>("Variant"),
            Numerator = ToClr(obj["numerator"] ?? obj["Numerator"]),
            Denominator = ToClr(obj["denominator"] ?? obj["Denominator"]),
            Subtitle = obj.Value<string>("subtitle") ?? obj.Value<string>("Subtitle"),
            Icon = obj.Value<string>("icon") ?? obj.Value<string>("Icon")
        };
    }

    public static DashboardCardsInput ToCards(object? data)
    {
        if (data is DashboardCardsInput typed)
            return typed;

        var cards = new List<DashboardCardItem>();
        var token = ToJToken(data);
        if (token is not JArray array)
            return new DashboardCardsInput { Cards = cards };

        foreach (var item in array.OfType<JObject>())
        {
            var rows = new List<DashboardCardRow>();
            var rowsToken = item["rows"] ?? item["Rows"];
            if (rowsToken is JArray rowsArray)
            {
                foreach (var row in rowsArray.OfType<JObject>())
                {
                    rows.Add(new DashboardCardRow
                    {
                        Key = row.Value<string>("key") ?? row.Value<string>("Key"),
                        Value = ToClr(row["value"] ?? row["Value"])
                    });
                }
            }

            cards.Add(new DashboardCardItem
            {
                Title = item.Value<string>("title") ?? item.Value<string>("Title"),
                Value = ToClr(item["value"] ?? item["Value"]),
                Variant = item.Value<string>("variant") ?? item.Value<string>("Variant"),
                Icon = item.Value<string>("icon") ?? item.Value<string>("Icon"),
                Rows = rows
            });
        }

        return new DashboardCardsInput { Cards = cards };
    }

    public static DashboardTextInput ToText(object? data)
    {
        if (data is null)
            return new DashboardTextInput();

        if (data is DashboardTextInput typed)
            return NormalizeText(typed);

        if (data is string s)
            return NormalizeText(new DashboardTextInput
            {
                Lines = [new DashboardTextLine { Text = s, Size = "md" }]
            });

        var token = ToJToken(data);
        if (token is JArray array)
        {
            return NormalizeText(new DashboardTextInput
            {
                Lines = array.OfType<JObject>().Select(ParseTextLine).ToList()
            });
        }

        var obj = token as JObject ?? new JObject();
        var linesToken = obj["lines"] ?? obj["Lines"];
        if (linesToken is JArray linesArray)
        {
            return NormalizeText(new DashboardTextInput
            {
                Lines = linesArray.OfType<JObject>().Select(ParseTextLine).ToList()
            });
        }

        var text = obj.Value<string>("text") ?? obj.Value<string>("Text");
        var variant = obj.Value<string>("variant") ?? obj.Value<string>("Variant");
        if (text is not null || variant is not null)
        {
            return NormalizeText(new DashboardTextInput
            {
                Lines =
                [
                    new DashboardTextLine
                    {
                        Text = text ?? data.ToString(),
                        Size = MapVariantToSize(variant)
                    }
                ]
            });
        }

        // Single line object without wrapping { lines: ... }
        if (obj["text"] is null && obj["Text"] is null && obj.Properties().Any())
        {
            // fall through to string representation
        }

        return NormalizeText(new DashboardTextInput
        {
            Lines = [new DashboardTextLine { Text = data.ToString(), Size = "md" }]
        });
    }

    private static DashboardTextLine ParseTextLine(JObject item)
    {
        return new DashboardTextLine
        {
            Text = item.Value<string>("text") ?? item.Value<string>("Text"),
            Size = item.Value<string>("size") ?? item.Value<string>("Size"),
            Color = item.Value<string>("color") ?? item.Value<string>("Color"),
            Align = item.Value<string>("align") ?? item.Value<string>("Align"),
            Weight = item.Value<string>("weight") ?? item.Value<string>("Weight")
        };
    }

    private static string MapVariantToSize(string? variant)
    {
        return (variant ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "heading" => "xl",
            "muted" => "sm",
            _ => "md"
        };
    }

    private static DashboardTextInput NormalizeText(DashboardTextInput input)
    {
        if (input.Lines.Count == 0 && !string.IsNullOrWhiteSpace(input.Text))
        {
            input.Lines =
            [
                new DashboardTextLine
                {
                    Text = input.Text,
                    Size = MapVariantToSize(input.Variant)
                }
            ];
        }

        return input;
    }

    public static DashboardBadgeInput ToBadge(object? data)
    {
        if (data is null)
            return new DashboardBadgeInput();

        if (data is string s)
            return new DashboardBadgeInput { Text = s };

        if (data is DashboardBadgeInput typed)
            return typed;

        var obj = ToJObject(data);
        return new DashboardBadgeInput
        {
            Text = obj.Value<string>("text") ?? obj.Value<string>("Text") ?? data.ToString(),
            Variant = obj.Value<string>("variant") ?? obj.Value<string>("Variant") ?? "normal"
        };
    }

    public static IReadOnlyList<DashboardChartPoint> ToChartPoints(object? data)
    {
        if (data is null)
            return Array.Empty<DashboardChartPoint>();

        if (data is IEnumerable<DashboardChartPoint> typed)
            return typed.ToList();

        var token = ToJToken(data);
        if (token is not JArray array)
            return Array.Empty<DashboardChartPoint>();

        return array.Select(item =>
        {
            if (item is not JObject obj)
                return new DashboardChartPoint { Label = item.ToString(), Value = 0 };

            return new DashboardChartPoint
            {
                Label = obj.Value<string>("label") ?? obj.Value<string>("Label") ?? string.Empty,
                Value = obj.Value<double?>("value") ?? obj.Value<double?>("Value") ?? 0
            };
        }).ToList();
    }

    public static DashboardTableInput ToTable(object? data, JObject? config)
    {
        var columns = ReadColumns(config);
        var rows = new List<IDictionary<string, object?>>();

        var token = ToJToken(data);
        if (token is JArray array)
        {
            foreach (var item in array)
            {
                if (item is JObject obj)
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in obj.Properties())
                        row[prop.Name] = ToClr(prop.Value);
                    rows.Add(row);
                }
            }
        }

        if (columns.Count == 0 && rows.Count > 0)
        {
            columns = rows[0].Keys
                .Select(k => new DashboardTableColumn { Property = k, Header = k })
                .ToList();
        }

        return new DashboardTableInput { Rows = rows, Columns = columns };
    }

    public static DashboardListInput ToList(object? data)
    {
        var items = new List<DashboardListItem>();
        var token = ToJToken(data);
        if (token is JArray array)
        {
            foreach (var item in array)
            {
                if (item is JValue value)
                {
                    items.Add(new DashboardListItem { Text = value.ToString() });
                    continue;
                }

                if (item is JObject obj)
                {
                    items.Add(new DashboardListItem
                    {
                        Text = obj.Value<string>("text") ?? obj.Value<string>("Text") ??
                                obj.Value<string>("name") ?? obj.Value<string>("Name") ?? obj.ToString(),
                        Secondary = obj.Value<string>("secondary") ?? obj.Value<string>("Secondary"),
                        Variant = obj.Value<string>("variant") ?? obj.Value<string>("Variant")
                    });
                }
            }
        }

        return new DashboardListInput { Items = items };
    }

    private static readonly HashSet<string> KeyValueReservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "statusIcon", "statusColor", "items"
    };

    public static DashboardKeyValueInput ToKeyValue(object? data, JObject? config = null)
    {
        var items = new List<DashboardKeyValueItem>();
        string? statusIcon = null;
        string? statusColor = null;
        var token = ToJToken(data);

        if (token is JArray array)
        {
            foreach (var item in array.OfType<JObject>())
            {
                items.Add(new DashboardKeyValueItem
                {
                    Key = item.Value<string>("key") ?? item.Value<string>("Key"),
                    Value = ToClr(item["value"] ?? item["Value"])
                });
            }
        }
        else if (token is JObject obj)
        {
            statusIcon = obj.Value<string>("statusIcon") ?? obj.Value<string>("StatusIcon");
            statusColor = obj.Value<string>("statusColor") ?? obj.Value<string>("StatusColor");

            var itemsToken = obj["items"] ?? obj["Items"];
            if (itemsToken is JArray itemsArray)
            {
                foreach (var item in itemsArray.OfType<JObject>())
                {
                    items.Add(new DashboardKeyValueItem
                    {
                        Key = item.Value<string>("key") ?? item.Value<string>("Key"),
                        Value = ToClr(item["value"] ?? item["Value"])
                    });
                }
            }
            else
            {
                foreach (var prop in obj.Properties())
                {
                    if (KeyValueReservedKeys.Contains(prop.Name))
                        continue;

                    items.Add(new DashboardKeyValueItem { Key = prop.Name, Value = ToClr(prop.Value) });
                }
            }
        }

        if (string.IsNullOrWhiteSpace(statusIcon))
            statusIcon = config?["statusIcon"]?.ToString() ?? config?["StatusIcon"]?.ToString();
        if (string.IsNullOrWhiteSpace(statusColor))
            statusColor = config?["statusColor"]?.ToString() ?? config?["StatusColor"]?.ToString();

        return new DashboardKeyValueInput
        {
            Items = items,
            StatusIcon = string.IsNullOrWhiteSpace(statusIcon) ? null : statusIcon.Trim(),
            StatusColor = string.IsNullOrWhiteSpace(statusColor) ? null : statusColor.Trim()
        };
    }

    public static LegendPosition ReadLegendPosition(JObject? config)
    {
        var value = config?["legendPosition"]?.ToString() ?? config?["LegendPosition"]?.ToString();
        return string.Equals(value, "bottom", StringComparison.OrdinalIgnoreCase)
            ? LegendPosition.Bottom
            : LegendPosition.Right;
    }

    public static bool ReadLegendVisible(JObject? config)
    {
        var value = config?["legendPosition"]?.ToString() ?? config?["LegendPosition"]?.ToString();
        return !string.Equals(value, "hidden", StringComparison.OrdinalIgnoreCase);
    }

    public static string ReadLegendPositionCss(JObject? config)
    {
        var value = config?["legendPosition"]?.ToString() ?? config?["LegendPosition"]?.ToString();
        if (string.Equals(value, "hidden", StringComparison.OrdinalIgnoreCase))
            return "hidden";
        return string.Equals(value, "bottom", StringComparison.OrdinalIgnoreCase) ? "bottom" : "right";
    }

    /// <summary>Returns <c>compact</c>, <c>wide</c>, or <c>extraWide</c> for cards layout style.</summary>
    public static string ReadCardStyle(JObject? config)
    {
        var value = config?["cardStyle"]?.ToString() ?? config?["CardStyle"]?.ToString();
        if (string.Equals(value, "extraWide", StringComparison.OrdinalIgnoreCase))
            return "extraWide";
        if (string.Equals(value, "wide", StringComparison.OrdinalIgnoreCase))
            return "wide";
        return "compact";
    }

    /// <summary>Returns <c>large</c> or <c>small</c> for donut chart size.</summary>
    public static string ReadChartSize(JObject? config)
    {
        var value = config?["chartSize"]?.ToString() ?? config?["ChartSize"]?.ToString();
        if (string.Equals(value, "small", StringComparison.OrdinalIgnoreCase))
            return "small";
        return "large";
    }

    private static IReadOnlyList<DashboardTableColumn> ReadColumns(JObject? config)
    {
        if (config is null)
            return Array.Empty<DashboardTableColumn>();

        var columnsToken = config["columns"] ?? config["Columns"];
        if (columnsToken is not JArray array)
            return Array.Empty<DashboardTableColumn>();

        return array.OfType<JObject>().Select(c => new DashboardTableColumn
        {
            Property = c.Value<string>("property") ?? c.Value<string>("Property") ?? string.Empty,
            Header = c.Value<string>("header") ?? c.Value<string>("Header"),
            Align = c.Value<string>("align") ?? c.Value<string>("Align")
        }).Where(c => !string.IsNullOrWhiteSpace(c.Property)).ToList();
    }

    private static JObject ToJObject(object data)
    {
        var token = ToJToken(data);
        return token as JObject ?? new JObject();
    }

    private static JToken ToJToken(object? data)
    {
        if (data is null)
            return JValue.CreateNull();

        if (data is JToken token)
            return token;

        if (data is string s)
        {
            try
            {
                return JToken.Parse(s);
            }
            catch
            {
                return new JValue(s);
            }
        }

        // Jint object literals become ExpandoObject under .NET 10, which implements
        // IDictionary<string, object> but NOT non-generic IDictionary. Handle generic
        // dictionaries before IEnumerable (ExpandoObject enumerates as KeyValuePairs).
        if (data is IDictionary<string, object?> stringObjectDict)
            return DictionaryToJObject(stringObjectDict);

        if (data is KeyValuePair<string, object?> stringObjectKvp)
        {
            return new JObject
            {
                [stringObjectKvp.Key] = stringObjectKvp.Value is null
                    ? JValue.CreateNull()
                    : ToJToken(stringObjectKvp.Value)
            };
        }

        if (data is IDictionary dictionary)
        {
            var obj = new JObject();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                obj[key] = entry.Value is null ? JValue.CreateNull() : ToJToken(entry.Value);
            }

            return obj;
        }

        if (data is IEnumerable enumerable and not string and not IDictionary
            and not IDictionary<string, object?>)
        {
            var array = new JArray();
            foreach (var item in enumerable)
                array.Add(item is null ? JValue.CreateNull() : ToJToken(item));
            return array;
        }

        return JToken.FromObject(data);
    }

    private static JObject DictionaryToJObject(IEnumerable<KeyValuePair<string, object?>> entries)
    {
        var obj = new JObject();
        foreach (var (key, value) in entries)
            obj[key] = value is null ? JValue.CreateNull() : ToJToken(value);
        return obj;
    }

    private static object? ToClr(JToken? token)
    {
        if (token is null || token.Type == JTokenType.Null)
            return null;

        return token.Type switch
        {
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => token.Value<double>(),
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.String => token.Value<string>(),
            _ => token.ToString(Formatting.None)
        };
    }

    private static bool IsPrimitive(object data) =>
        data is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal or Guid or DateTime or DateTimeOffset;
}
