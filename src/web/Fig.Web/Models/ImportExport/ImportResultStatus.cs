using Fig.Contracts.ImportExport;

namespace Fig.Web.Models.ImportExport;

/// <summary>
/// Interprets import API results for Import/Export page status text.
/// Null means HttpService already handled a non-success status (toast shown).
/// </summary>
public static class ImportResultStatus
{
    public static (string Message, bool Succeeded) DescribeGroupImport(ImportResultDataContract? result)
    {
        if (result is null)
            return ("Import failed (see notification for details).", false);

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            return ($"Import failed: {result.ErrorMessage}", false);

        return ("Import completed successfully.", true);
    }

    public static string DescribeSettingsImportHttpFailure() =>
        "Import Failed (see notification for details).";
}
