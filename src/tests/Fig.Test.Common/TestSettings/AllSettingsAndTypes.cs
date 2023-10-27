using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class AllSettingsAndTypes : TestSettingsBase
{
    public override string ClientName => "AllSettingsAndTypes";
    public override string ClientDescription => "Sample settings with all types of settings";


    [Setting("String Setting", "Cat")]
    public string StringSetting { get; set; } = null!;

    [Setting("Int Setting", 34)]
    public int IntSetting { get; set; }

    [Setting("Long Setting", 64)]
    public long LongSetting { get; set; }

    [Setting("Long Setting", 45.3)]
    public double DoubleSetting { get; set; }

    [Setting("Date Time Setting")]
    public DateTime? DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan? TimespanSetting { get; set; }

    [Setting("Bool Setting", true)]
    public bool BoolSetting { get; set; }

    [Setting("Common LookupTable Setting", 5)]
    [LookupTable("States")]
    public long LookupTableSetting { get; set; }

    [Setting("Secret Setting", "SecretString")]
    [Secret]
    public string SecretSetting { get; set; } = null!;

    [Setting("String Collection")]
    public List<string>? StringCollectionSetting { get; set; }

    [Setting("Object List Setting")]
    public List<SomeSetting>? ObjectListSetting { get; set; }

    [Setting("Enum Setting", Pets.Cat)]
    [ValidValues(typeof(Pets))]
    public Pets EnumSetting { get; set; }

    [Setting("Json Setting")]
    public SomeSetting? JsonSetting { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}

public class SomeSetting
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public int MyInt { get; set; }

    public override bool Equals(object? obj)
    {
        var other = obj as SomeSetting;
        return $"{Key}-{Value}-{MyInt}" == $"{other?.Key}-{other?.Value}-{other?.MyInt}";
    }
}

public enum Pets
{
    Cat,
    Dog,
    Fish
}