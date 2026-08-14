namespace Fig.Web.Dashboards.Components.Contracts;

public class DashboardKpiInput
{
    public object? Value { get; set; }

    public string? Label { get; set; }

    public object? Trend { get; set; }

    /// <summary>normal | info | success | warning | danger</summary>
    public string? Variant { get; set; }

    /// <summary>When set with <see cref="Denominator"/>, displayed as numerator/denominator instead of <see cref="Value"/>.</summary>
    public object? Numerator { get; set; }

    public object? Denominator { get; set; }

    public string? Subtitle { get; set; }

    /// <summary>Radzen Material icon name; null/empty hides the icon.</summary>
    public string? Icon { get; set; }
}

public class DashboardCardRow
{
    public string? Key { get; set; }

    public object? Value { get; set; }
}

public class DashboardCardItem
{
    public string? Title { get; set; }

    public object? Value { get; set; }

    /// <summary>normal | info | success | warning | danger</summary>
    public string? Variant { get; set; }

    /// <summary>Radzen Material icon name; null/empty hides the icon.</summary>
    public string? Icon { get; set; }

    public IReadOnlyList<DashboardCardRow> Rows { get; set; } =
        Array.Empty<DashboardCardRow>();
}

public class DashboardCardsInput
{
    public IReadOnlyList<DashboardCardItem> Cards { get; set; } =
        Array.Empty<DashboardCardItem>();
}

public class DashboardTextInput
{
    public IReadOnlyList<DashboardTextLine> Lines { get; set; } = Array.Empty<DashboardTextLine>();

    /// <summary>Legacy single-line text; preferred shape is <see cref="Lines"/>.</summary>
    public string? Text { get; set; }

    /// <summary>Legacy variant (heading|body|muted); mapped to size when Lines is empty.</summary>
    public string? Variant { get; set; }
}

public class DashboardTextLine
{
    public string? Text { get; set; }

    /// <summary>xs | sm | md | lg | xl | xxl</summary>
    public string? Size { get; set; }

    /// <summary>Any CSS color.</summary>
    public string? Color { get; set; }

    /// <summary>left | center | right</summary>
    public string? Align { get; set; }

    /// <summary>normal | bold</summary>
    public string? Weight { get; set; }
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
