namespace Fig.Web.Models.Compare;

public class SettingCompareModel
{
    public SettingCompareModel(
        string clientName,
        string? instance,
        string settingName,
        string? liveValue,
        string? exportValue,
        CompareStatus status,
        bool isAdvanced = false,
        string? lastChangedBy = null,
        DateTime? lastChangedAt = null,
        string? lastChangeMessage = null,
        string? exportChangedBy = null,
        DateTime? exportChangedAt = null,
        string? exportChangeMessage = null)
    {
        ClientName = clientName;
        Instance = instance;
        SettingName = settingName;
        LiveValue = liveValue;
        ExportValue = exportValue;
        Status = status;
        IsAdvanced = isAdvanced;
        LastChangedBy = lastChangedBy;
        LastChangedAt = lastChangedAt;
        LastChangeMessage = lastChangeMessage;
        ExportChangedBy = exportChangedBy;
        ExportChangedAt = exportChangedAt;
        ExportChangeMessage = exportChangeMessage;
    }

    public string ClientName { get; }

    public string? Instance { get; }

    public string SettingName { get; }

    public string? LiveValue { get; }

    public string? ExportValue { get; }

    public CompareStatus Status { get; }

    public bool IsAdvanced { get; }

    public string? LastChangedBy { get; }

    public DateTime? LastChangedAt { get; }

    public string? LastChangeMessage { get; }

    public string? ExportChangedBy { get; }

    public DateTime? ExportChangedAt { get; }

    public string? ExportChangeMessage { get; }

    /// <summary>
    /// For data grid settings, holds the raw JSON export value so it can be
    /// correctly deserialized when applying the export value as a pending change.
    /// Null for non-data-grid settings.
    /// </summary>
    public string? RawExportJson { get; init; }

    /// <summary>
    /// Display-friendly client identifier including instance when present.
    /// </summary>
    public string ClientDisplayName => Instance != null ? $"{ClientName} [{Instance}]" : ClientName;

    /// <summary>
    /// Whether this row represents a difference that can be applied from the export.
    /// </summary>
    public bool IsApplicable => Status == CompareStatus.Different && ExportValue != null;

    /// <summary>
    /// True when the live change is strictly more recent than the export change.
    /// </summary>
    public bool IsLiveNewer => LastChangedAt != null && ExportChangedAt != null && LastChangedAt > ExportChangedAt;

    /// <summary>
    /// True when the export change is strictly more recent than the live change.
    /// </summary>
    public bool IsExportNewer => ExportChangedAt != null && LastChangedAt != null && ExportChangedAt > LastChangedAt;
}
