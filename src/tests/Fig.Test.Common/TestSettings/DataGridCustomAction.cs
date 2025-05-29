using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.CustomActions;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings; // For DataGridSettingDataContract and DataGridColumnDataContract
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings
{
    public class DataGridCustomAction : ICustomAction
    {
        private readonly ILogger<DataGridCustomAction>? _logger;

        public string Name => "DataGridAction";
        public string ButtonName => "Run DataGrid Action";
        public string Description => "Action returning a datagrid.";
        public IEnumerable<SettingDefinitionDataContract> SettingsUsed => Enumerable.Empty<SettingDefinitionDataContract>();

        public DataGridCustomAction(ILogger<DataGridCustomAction>? logger = null)
        {
            _logger = logger;
        }

        public Task<IEnumerable<CustomActionResultDataContract>> Execute(IEnumerable<SettingDataContract>? settings, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("{ActionName} executed.", Name);

            var dataGridResult = new DataGridSettingDataContract(new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?> { { "ID", 1 }, { "Value", "A" } },
                    new Dictionary<string, object?> { { "ID", 2 }, { "Value", "B" } }
                },
                new List<DataGridColumnDataContract>
                {
                    new DataGridColumnDataContract { Name = "ID", Type = typeof(int).FullName! },
                    new DataGridColumnDataContract { Name = "Value", Type = typeof(string).FullName! }
                });
            
            var result = new List<CustomActionResultDataContract>
            {
                new CustomActionResultDataContract
                {
                    Name = "DataGridOutput",
                    ResultType = CustomActionResultTypeDataContract.DataGrid,
                    DataGridResult = dataGridResult
                }
            };
            return Task.FromResult(result.AsEnumerable());
        }
    }
}
