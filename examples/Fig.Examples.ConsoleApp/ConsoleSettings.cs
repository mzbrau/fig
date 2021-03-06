using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.SettingVerification;

namespace Fig.Examples.ConsoleApp;

[Verification("Check Website", "Runs a website check", typeof(CheckWebsiteVerification), TargetRuntime.Dotnet6)]
public class ConsoleSettings : SettingsBase, IConsoleSettings
{
    public override string ClientName => "ConsoleApp";

    [ValidValues("A", "B", "C")]
    [Setting("Some items")]
    public List<string> Items { get; set; }

    [ValidValues("1", "2", "3")]
    [Setting("Some int items")]
    public List<int> IntItems { get; set; }

    // [Setting("A data grid setting")]
    // public List<MyClass> DataGridSetting { get; set; }
    //
    // [Setting("The address of the website", "http://www.google.com")]
    // public string WebsiteAddress { get; set; }
    //
    // [Setting("My favourite animal", "Cow")]
    // [Secret]
    // public string FavouriteAnimal { get; set; }
    //
    // [Setting("My favourite number", 66)]
    // public int FavouriteNumber { get; set; }
    //
    // [Setting("True or false, your choice...", false)]
    // public bool TrueOrFalse { get; set; }

    /*[Setting("Enum value", "Cat")]
    [ValidValues(typeof(Animals))]
    public string? Pets { get; set; }

    [Setting("Enum value", "Shark")]
    //[ValidValues("Shark", "Whale", "Salmon")]
    [Secret]
    public string? Fish { get; set; }

    [Setting("Enum value", 1)]
    [CommonEnumeration("Animals2")]
    public int AustralianAnimals { get; set; }

    [Setting("Enum value", 1)]
    [ValidValues("1 -> Moose", "2 -> Tick", "4 -> Deer")]
    public int SwedishAnimals { get; set; }*/
}

public class MyClass
{
    public string Name { get; set; }

    public bool IsCool { get; set; }

    public int FavNum { get; set; }
}

public enum Animals
{
    Cat,
    Dog,
    Horse
}