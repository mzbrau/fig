namespace Fig.Examples.ConsoleApp;

public interface IConsoleSettings
{
    List<string> Items { get; set; }

    List<int> IntItems { get; set; }

    /*string? Pets { get; set; }

    string? Fish { get; set; }

    int AustralianAnimals { get; set; }

    int SwedishAnimals { get; set; }*/

    // List<MyClass> DataGridSetting { get; }
    //
    // string FavouriteAnimal { get; }
    //
    // int FavouriteNumber { get; }
    //
    // bool TrueOrFalse { get; }
    event EventHandler SettingsChanged;

    event EventHandler RestartRequested;

    void SetConfigurationErrorStatus(bool hasConfigError);
}