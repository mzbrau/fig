namespace Fig.Contracts.ImportExport
{
    public class DeferredImportClient
    {
        public DeferredImportClient(string name, string? instance)
        {
            Name = name;
            Instance = instance;
        }
        
        public string Name { get; }
        
        public string? Instance { get; }
    }
}