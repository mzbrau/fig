namespace Fig.Web.Services;

/// <summary>
/// Result of a large GET including request vs deserialize timing for load diagnostics.
/// </summary>
public sealed class TimedHttpResult<T>
{
    public TimedHttpResult(T? value, long requestMs, long deserializeMs)
    {
        Value = value;
        RequestMs = requestMs;
        DeserializeMs = deserializeMs;
    }

    public T? Value { get; }

    /// <summary>Time until response headers/body stream are available (includes server + network).</summary>
    public long RequestMs { get; }

    /// <summary>Time spent deserializing the response body.</summary>
    public long DeserializeMs { get; }
}
