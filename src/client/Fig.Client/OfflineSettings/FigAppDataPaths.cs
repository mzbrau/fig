using System;
using System.IO;
using System.Linq;

namespace Fig.Client.OfflineSettings;

internal static class FigAppDataPaths
{
    public static string GetFigFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Fig");
    }

    public static string SanitizeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(input.Where(c => !invalidChars.Contains(c)).ToArray());
        return sanitized.Replace(" ", string.Empty);
    }

    public static string BuildFileName(string clientName, string? instance, string extension)
    {
        var sanitizedClientName = SanitizeFileName(clientName);
        var sanitizedInstance = string.IsNullOrWhiteSpace(instance) ? null : SanitizeFileName(instance!);

        // Length prefix prevents collision between e.g. 'A_B' (no instance) and 'A' + instance 'B'
        return sanitizedInstance is null
            ? $"{sanitizedClientName}.{extension}"
            : $"{sanitizedClientName.Length}_{sanitizedClientName}_{sanitizedInstance}.{extension}";
    }

    public static string GetFilePath(string clientName, string? instance, string extension) =>
        Path.Combine(GetFigFolder(), BuildFileName(clientName, instance, extension));

    public static void EnsureFigFolderExists()
    {
        var folder = GetFigFolder();
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }
}
