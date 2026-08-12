namespace Fig.Web.Dashboards.Components.Contracts;

public class DashboardKpiInput
{
    public object? Value { get; set; }

    public string? Label { get; set; }

    public object? Trend { get; set; }

    public string? Variant { get; set; }
}

public class DashboardTextInput
{
    public string? Text { get; set; }

    /// <summary>heading | body | muted</summary>
    public string? Variant { get; set; }
}

public class DashboardBadgeInput
{
    public string? Text { get; set; }

    /// <summary>normal | info | success | warning | danger | muted</summary>
    public string? Variant { get; set; }
}

public class DashboardChartPoint
{
    public string? Label { get; set; }

    public double Value { get; set; }
}

public class DashboardTableColumn
{
    public string Property { get; set; } = string.Empty;

    public string? Header { get; set; }

    public string? Align { get; set; }
}

public class DashboardTableInput
{
    public IReadOnlyList<IDictionary<string, object?>> Rows { get; set; } =
        Array.Empty<IDictionary<string, object?>>();

    public IReadOnlyList<DashboardTableColumn> Columns { get; set; } =
        Array.Empty<DashboardTableColumn>();
}

public class DashboardListItem
{
    public string? Text { get; set; }

    public string? Secondary { get; set; }

    public string? Variant { get; set; }
}

public class DashboardListInput
{
    public IReadOnlyList<DashboardListItem> Items { get; set; } = Array.Empty<DashboardListItem>();
}

public class DashboardKeyValueItem
{
    public string? Key { get; set; }

    public object? Value { get; set; }
}

public class DashboardKeyValueInput
{
    public IReadOnlyList<DashboardKeyValueItem> Items { get; set; } =
        Array.Empty<DashboardKeyValueItem>();

    /// <summary>Radzen Material icon name; null/empty hides the status badge.</summary>
    public string? StatusIcon { get; set; }

    /// <summary>CSS color for the status circle background.</summary>
    public string? StatusColor { get; set; }
}
