using Fig.Contracts.Settings;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionResultDataContract
    {
        public string Name { get; set; } // A name for this specific result, e.g., "Log Output", "Affected Users"
        public CustomActionResultTypeDataContract ResultType { get; set; }
        public string? TextResult { get; set; } // Nullable, used if ResultType is Text
        public DataGridSettingDataContract? DataGridResult { get; set; } // Nullable, used if ResultType is DataGrid. This reuses the existing DataGridSettingDataContract to structure datagrid results
    }
}
