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

    [Setting("Long Setting", 99)]
    public long LongSetting { get; set; }

    [Setting("Double Setting", 22.5)]
    public double DoubleSetting { get; set; }

    [Setting("Date Time Setting")]
    public DateTime? DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan? TimespanSetting { get; set; }

    [Setting("Bool Setting", true)]
    public bool BoolSetting { get; set; }

    [Setting("Secret Setting", "SecretString")]
    [Secret]
    public string SecretSetting { get; set; }

    [ValidValues(typeof(LogLevel))]
    [Setting("Choice of log levels", LogLevel.Info)]
    public LogLevel LogLevel { get; set; }

    [Setting("Complex String Setting", "a:b,c:d")]
    [SettingStringFormat("{key}:{value},")]
    public string ComplexStringSetting { get; set; }

    [Setting("String Collection")]
    public List<string> StringCollectionSetting { get; set; }

    [Setting("Key Value Pair Setting")]
    public List<KeyValuePair<string, string>> KvpCollectionSetting { get; set; }

    [Setting("Object List Setting")]
    public List<SomeSetting> ObjectListSetting { get; set; }

    //[Setting("Extra Setting")]
    //public string? ExtraSetting { get; set; }
    [Setting("Json Setting")]
    public SomeSetting JsonSetting { get; set; }
    
    [Setting("Multi Line Setting")]
    [MultiLine(6)]
    public string? MultiLineString { get; set; }
}

public class SomeSetting
{
    [Required]
    public string? StringVal { get; set; }

    [Required]
    public int IntVal { get; set; }

    [Required]
    public double DoubleVal { get; set; }

    [Required]
    public long LongVal { get; set; }

    [Required]
    public DateTime DateTimeVal { get; set; }

    [Required]
    public TimeSpan TimeSpanVal { get; set; }

    [Required]
    public bool BoolVal { get; set; }

    [Required]
    public LogLevel DropDownVal { get; set; }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}