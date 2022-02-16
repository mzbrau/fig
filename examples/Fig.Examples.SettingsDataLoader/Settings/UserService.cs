using System.ComponentModel.DataAnnotations;
using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class UserService : SettingsBase
{
    public override string ClientName => "UserService";

    public override string ClientSecret => "0492d5f8-d375-4209-a8af-c7c95371024d";

    [Group("GroupA")]
    [Setting("String Setting", "Cat")]
    public string StringSetting { get; set; }

    [Group("GroupA")]
    [Setting("Int Setting", 34)]
    public int IntSetting { get; set; }

    [Setting("Date Time Setting")]
    public DateTime DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan TimespanSetting { get; set; }

    [Setting("Bool Setting", true)]
    public bool BoolSetting { get; set; }

    [Setting("Secret Setting", "SecretString")]
    [Secret]
    public string SecretSetting { get; set; }

    [ValidValues(typeof(LogLevel))]
    [Setting("Choice of log levels")]
    public LogLevel LogLevel { get; set; }

    [Setting("Complex String Setting", "a:b,c:d")]
    [SettingStringFormat("{key}:{value},")]
    public string ComplexStringSetting { get; set; }

    [Setting("String Collection")]
    public List<string> StringCollectionSetting { get; set; }

    [Setting("Key Value Pair Setting")]
    public List<KeyValuePair<string, string>> KvpCollectionSetting { get; set; }

    // TODO: Investigate why this doesn't work.
    [Setting("Object List Setting")]
    public List<SomeSetting> ObjectListSetting { get; set; }
}

public class SomeSetting
{
    [Required]
    public string Key { get; set; }

    [Required]
    public string Value { get; set; }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}