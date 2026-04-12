using System.Collections.Generic;

namespace Fig.Contracts.SettingGroups
{
    public class GroupedSettingDataContract
    {
        public GroupedSettingDataContract(string name, string? description, string valueType, List<SourceSettingDataContract> sourceSettings)
        {
            Name = name;
            Description = description;
            ValueType = valueType;
            SourceSettings = sourceSettings;
        }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string ValueType { get; set; }

        public List<SourceSettingDataContract> SourceSettings { get; set; }
    }
}
