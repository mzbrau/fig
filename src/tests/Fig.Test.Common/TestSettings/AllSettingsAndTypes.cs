using Fig.Client.Attributes;
using Fig.Client.Enums;

namespace Fig.Test.Common.TestSettings;

public class AllSettingsAndTypes : TestSettingsBase
{
    public override string ClientName => "AllSettingsAndTypes";
    public override string ClientDescription => "Sample settings with all types of settings";


    [Setting("String Setting")]
    public string StringSetting { get; set; } = "Cat";

    [Setting("Int Setting")] 
    public int IntSetting { get; set; } = 34;

    [Setting("Long Setting")] 
    public long LongSetting { get; set; } = 64;

    [Setting("Long Setting")] 
    public double DoubleSetting { get; set; } = 45.3;

    [Setting("Date Time Setting")]
    public DateTime? DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan? TimespanSetting { get; set; }

    [Setting("Bool Setting")] 
    public bool BoolSetting { get; set; } = true;

    [Setting("Common LookupTable Setting")]
    [LookupTable("States", LookupSource.UserDefined)]
    public long LookupTableSetting { get; set; } = 5;

    [Setting("Secret Setting")]
    [Secret]
    public string SecretSetting { get; set; } = "SecretString";

    [Setting("String Collection")]
    public List<string>? StringCollectionSetting { get; set; }

    [Setting("Object List Setting", defaultValueMethodName: nameof(GetDefaultObjectList))] 
    public List<SomeSetting>? ObjectListSetting { get; set; }

    [Setting("Enum Setting")]
    [ValidValues(typeof(Pets))]
    public Pets EnumSetting { get; set; } = Pets.Cat;

    [Setting("Json Setting")]
    public SomeSetting? JsonSetting { get; set; }

    [Setting("Environment Specific Setting")]
    [EnvironmentSpecific]
    public string EnvironmentSpecificSetting { get; set; } = "EnvSpecific";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<SomeSetting> GetDefaultObjectList()
    {
        return new List<SomeSetting>
        {
            new() {Key = "Name", Value = "some val 1", MyInt = 99},
            new() {Key = "Name 2", Value = "some val 2", MyInt = 100}
        };
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

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value, MyInt);
    }
}

public enum Pets
{
    Cat,
    Dog,
    Fish
}