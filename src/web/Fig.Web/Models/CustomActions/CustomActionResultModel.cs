using Fig.Contracts.CustomActions; // For CustomActionResultDataContract and CustomActionResultTypeDataContract
using Fig.Web.Models.Settings; // For DataGridSettingConfigurationModel (assuming it exists or similar)

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionResultModel
    {
        public string Name { get; set; }
        public CustomActionResultTypeDataContract ResultType { get; set; }
        public string? TextResult { get; set; }
        // Assuming DataGridSettingConfigurationModel is the UI model for DataGridSettingDataContract
        public DataGridSettingConfigurationModel? DataGridResult { get; set; } 

        public CustomActionResultModel(CustomActionResultDataContract contract)
        {
            Name = contract.Name;
            ResultType = contract.ResultType;
            TextResult = contract.TextResult;
            if (contract.DataGridResult != null)
            {
                // This assumes a constructor or mapping logic exists to convert
                // DataGridSettingDataContract to DataGridSettingConfigurationModel.
                // If DataGridSettingConfigurationModel directly wraps DataGridSettingDataContract,
                // or if a direct assignment is possible, adjust accordingly.
                // For now, let's assume a direct mapping or that DataGridSettingConfigurationModel
                // can be constructed from DataGridSettingDataContract.
                // This might require a new model or modification of an existing one if
                // DataGridSettingConfigurationModel is not suitable.
                // For placeholder:
                DataGridResult = new DataGridSettingConfigurationModel(contract.DataGridResult, contract.Name, null);
            }
        }
    }
}
