namespace Fig.Web.Services;

/// <summary>
/// Result of a large GET including request vs body-read vs parse timing for load diagnostics.
/// </summary>
public sealed class TimedHttpResult<T>
{
    public TimedHttpResult(T? value, long requestMs, long deserializeMs)
        : this(value, requestMs, deserializeMs, bodyReadMs: null, parseMs: null)
    {
    }

    public TimedHttpResult(
        T? value,
        long requestMs,
        long deserializeMs,
        long? bodyReadMs,
        long? parseMs)
    {
        Value = value;
        RequestMs = requestMs;
        DeserializeMs = deserializeMs;
        BodyReadMs = bodyReadMs;
        ParseMs = parseMs;
    }

    public T? Value { get; }

    /// <summary>Time until response headers/body stream are available (includes server + network).</summary>
    public long RequestMs { get; }

    /// <summary>
    /// Combined body download + deserialize duration (BodyReadMs + ParseMs when split is available).
    /// </summary>
    public long DeserializeMs { get; }

    /// <summary>Time spent copying the response body into memory (JS-interop / transfer).</summary>
    public long? BodyReadMs { get; }

    /// <summary>Time spent deserializing the buffered body with Newtonsoft.</summary>
    public long? ParseMs { get; }
}
