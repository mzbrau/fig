namespace Fig.Client.Versions
{
    public interface IVersionProvider
    {
        string GetFigVersion();

        string GetHostVersion();
    }
}