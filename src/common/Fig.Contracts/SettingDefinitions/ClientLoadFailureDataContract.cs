namespace Fig.Contracts.SettingDefinitions
{
    public class ClientLoadFailureDataContract
    {
        public ClientLoadFailureDataContract(string clientName, string? instance, string? settingName, string message)
        {
            ClientName = clientName;
            Instance = instance;
            SettingName = settingName;
            Message = message;
        }

        public string ClientName { get; }

        public string? Instance { get; }

        public string? SettingName { get; }

        public string Message { get; }
    }
}
