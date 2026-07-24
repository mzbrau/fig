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
        
        public int TimelineDurationDays { get; set; } = 30;

        public int? TimeMachineCleanupDays { get; set; }
        
        public int? EventLogsCleanupDays { get; set; }
        
        public int? ApiStatusCleanupDays { get; set; }
        
        public int? SettingHistoryCleanupDays { get; set; }

        public bool AllowMigrateFromMigrations { get; set; } = true;

        public bool EnableFigAssistant { get; set; }

        public string? FigAssistantEndpoint { get; set; }

        public string? FigAssistantModel { get; set; }

        /// <summary>
        /// Access token for the configured LLM. On GET this is a placeholder when a token is stored.
        /// </summary>
        public string? FigAssistantAccessToken { get; set; }

        public int FigAssistantMaxToolIterations { get; set; } = 12;

        public int FigAssistantRequestTimeoutSeconds { get; set; } = 120;

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