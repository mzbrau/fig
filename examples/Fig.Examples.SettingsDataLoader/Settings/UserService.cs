using System.ComponentModel.DataAnnotations;
using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Examples.SettingsDataLoader.Settings;

public class UserService : SettingsBase
{
    public override string ClientDescription => "Sample User Service";


    //[Group("GroupA")]
    [Setting("String Setting")] 
    public string StringSetting { get; set; } = "Cat";

    ///[Group("GroupA")]
    [Setting("Int Setting")]
    public int IntSetting { get; set; } = 34;

    [Setting("Long Setting")] 
    public long LongSetting { get; set; } = 99;

    [Setting("Double Setting")] 
    public double DoubleSetting { get; set; } = 22.5;

    [Setting("Date Time Setting")]
    public DateTime? DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan? TimespanSetting { get; set; }

    [Setting("Bool Setting", true)]
    public bool BoolSetting { get; set; }

    [Setting("Secret Setting")] [Secret] 
    public string SecretSetting { get; set; } = "SecretString";

    [DisplayOrder(1)]
    [ValidValues(typeof(LogLevel))]
    [Setting("Choice of log levels")]
    public LogLevel EnumSetting { get; set; } = LogLevel.Info;

    [DisplayOrder(2)]
    [ValidValues("a", "b", "c")]
    [Setting("Choose from a, b or c")]
    public string DropDownStringSetting { get; set; } = "a";

    [DisplayOrder(3)]
    [ValidValues("1 -> High", "2 -> Medium", "3 -> Low")]
    [Setting("Enum value")]
    public int Levels { get; set; } = 1;

    [Setting("String Collection")]
    public List<string> StringCollectionSetting { get; set; }

    [Setting("Object List Setting")]
    public List<SomeSetting> ObjectListSetting { get; set; }

    //[Setting("Extra Setting")]
    //public string? ExtraSetting { get; set; }
    [Setting("Json Setting")]
    public SomeSetting JsonSetting { get; set; }
    
    [Setting("Multi Line Setting")]
    [MultiLine(6)]
    public string? MultiLineString { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
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