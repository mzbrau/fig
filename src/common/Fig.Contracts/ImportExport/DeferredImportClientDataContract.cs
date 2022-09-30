namespace Fig.Contracts.ImportExport
{
    public class DeferredImportClientDataContract
    {
        public DeferredImportClientDataContract(string name, string? instance, int settingCount, string importingUser)
        {
            Name = name;
            Instance = instance;
            SettingCount = settingCount;
            ImportingUser = importingUser;
        }
        
        public string Name { get; }
        
        public string? Instance { get; }
        
        public int SettingCount { get; }
        
        public string ImportingUser { get; }
    }
}