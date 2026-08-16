using Fig.Common.NetStandard.Scripting;
using Fig.Web.Facades;

namespace Fig.Web.Dashboards.Runtime;

public class DashboardTransformEngine
{
    private readonly IJsEngineFactory _jsEngineFactory;
    private readonly IConfigurationFacade _configurationFacade;

    public DashboardTransformEngine(IJsEngineFactory jsEngineFactory, IConfigurationFacade configurationFacade)
    {
        _jsEngineFactory = jsEngineFactory;
        _configurationFacade = configurationFacade;
    }

    /// <summary>
    /// Executes a dashboard component script against the given <paramref name="figRoot"/>.
    /// </summary>
    public object? ExecuteScript(string? script, DashboardFigRoot figRoot)
    {
        if (string.IsNullOrWhiteSpace(script))
            return null;

        if (!_configurationFacade.AllowDisplayScripts)
            throw new InvalidOperationException(
                "Dashboard scripts cannot run because JavaScript execution is disabled.");

        using var engine = _jsEngineFactory.CreateEngine();
        var helpers = new DashboardJsLinq();

        engine.SetValue("fig", figRoot);
        engine.SetValue("helpers", helpers);
        engine.SetValue("DashboardJsLinq", helpers);

        var code = PrepareScript(script);
        return engine.Evaluate(code);
    }

    internal static string PrepareScript(string script)
    {
        var trimmed = script.Trim();
        if (ContainsTopLevelReturn(trimmed))
            return $"(function(){{ {trimmed}\n}})()";

        return trimmed;
    }

    /// <summary>
    /// True when the script contains a <c>return</c> at brace depth 0 (outside strings and comments).
    /// </summary>
    internal static bool ContainsTopLevelReturn(string script)
    {
        var braceDepth = 0;
        var i = 0;
        while (i < script.Length)
        {
            var c = script[i];

            if (c is '"' or '\'')
            {
                i = SkipQuotedString(script, i);
                continue;
            }

            if (c == '`')
            {
                i = SkipTemplateLiteral(script, i);
                continue;
            }

            if (c == '/' && i + 1 < script.Length)
            {
                var next = script[i + 1];
                if (next == '/')
                {
                    i = SkipLineComment(script, i);
                    continue;
                }

                if (next == '*')
                {
                    i = SkipBlockComment(script, i);
                    continue;
                }
            }

            if (c == '{')
            {
                braceDepth++;
                i++;
                continue;
            }

            if (c == '}')
            {
                if (braceDepth > 0)
                    braceDepth--;
                i++;
                continue;
            }

            if (braceDepth == 0 && IsReturnKeywordAt(script, i))
                return true;

            i++;
        }

        return false;
    }

    private static bool IsReturnKeywordAt(string script, int index)
    {
        const string keyword = "return";
        if (index + keyword.Length > script.Length)
            return false;

        if (!script.AsSpan(index, keyword.Length).Equals(keyword, StringComparison.Ordinal))
            return false;

        if (index > 0 && IsIdentifierPart(script[index - 1]))
            return false;

        var after = index + keyword.Length;
        if (after < script.Length && IsIdentifierPart(script[after]))
            return false;

        return true;
    }

    private static bool IsIdentifierPart(char c) =>
        char.IsLetterOrDigit(c) || c == '_' || c == '$';

    private static int SkipQuotedString(string script, int start)
    {
        var quote = script[start];
        var i = start + 1;
        while (i < script.Length)
        {
            var c = script[i];
            if (c == '\\')
            {
                i += 2;
                continue;
            }

            if (c == quote)
                return i + 1;

            i++;
        }

        return script.Length;
    }

    private static int SkipTemplateLiteral(string script, int start)
    {
        var i = start + 1;
        while (i < script.Length)
        {
            var c = script[i];
            if (c == '\\')
            {
                i += 2;
                continue;
            }

            if (c == '`')
                return i + 1;

            // Nested ${...} — skip until matching brace at depth 1 from the ${
            if (c == '$' && i + 1 < script.Length && script[i + 1] == '{')
            {
                i += 2;
                var depth = 1;
                while (i < script.Length && depth > 0)
                {
                    var inner = script[i];
                    if (inner is '"' or '\'')
                    {
                        i = SkipQuotedString(script, i);
                        continue;
                    }

                    if (inner == '`')
                    {
                        i = SkipTemplateLiteral(script, i);
                        continue;
                    }

                    if (inner == '{')
                        depth++;
                    else if (inner == '}')
                        depth--;
                    i++;
                }

                continue;
            }

            i++;
        }

        return script.Length;
    }

    private static int SkipLineComment(string script, int start)
    {
        var i = start + 2;
        while (i < script.Length && script[i] is not ('\n' or '\r'))
            i++;
        return i;
    }

    private static int SkipBlockComment(string script, int start)
    {
        var i = start + 2;
        while (i + 1 < script.Length)
        {
            if (script[i] == '*' && script[i + 1] == '/')
                return i + 2;
            i++;
        }

        return script.Length;
    }
}
