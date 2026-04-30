using Fig.Api.Utils;

namespace Fig.Api.DataImport;

public class ApplySettingsResult
{
    public List<ChangedSetting> Changes { get; } = new();

    public HashSet<string> HandledImportSettingNames { get; } = new(StringComparer.Ordinal);

    public List<string> Warnings { get; } = new();
}
