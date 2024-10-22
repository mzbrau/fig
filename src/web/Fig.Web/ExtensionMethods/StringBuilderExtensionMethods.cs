using System.Globalization;
using System.Reflection;
using System.Text;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.ImportExport;
using Fig.Web.Attributes;

namespace Fig.Web.ExtensionMethods;

public static class StringBuilderExtensionMethods
{
    public static void AddHeading(this StringBuilder builder, int headingLevel, string text)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (headingLevel < 1 || headingLevel > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(headingLevel), "Heading level must be between 1 and 6.");
        }

        string heading = new string('#', headingLevel) + " " + text;
        builder.AppendLine(heading);
        builder.AppendLine();
    }
    
    public static void AddParagraph(this StringBuilder builder, string text)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return; // Skip empty paragraphs.
        }

        builder.AppendLine(text);
        builder.AppendLine(); // Add a blank line after the paragraph.
    }
    
    public static void AddProperty(this StringBuilder builder, string key, string? value)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return; // Skip empty key-value pairs.
        }

        builder.AppendLine($"**{key}:** {value}");
    }

    public static void AddPropertyValue(this StringBuilder builder, SettingExportDataContract setting, bool defaultVal)
    {
        var propertyType = setting.ValueType.FigPropertyType();
        if (propertyType == FigPropertyType.DataGrid)
        {
            builder.AddHeading(5, defaultVal ? "Default" : "Value");
            var value = defaultVal
                ? setting.DefaultValue?.GetValue() as List<Dictionary<string, object?>>
                : setting.Value?.GetValue() as List<Dictionary<string, object?>>;
            builder.AddTable(value);
        }
        else
        {
            var value = defaultVal
                ? Convert.ToString(setting.DefaultValue?.GetValue(),CultureInfo.InvariantCulture)
                : Convert.ToString(setting.Value?.GetValue(), CultureInfo.InvariantCulture);
            builder.AddProperty(defaultVal ? "Default" : "Value", value);
        }
    }

    public static void AddLine(this StringBuilder builder)
    {
        builder.AppendLine("---");
        builder.AppendLine();
    }

    public static void AddTable(this StringBuilder builder, List<Dictionary<string, object?>>? value)
    {
        if (value is null || value.Count == 0)
            return;

        var headings = value.First().Keys;
        var headerRow = new StringBuilder("|");
        foreach (var heading in headings)
        {
            headerRow.Append($" {heading} |");
        }
        builder.AppendLine(headerRow.ToString());
        
        var separatorRow = new StringBuilder("|");
        foreach (var _ in headings)
        {
            separatorRow.Append(" --- |");
        }
        builder.AppendLine(separatorRow.ToString());

        foreach (var row in value)
        {
            var rowBuilder = new StringBuilder("|");
            foreach (var columnValue in row)
            {
                rowBuilder.Append($" {columnValue.Value} |");
            }

            builder.AppendLine(rowBuilder.ToString());
        }

        builder.AppendLine();
    }

    public static void AddLink(this StringBuilder builder, string name)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return; // Skip empty names
        }

        builder.AppendLine($"- [{name}](#{name.Replace(" ", "")})");
    }

    public static void AddAnchor(this StringBuilder builder, string name)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return; // Skip empty names
        }

        builder.AppendLine($"<a name=\"{name.Replace(" ", "")}\"/>");
    }
    
    public static void AddTable<T>(this StringBuilder builder, List<T> items)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (items == null || items.Count == 0)
        {
            return; // Skip empty tables.
        }

        // Get the properties of type T with the [Order] attribute and order them accordingly.
        var properties = typeof(T)
            .GetProperties()
            .Where(prop => prop.GetCustomAttribute<OrderAttribute>() != null)
            .OrderBy(prop => prop.GetCustomAttribute<OrderAttribute>()!.Value)
            .ToList();

        if (properties.Count == 0)
        {
            throw new InvalidOperationException("No properties with [Order] attribute found.");
        }

        // Get the property marked with [Sort] attribute, if any.
        var sortProperty = properties.FirstOrDefault(prop => prop.GetCustomAttribute<SortAttribute>() != null);

        // Create the table header row based on ordered properties.
        var headerRow = new StringBuilder("|");
        foreach (var prop in properties)
        {
            headerRow.Append($" {prop.Name.SplitCamelCase()} |");
        }
        builder.AppendLine(headerRow.ToString());

        // Create the table header separator.
        var separatorRow = new StringBuilder("|");
        foreach (var _ in properties)
        {
            separatorRow.Append(" --- |");
        }
        builder.AppendLine(separatorRow.ToString());

        // Sort the rows based on the property marked with [Sort] attribute, if any.
        if (sortProperty != null)
        {
            items = sortProperty!.GetCustomAttribute<SortAttribute>()!.Ascending
                ? items.OrderBy(item => sortProperty.GetValue(item)).ToList()
                : items.OrderByDescending(item => sortProperty.GetValue(item)).ToList();
        }

        // Create rows for each item in the list, ordered by columns.
        foreach (var item in items)
        {
            var dataRow = new StringBuilder("|");
            foreach (var prop in properties)
            {
                var value = Convert.ToString(prop.GetValue(item), CultureInfo.InvariantCulture) ?? string.Empty;
                dataRow.Append($" {value} |");
            }
            builder.AppendLine(dataRow.ToString());
        }

        builder.AppendLine();
    }
}