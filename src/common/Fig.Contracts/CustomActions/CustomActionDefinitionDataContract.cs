using ClassificationType = Fig.Client.Abstractions.Data.Classification;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionDefinitionDataContract
    {
        public CustomActionDefinitionDataContract()
            : this(string.Empty, string.Empty, string.Empty, string.Empty, ClassificationType.Technical)
        {
        }

        public CustomActionDefinitionDataContract(
            string name,
            string buttonName,
            string description,
            string settingsUsed)
            : this(name, buttonName, description, settingsUsed, ClassificationType.Technical)
        {
        }

        public CustomActionDefinitionDataContract(
            string name,
            string buttonName,
            string description,
            string settingsUsed,
            ClassificationType classification)
        {
            Name = name;
            ButtonName = buttonName;
            Description = description;
            SettingsUsed = settingsUsed;
            Classification = classification;
        }

        public string Name { get; set; }
        public string ButtonName { get; set; }
        public string Description { get; set; }
        public string SettingsUsed { get; set; }
        public ClassificationType Classification { get; set; } = ClassificationType.Technical;
    }
}
