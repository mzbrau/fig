using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Dashboards.Runtime;

public class DashboardTransformEngine
{
    private static readonly Regex ContainsReturnRegex = new(
        @"\breturn\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IJsEngineFactory _jsEngineFactory;

    public DashboardTransformEngine(IJsEngineFactory jsEngineFactory)
    {
        _jsEngineFactory = jsEngineFactory;
    }

    /// <summary>
    /// Executes a dashboard transform script against the given <paramref name="figRoot"/>.
    /// Named transform results are available as <c>transforms</c> and, when identifiers are valid, as top-level bindings.
    /// </summary>
    public object? ExecuteScript(
        string? script,
        DashboardFigRoot figRoot,
        IReadOnlyDictionary<string, object?>? namedResults = null)
    {
        if (string.IsNullOrWhiteSpace(script))
            return null;

        using var engine = _jsEngineFactory.CreateEngine();
        var helpers = new DashboardJsLinq();

        engine.SetValue("fig", figRoot);
        engine.SetValue("helpers", helpers);
        engine.SetValue("DashboardJsLinq", helpers);

        var transforms = namedResults is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(namedResults, StringComparer.OrdinalIgnoreCase);

        engine.SetValue("transforms", transforms);

        foreach (var (name, value) in transforms)
        {
            if (IsValidJsIdentifier(name))
                engine.SetValue(name, value);
        }

        var code = PrepareScript(script);
        return engine.Evaluate(code);
    }

    internal static string PrepareScript(string script)
    {
        var trimmed = script.Trim();
        if (trimmed.StartsWith("return", StringComparison.Ordinal) || ContainsReturnRegex.IsMatch(trimmed))
            return $"(function(){{ {trimmed}\n}})()";

        return trimmed;
    }

    private static bool IsValidJsIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!(char.IsLetter(name[0]) || name[0] == '_' || name[0] == '$'))
            return false;

        for (var i = 1; i < name.Length; i++)
        {
            if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_' || name[i] == '$'))
                return false;
        }

        return true;
    }
}
