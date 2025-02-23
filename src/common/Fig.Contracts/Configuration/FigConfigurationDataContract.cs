using System.Text;

namespace Fig.Contracts.Configuration
{
    public class FigConfigurationDataContract
    {
        public bool AllowNewRegistrations { get; set; }

        public bool AllowUpdatedRegistrations { get; set; }

        public bool AllowFileImports { get; set; }

        public bool AllowOfflineSettings { get; set; }

        public bool AllowClientOverrides { get; set; }
        
        public string? ClientOverridesRegex { get; set; }
        
        public string? WebApplicationBaseAddress { get; set; }
        
        public bool UseAzureKeyVault { get; set; }
    
        public string? AzureKeyVaultName { get; set; }
        
        public double? PollIntervalOverride { get; set; }
        
        public bool AllowDisplayScripts { get; set; }
        
        public bool EnableTimeMachine { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            var properties = GetType().GetProperties();

            foreach (var prop in properties)
            {
                var propName = prop.Name;
                var propValue = prop.GetValue(this);

                // Check for null values and format accordingly
                var propValueStr = propValue != null ? propValue.ToString() : "null";

                sb.AppendLine($"{propName}: {propValueStr}");
            }

            return sb.ToString();
        }
    }
}