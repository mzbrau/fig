namespace Fig.Integration.ConsoleWebHookHandler.Settings;

public interface ISettings
{
    string HashedSecret { get; }
}