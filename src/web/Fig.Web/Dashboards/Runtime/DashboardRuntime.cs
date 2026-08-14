using Fig.Contracts.Dashboards;

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
    private readonly IDashboardDataProvider _dataProvider;

    public DashboardRuntime(
        DashboardTransformEngine transformEngine,
        IDashboardDataProvider dataProvider)
    {
        _transformEngine = transformEngine;
        _dataProvider = dataProvider;
    }

    public DashboardDefinitionDataContract? Definition { get; private set; }

    public void SetDefinition(DashboardDefinitionDataContract definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <summary>
    /// Resolves each component's inline script binding. Failures are isolated per component.
    /// </summary>
    public Dictionary<string, DashboardComponentResult> Evaluate()
    {
        if (Definition is null)
            throw new InvalidOperationException("Dashboard definition has not been set.");

        var fig = _dataProvider.Current;
        var results = new Dictionary<string, DashboardComponentResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in Definition.Components)
        {
            results[component.Id] = ResolveComponentData(component, fig);
        }

        return results;
    }

    private DashboardComponentResult ResolveComponentData(
        DashboardComponentInstanceDataContract component,
        DashboardFigRoot fig)
    {
        try
        {
            var binding = component.DataBinding ?? new DashboardDataBindingDataContract();
            if (string.IsNullOrWhiteSpace(binding.InlineScript))
                return DashboardComponentResult.Fail("Inline binding is missing InlineScript.");

            var data = _transformEngine.ExecuteScript(binding.InlineScript, fig);
            return DashboardComponentResult.Ok(data);
        }
        catch (Exception ex)
        {
            return DashboardComponentResult.Fail(ex.Message);
        }
    }
}
