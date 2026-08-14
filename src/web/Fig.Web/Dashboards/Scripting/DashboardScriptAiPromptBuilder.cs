using System.Text;
using Fig.Web.Dashboards.Runtime;

namespace Fig.Web.Dashboards.Scripting;

/// <summary>
/// Builds a clipboard-ready prompt so an external AI can write a valid dashboard inline script.
/// </summary>
public static class DashboardScriptAiPromptBuilder
{
    public static string Build(
        string componentType,
        string? displayName,
        string expectedShape,
        string? currentScript,
        DashboardFigRoot? fig = null)
    {
        var type = string.IsNullOrWhiteSpace(componentType) ? "unknown" : componentType.Trim();
        var name = string.IsNullOrWhiteSpace(displayName) ? type : displayName.Trim();
        var shape = string.IsNullOrWhiteSpace(expectedShape)
            ? "See component documentation for the expected return shape."
            : expectedShape.Trim();
        var expectedResult = DashboardScriptTypings.BuildExpectedResult(type).Trim();
        var ambient = DashboardScriptTypings.BuildAmbient().Trim();

        var sb = new StringBuilder();
        sb.AppendLine("USER REQUEST: <describe what you want this visualization to show>");
        sb.AppendLine();
        sb.AppendLine("## Task");
        sb.AppendLine(
            $"Write a Fig dashboard inline JavaScript script for a '{name}' ({type}) visualization.");
        sb.AppendLine(
            "Return a value that matches the expected shape below. Top-level `return` is valid in this scripting context.");
        sb.AppendLine(
            "Respond with the complete JavaScript inside a single fenced markdown code block (```javascript ... ```). Do not include surrounding explanation unless the user asks.");
        sb.AppendLine();
        sb.AppendLine("## Component");
        sb.AppendLine($"Type: {type}");
        sb.AppendLine($"Display name: {name}");
        sb.AppendLine($"Expected return shape: {shape}");
        sb.AppendLine();
        sb.AppendLine("## Expected return type");
        sb.AppendLine(expectedResult);
        sb.AppendLine();
        sb.AppendLine("## Available inputs (`fig`)");
        sb.AppendLine(
            "`fig.clients` and `fig.runSessions` are `DashboardJsArray` instances (not native JavaScript arrays). Use the fluent methods below.");
        sb.AppendLine();
        sb.AppendLine(ambient);

        if (fig is not null)
        {
            var dynamic = DashboardScriptTypings.BuildDynamic(fig).Trim();
            sb.AppendLine();
            sb.AppendLine("## Live keys (from current data)");
            sb.AppendLine(
                "These keys were observed on live clients/run sessions. Prefer them for `settings` / `customProperties` access; other keys may also exist.");
            sb.AppendLine();
            sb.AppendLine(dynamic);
        }

        sb.AppendLine();
        sb.AppendLine("## Scripting rules");
        sb.AppendLine("- Prefer `fig.clients` / `fig.runSessions` fluent APIs (`length`, `filter`, `map`, `groupBy`, `sort`, `take`, `distinct`, `count`, `sum`, `average`, `min`, `max`, `first`, `last`, `toArray`).");
        sb.AppendLine("- Do not use `Object.keys` or `Array.isArray` on CLR-backed `fig` objects — they are `DashboardJsArray`, not native JS arrays.");
        sb.AppendLine("- `groupBy` returns groups shaped as `{ key, items }` where `items` is also a `DashboardJsArray`.");
        sb.AppendLine("- Scripts may use `return { ... }` or evaluate to an expression.");
        sb.AppendLine("- Put the complete script in a ```javascript``` markdown code block so the user can copy it into the Inline script field.");

        if (!string.IsNullOrWhiteSpace(currentScript))
        {
            sb.AppendLine();
            sb.AppendLine("## Current script");
            sb.AppendLine("Use this as a starting point to refine (unless the user request says otherwise):");
            sb.AppendLine();
            sb.AppendLine(currentScript.Trim());
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }
}
