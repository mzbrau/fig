using Fig.Client;
using Fig.Client.Attributes;

namespace Fig.Examples.ConsoleApp;

public class ConsoleSettings : SettingsBase, IConsoleSettings
{
    public override string ClientName => "ConsoleApp";
    public override string ClientSecret => "87c20b6a-9159-4daa-a171-9e297f47e08d";

    [Setting("My favourite animal", "Cow")]
    public string FavouriteAnimal { get; set; }

    [Setting("My favourite number", 66)]
    public int FavouriteNumber { get; set; }

    [Setting("True or false, your choice...", false)]
    public bool TrueOrFalse { get; set; }
}