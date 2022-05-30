using System;
using System.Collections.Generic;
using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Integration.Test.Api.TestSettings;

public class AllSettingsAndTypes : SettingsBase
{
    public override string ClientName => "AllSettingsAndTypes";


    [Setting("String Setting", "Cat")]
    public string StringSetting { get; set; }

    [Setting("Int Setting", 34)]
    public int IntSetting { get; set; }

    [Setting("Long Setting", 64)]
    public long LongSetting { get; set; }

    [Setting("Date Time Setting")]
    public DateTime? DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan? TimespanSetting { get; set; }

    [Setting("Bool Setting", true)]
    public bool BoolSetting { get; set; }

    [Setting("Common Enumeration Setting", 5)]
    [CommonEnumeration("States")]
    public long CommonEnumerationSetting { get; set; }

    [Setting("Secret Setting", "SecretString")]
    [Secret]
    public string SecretSetting { get; set; }

    [Setting("String Collection")]
    public List<string>? StringCollectionSetting { get; set; }

    [Setting("Key Value Pair Setting")]
    public List<KeyValuePair<string, string>>? KvpCollectionSetting { get; set; }

    [Setting("Object List Setting")]
    public List<SomeSetting>? ObjectListSetting { get; set; }
}

public class SomeSetting
{
    public string Key { get; set; }

    public string Value { get; set; }
}