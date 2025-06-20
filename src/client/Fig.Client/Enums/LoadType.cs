namespace Fig.Client.Enums;

public enum LoadType
{
    /// <summary>
    /// Settings were not loaded.
    /// </summary>
    None,
    
    /// <summary>
    /// Settings were loaded from the server.
    /// </summary>
    Server,
    
    /// <summary>
    /// Settings were loaded from a saved offline file.
    /// </summary>
    Offline
}