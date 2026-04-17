using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.DisplayScripts;

namespace Fig.Test.Common.TestSettings;

public class MultipleDisplayScriptsSettings : TestSettingsBase
{
    public const string Script1 = "AStringSetting.IsReadOnly = true;";
    public const string Script2 = "AnIntSetting.IsVisible = false;";
    public const string PlaceholderScript = "{{this}}.InformationText = 'Current value: ' + {{this}}.Value;";

    public override string ClientName => "MultipleDisplayScriptsSettings";
    public override string ClientDescription => "Client with multiple display scripts";

    [Setting("This is a string")]
    [DisplayScript(PlaceholderScript)]
    public string AStringSetting { get; set; } = "Hello";

    [Setting("This is an int", false)]
    [DisplayScript(DisplayScriptLibrary.ValidatePort)]
    public int AnIntSetting { get; set; } = 8080;

    [Setting("This is a bool setting")]
    [DisplayScript(Script1)]
    [DisplayScript(Script2)]
    public bool ABoolSetting { get; set; } = true;

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
