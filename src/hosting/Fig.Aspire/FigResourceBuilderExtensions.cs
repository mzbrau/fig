namespace Fig.Aspire;

/// <summary>
/// Provides extension methods for adding Fig resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class FigResourceBuilderExtensions
{
    private const string FigApiImage = "mzbrau/fig-api";
    private const string FigWebImage = "mzbrau/fig-web";
    private const string DefaultTag = "latest";
    private const int FigApiDefaultPort = 8080;
    private const int FigWebDefaultPort = 80;

    /// <summary>
    /// Adds a Fig API container resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="port">The host port to bind to the Fig API container. If not specified, a random port will be assigned.</param>
    /// <param name="tag">The container image tag to use. Defaults to "latest".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{FigApiResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a Fig API container to your Aspire application. The Fig API provides
    /// centralized configuration management for .NET microservices.
    /// </para>
    /// <para>
    /// When other resources reference this Fig API resource using <see cref="ResourceBuilderExtensions.WithReference{TDestination}(IResourceBuilder{TDestination}, IResourceBuilder{IResourceWithConnectionString}, string?, bool)"/>,
    /// an environment variable named <c>FIG_API_URI</c> will be injected with the Fig API's HTTP endpoint URL.
    /// </para>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var figApi = builder.AddFigApi("fig-api");
    /// 
    /// builder.AddProject&lt;Projects.MyService&gt;("myservice")
    ///     .WithReference(figApi);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<FigApiResource> AddFigApi(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string tag = DefaultTag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new FigApiResource(name);

        var resourceBuilder = builder.AddResource(resource)
            .WithImage(FigApiImage)
            .WithImageTag(tag)
            .WithHttpEndpoint(port: port, targetPort: FigApiDefaultPort, name: "http")
            .WithLifetime(ContainerLifetime.Persistent);

        return resourceBuilder;
    }

    /// <summary>
    /// Adds a Fig Web container resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="port">The host port to bind to the Fig Web container. If not specified, a random port will be assigned.</param>
    /// <param name="tag">The container image tag to use. Defaults to "latest".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{FigWebResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a Fig Web container to your Aspire application. The Fig Web provides
    /// a web-based UI for managing configuration settings in Fig.
    /// </para>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var figApi = builder.AddFigApi("fig-api");
    /// var figWeb = builder.AddFigWeb("fig-web")
    ///     .WithFigApiReference(figApi);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<FigWebResource> AddFigWeb(
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null,
        string tag = DefaultTag)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new FigWebResource(name);

        var resourceBuilder = builder.AddResource(resource)
            .WithImage(FigWebImage)
            .WithImageTag(tag)
            .WithHttpEndpoint(port: port, targetPort: FigWebDefaultPort, name: "http")
            .WithLifetime(ContainerLifetime.Persistent);

        return resourceBuilder;
    }

    /// <summary>
    /// Configures the Fig Web resource to use the specified Fig API resource.
    /// </summary>
    /// <param name="builder">The Fig Web resource builder.</param>
    /// <param name="figApi">The Fig API resource builder to reference.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{FigWebResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the Fig Web container with the FIG_API_URI environment variable
    /// pointing to the specified Fig API resource, and sets up a wait dependency so that
    /// Fig Web starts after Fig API is ready.
    /// </para>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var figApi = builder.AddFigApi("fig-api");
    /// var figWeb = builder.AddFigWeb("fig-web")
    ///     .WithFigApiReference(figApi);
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<FigWebResource> WithFigApiReference(
        this IResourceBuilder<FigWebResource> builder,
        IResourceBuilder<FigApiResource> figApi)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(figApi);

        // Fig Web is a Blazor WebAssembly app that runs in the browser.
        // It must call the Fig API via the host's published address/port (localhost network context),
        // not via container-to-container DNS (e.g., http://fig-api:8080).
        return builder
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables[FigApiResource.FigApiUriEnvironmentVariable] =
                    figApi.Resource.LocalhostEndpoint.Url;
            })
            .WaitFor(figApi);
    }
}
