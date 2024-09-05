namespace Fig.Client.Versions;

internal interface IVersionProvider
{
    string GetFigVersion();

    string GetHostVersion();
}