namespace Fig.Examples.ConsoleApp;

public interface IConsoleSettings
{
    List<MyClass> DataGridSetting { get; }

    string FavouriteAnimal { get; }

    int FavouriteNumber { get; }

    bool TrueOrFalse { get; }
    event EventHandler SettingsChanged;
}