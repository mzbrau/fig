namespace Fig.Integration.ConsoleWebHookHandler;

public interface ISettings
{
    string HashedSecret { get; }
}