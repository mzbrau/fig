namespace Fig.Contracts.Diagnostics;

public static class WebClientLoadTimingStageNames
{
    public const string HttpFetchClients = "HttpFetchClients";
    public const string ConvertToModels = "ConvertToModels";
    public const string BuildGroups = "BuildGroups";
    public const string LinkInstances = "LinkInstances";
    public const string InitializeModels = "InitializeModels";
    public const string LoadClientDescriptions = "LoadClientDescriptions";
}
