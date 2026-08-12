using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components;

namespace Fig.Web.Dashboards.Runtime;

public sealed class DashboardComponentResult
{
    public bool Success { get; init; }

    public object? Data { get; init; }

    public string? Error { get; init; }

    public static DashboardComponentResult Ok(object? data) => new()
    {
        Success = true,
        Data = data
    };

    public static DashboardComponentResult Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}

public class DashboardRuntime
{
    private readonly DashboardTransformEngine _transformEngine;
    private readonly DashboardDependencyResolver _dependencyResolver;
    private readonly DashboardComponentRegistry _componentRegistry;
    private readonly IDashboardDataProvider _dataProvider;

    public DashboardRuntime(
        DashboardTransformEngine transformEngine,
        DashboardDependencyResolver dependencyResolver,
        DashboardComponentRegistry componentRegistry,
        IDashboardDataProvider dataProvider)
    {
        _transformEngine = transformEngine;
        _dependencyResolver = dependencyResolver;
        _componentRegistry = componentRegistry;
        _dataProvider = dataProvider;
    }

    public DashboardDefinitionDataContract? Definition { get; private set; }

    public IReadOnlyDictionary<string, object?> NamedTransformResults { get; private set; } =
        new Dictionary<string, object?>();

    public void SetDefinition(DashboardDefinitionDataContract definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <summary>
    /// Evaluates named transforms then resolves each component's data binding.
    /// Failures are isolated per component / transform.
    /// </summary>
    public Dictionary<string, DashboardComponentResult> Evaluate()
    {
        if (Definition is null)
            throw new InvalidOperationException("Dashboard definition has not been set.");

        var fig = _dataProvider.Current;
        var transformResults = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var transformErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<string> order;
        try
        {
            order = _dependencyResolver.ResolveOrder(Definition.Transforms);
        }
        catch (InvalidOperationException ex)
        {
            NamedTransformResults = transformResults;
            return Definition.Components.ToDictionary(
                c => c.Id,
                c => DashboardComponentResult.Fail(ex.Message),
                StringComparer.OrdinalIgnoreCase);
        }

        var transformsById = Definition.Transforms
            .ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var id in order)
        {
            if (!transformsById.TryGetValue(id, out var transform))
                continue;

            try
            {
                transformResults[id] = _transformEngine.ExecuteScript(
                    transform.Script,
                    fig,
                    transformResults);
            }
            catch (Exception ex)
            {
                transformErrors[id] = ex.Message;
                transformResults[id] = null;
            }
        }

        NamedTransformResults = transformResults;

        var results = new Dictionary<string, DashboardComponentResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in Definition.Components)
        {
            results[component.Id] = ResolveComponentData(component, fig, transformResults, transformErrors);
        }

        return results;
    }

    private DashboardComponentResult ResolveComponentData(
        DashboardComponentInstanceDataContract component,
        DashboardFigRoot fig,
        IReadOnlyDictionary<string, object?> transformResults,
        IReadOnlyDictionary<string, string> transformErrors)
    {
        try
        {
            var binding = component.DataBinding ?? new DashboardDataBindingDataContract();
            var mode = binding.Mode?.Trim() ?? "inline";

            if (string.Equals(mode, "namedTransform", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(binding.TransformId))
                    return DashboardComponentResult.Fail("Named transform binding is missing TransformId.");

                if (transformErrors.TryGetValue(binding.TransformId, out var transformError))
                    return DashboardComponentResult.Fail($"Transform '{binding.TransformId}' failed: {transformError}");

                if (!transformResults.TryGetValue(binding.TransformId, out var data))
                    return DashboardComponentResult.Fail($"Transform '{binding.TransformId}' was not found.");

                return DashboardComponentResult.Ok(data);
            }

            if (string.Equals(mode, "preset", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(binding.PresetId))
                    return DashboardComponentResult.Fail("Preset binding is missing PresetId.");

                var preset = _componentRegistry.GetPreset(binding.PresetId);
                if (preset is null)
                    return DashboardComponentResult.Fail($"Preset '{binding.PresetId}' was not found.");

                var data = _transformEngine.ExecuteScript(preset.Script, fig, transformResults);
                return DashboardComponentResult.Ok(data);
            }

            // inline (default)
            if (string.IsNullOrWhiteSpace(binding.InlineScript))
                return DashboardComponentResult.Fail("Inline binding is missing InlineScript.");

            var inlineData = _transformEngine.ExecuteScript(binding.InlineScript, fig, transformResults);
            return DashboardComponentResult.Ok(inlineData);
        }
        catch (Exception ex)
        {
            return DashboardComponentResult.Fail(ex.Message);
        }
    }
}
