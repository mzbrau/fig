namespace Fig.Client.Capabilities;

/// <summary>
/// Provides information about optional features supported by the connected Fig API.
/// </summary>
public interface IFigCapabilityProvider
{
    /// <summary>
    /// Returns true if the API supports the named feature.
    /// Safe to call before the API has been contacted — returns false until capabilities are fetched.
    /// </summary>
    bool Supports(string feature);

    /// <summary>
    /// Fetch (or refresh) capabilities from the API. Idempotent — subsequent calls are no-ops
    /// unless <paramref name="force"/> is true.
    /// </summary>
    System.Threading.Tasks.Task FetchAsync(bool force = false);
}
