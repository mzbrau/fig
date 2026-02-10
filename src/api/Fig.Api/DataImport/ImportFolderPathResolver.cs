namespace Fig.Api.DataImport;

internal static class ImportFolderPathResolver
{
    public static bool TryResolve(string? configuredPath, out string resolvedPath)
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
            Directory.CreateDirectory(fullPath);
            resolvedPath = fullPath;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
