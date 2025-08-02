namespace Fig.Web.Models.Setting;

public interface ISearchableSetting
{
    string ParentName { get; }

    string ParentInstance { get; }

    string DisplayName  { get; }

    string CategoryColor  { get; }

    string TruncatedDescription { get; }

    string IconKey { get; }

    string TruncatedStringValue  { get; }
    
    string ScrollId { get; }

    bool Advanced  { get; }
    
    SettingClientConfigurationModel Parent { get; }

    void Expand();

    bool IsSearchMatch(string? clientToken, string? settingToken, string? descriptionToken, string? instanceToken,
        string? valueToken, List<string> generalTokens);
}