namespace Fig.Examples.ConsoleApp;

public interface IConsoleSettings
{

    bool UseService { get; set; }
    
    string? ServiceUsername { get; set; }
    
    string? ServicePassword { get; set; }
    
    int UnrelatedSetting { get; set; }

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
}