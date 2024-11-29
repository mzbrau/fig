using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text.Json;
using System;
using System.Globalization;
using System.Linq;

namespace Fig.Client.Parsers;

public class JsonValueParser
{
    private readonly IDictionary<string, string?> _data = new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _context = new();
    private string? _currentPath;

    public IDictionary<string, string?> ParseJsonValue(string value)
    {
        _data.Clear();

        var jsonDocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        using var doc = JsonDocument.Parse(value, jsonDocumentOptions);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException($"Unsupported JSON token '{doc.RootElement.ValueKind}' was found.");
        }
        ParseElement(doc.RootElement);

        return _data;
    }

    private void ParseElement(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            EnterContext(property.Name);
            ParseValue(property.Value);
            ExitContext();
        }
    }

    private void ParseValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                ParseElement(value);
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var arrayElement in value.EnumerateArray())
                {
                    EnterContext(index.ToString());
                    ParseValue(arrayElement);
                    ExitContext();
                    index++;
                }
                break;

            case JsonValueKind.Number:
            case JsonValueKind.String:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                var key = _currentPath;
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Key was null");
                }
                
                if (_data.ContainsKey(key!))
                {
                    throw new FormatException($"A duplicate key '{key}' was found.");
                }
                _data[key!] = Convert.ToString(value, CultureInfo.InvariantCulture);
                break;

            default:
                throw new FormatException($"Unsupported JSON token '{value.ValueKind}' was found.");
        }
    }

    private void EnterContext(string context)
    {
        _context.Push(context);
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    private void ExitContext()
    {
        _context.Pop();
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }
}