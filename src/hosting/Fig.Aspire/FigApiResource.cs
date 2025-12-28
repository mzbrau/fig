namespace Fig.Aspire;

/// <summary>
/// Represents a Fig API container resource in the application model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class FigApiResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// The environment variable name for the Fig API URI.
    /// </summary>
    public const string FigApiUriEnvironmentVariable = "FIG_API_URI";

    /// <summary>
    /// The primary HTTP endpoint for the Fig API.
    /// </summary>
    public EndpointReference PrimaryEndpoint => new(this, "http");

    /// <summary>
    /// The Fig API HTTP endpoint resolved in the localhost network context.
    /// </summary>
    /// <remarks>
    /// This is required for browser-based clients (e.g., Fig Web / Blazor WASM) which must reach the API
    /// via the host's published port (e.g., <c>http://localhost:{port}</c>) rather than the container DNS name.
    /// </remarks>
    public EndpointReference LocalhostEndpoint => new(this, "http", KnownNetworkIdentifiers.LocalhostNetwork);

    /// <summary>
    /// Gets the connection string expression for the Fig API.
    /// </summary>
    /// <remarks>
    /// The connection string is the HTTP URI of the Fig API.
    /// </remarks>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");

    /// <summary>
    /// Gets the environment variable name to use for the connection string.
    /// </summary>
    public string? ConnectionStringEnvironmentVariable => FigApiUriEnvironmentVariable;
}
