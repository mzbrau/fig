using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class DataGridDefinitionDataContract
    {
        public DataGridDefinitionDataContract(List<DataGridColumnDataContract> columns)
        {
            Columns = columns;
        }
        
        public List<DataGridColumnDataContract> Columns { get; }
    }
}