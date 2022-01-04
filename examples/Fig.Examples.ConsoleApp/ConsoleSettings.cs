using System.ComponentModel;
using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.ConsoleApp;

public class ConsoleSettings : SettingsBase, IConsoleSettings
{
    public override string ClientName => "ConsoleApp";
    public override string ClientSecret => "87c20b6a-9159-4daa-a171-9e297f47e08d";
    
    [Setting]
    [SettingDescription("My favourite animal")]
    [Client.Attributes.DefaultValue("Cow")]
    public string FavouriteAnimal { get; set; }
    
    [Setting]
    [SettingDescription("My favourite number")]
    [Client.Attributes.DefaultValue(66)]
    public int FavouriteNumber { get; set; }
    
    [Setting]
    [SettingDescription("True or false, your choice...")]
    public bool TrueOrFalse { get; set; }
}