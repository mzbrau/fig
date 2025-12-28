using Aspire.Hosting.ApplicationModel;

namespace Fig.Aspire;

/// <summary>
/// Represents a Fig Web container resource in the application model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class FigWebResource(string name) : ContainerResource(name)
{
    /// <summary>
    /// The primary HTTP endpoint for the Fig Web application.
    /// </summary>
    public EndpointReference PrimaryEndpoint => new(this, "http");
}
