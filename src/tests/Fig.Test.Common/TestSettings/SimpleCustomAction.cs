using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.CustomActions;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging; // Assuming ILogger is available/injectable

namespace Fig.Test.Common.TestSettings
{
    public class SimpleCustomAction : ICustomAction
    {
        private readonly ILogger<SimpleCustomAction>? _logger;

        public string Name => "TestAction";
        public string ButtonName => "Run Test Action";
        public string Description => "A simple test action.";

        // Example of using a simple string setting.
        public IEnumerable<SettingDefinitionDataContract> SettingsUsed => new List<SettingDefinitionDataContract>
        {
            new StringSettingDefinitionDataContract(nameof(MyActionSetting), "Action Specific Setting", defaultValue: "ActionDefault")
        };

        // Constructor for DI, e.g., to get a logger
        public SimpleCustomAction(ILogger<SimpleCustomAction>? logger = null)
        {
            _logger = logger;
        }

        public Task<IEnumerable<CustomActionResultDataContract>> Execute(IEnumerable<SettingDataContract>? settings, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("{ActionName} executed.", Name);

            var actionSettingValue = settings?.FirstOrDefault(s => s.Name == nameof(MyActionSetting))?.Value?.ToString() ?? "Not Provided";

            var resultText = $"Action Executed Successfully. Setting Value: {actionSettingValue}";
            
            var result = new List<CustomActionResultDataContract>
            {
                new CustomActionResultDataContract
                {
                    Name = "ExecutionResult",
                    ResultType = CustomActionResultTypeDataContract.Text,
                    TextResult = resultText
                }
            };
            return Task.FromResult(result.AsEnumerable());
        }
    }

    // To make the setting name strongly typed if needed, though for this example it's simple.
    public class MyActionSetting {} 
}
