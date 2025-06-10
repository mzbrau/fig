namespace Fig.Contracts.SettingClients
{
    public class ClientDescriptionDataContract
    {
        public ClientDescriptionDataContract(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        
        public string Description { get; }
    }
}
