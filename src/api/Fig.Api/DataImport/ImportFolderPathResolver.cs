namespace Fig.Api.DataImport;

internal static class ImportFolderPathResolver
{
    /// <summary>
    /// Validates that the configured path is valid without creating the directory.
    /// Use this for validation during startup checks.
    /// </summary>
    public static bool TryValidate(string? configuredPath, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        if (string.IsNullOrWhiteSpace(configuredPath))
            return false;

        var trimmedPath = configuredPath.Trim();
        var expandedPath = Environment.ExpandEnvironmentVariables(trimmedPath);

        if (expandedPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return false;

        if (!Path.IsPathRooted(expandedPath))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(expandedPath);
            resolvedPath = fullPath;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                "Failed to resolve import folder path '{0}': {1}",
                expandedPath,
                ex);
            return false;
        }
    }

    /// <summary>
    /// Validates and creates the directory for the configured path.
    /// Use this when actually preparing to use the import folder.
    /// </summary>
    public static bool TryResolve(string? configuredPath, out string resolvedPath)
    {
        if (!TryValidate(configuredPath, out resolvedPath))
            return false;

        try
        {
            Directory.CreateDirectory(resolvedPath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                "Failed to create import folder directory '{0}': {1}",
                resolvedPath,
                ex);
            resolvedPath = string.Empty;
            return false;
        }
    }
}
