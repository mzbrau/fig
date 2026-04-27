namespace Fig.Web.ReleaseHighlights;

public static class ReleaseHighlightsVersionComparer
{
    public static Version GetSortKey(string version)
    {
        return TryParseMajorMinor(version, out var parsedVersion)
            ? parsedVersion
            : new Version(int.MaxValue, int.MaxValue);
    }

    public static bool IsReleasedOnOrBefore(string releaseVersion, string currentVersion)
    {
        if (!TryParseMajorMinor(releaseVersion, out var release))
            return false;

        if (!TryParseMajorMinor(currentVersion, out var current))
            return true;

        return release.CompareTo(current) <= 0;
    }

    private static bool TryParseMajorMinor(string version, out Version normalizedVersion)
    {
        if (Version.TryParse(version, out var parsedVersion))
        {
            normalizedVersion = new Version(parsedVersion.Major, parsedVersion.Minor);
            return true;
        }

        normalizedVersion = new Version(0, 0);
        return false;
    }
}
