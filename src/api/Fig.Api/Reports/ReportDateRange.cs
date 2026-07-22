namespace Fig.Api.Reports;

public static class ReportDateRange
{
    public const int MaxSpanDays = 366;

    public static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);

    public static (DateTime FromUtc, DateTime ToUtc) Validate(DateTime from, DateTime to, int maxSpanDays = MaxSpanDays)
    {
        var fromUtc = EnsureUtc(from);
        var toUtc = EnsureUtc(to);

        if (fromUtc > toUtc)
            throw new ReportParameterValidationException("From must be before To.");

        if ((toUtc - fromUtc).TotalDays > maxSpanDays)
            throw new ReportParameterValidationException($"Date range cannot exceed {maxSpanDays} days.");

        return (fromUtc, toUtc);
    }

    public static string? NormalizeOptionalClient(string? clientName)
        => string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim();
}
