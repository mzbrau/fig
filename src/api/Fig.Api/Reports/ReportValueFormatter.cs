using System.Net;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Api.Reports;

public static class ReportValueFormatter
{
    public static string FormatSettingValue(SettingValueBaseBusinessEntity? value)
    {
        if (value is null)
            return string.Empty;

        var raw = value.GetValue();
        return FormatObject(raw);
    }

    public static string FormatObject(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is string s)
            return s;

        if (value is DateTime dt)
            return dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

        if (value.GetType().IsPrimitive || value is decimal)
            return Convert.ToString(value) ?? string.Empty;

        try
        {
            return JsonConvert.SerializeObject(value, Formatting.None, JsonSettings.FigDefault);
        }
        catch
        {
            return Convert.ToString(value) ?? string.Empty;
        }
    }

    public static string FormatClientDisplay(string clientName, string? instance)
        => string.IsNullOrWhiteSpace(instance) ? clientName : $"{clientName} [{instance}]";

    public static string FormatFriendlyType(Type? type)
    {
        if (type is null)
            return "Unknown";

        var figType = type.FigPropertyType();
        if (figType != FigPropertyType.Unsupported)
            return figType.ToString();

        var name = type.Name;
        var tickIndex = name.IndexOf('`');
        return tickIndex > 0 ? name[..tickIndex] : name;
    }

    public static string FormatValueAsHtml(SettingValueBaseBusinessEntity? value)
        => WebUtility.HtmlEncode(FormatSettingValue(value));
}
