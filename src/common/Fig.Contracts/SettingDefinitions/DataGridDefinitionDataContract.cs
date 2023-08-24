using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridDefinitionDataContract
    {
        public DataGridDefinitionDataContract(List<DataGridColumnDataContract> columns, bool isLocked)
        {
            Columns = columns;
            IsLocked = isLocked;
        }
        
        public List<DataGridColumnDataContract> Columns { get; }

        public bool IsLocked { get; set; }
    }
}