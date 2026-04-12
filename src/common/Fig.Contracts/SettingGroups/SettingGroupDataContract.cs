using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingGroups
{
    public class SettingGroupDataContract
    {
        public SettingGroupDataContract(Guid? id, string name, string? description, List<GroupedSettingDataContract> groupedSettings)
        {
            Id = id;
            Name = name;
            Description = description;
            GroupedSettings = groupedSettings;
        }

        public Guid? Id { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public List<GroupedSettingDataContract> GroupedSettings { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastModifiedAt { get; set; }

        public string? LastModifiedBy { get; set; }
    }
}
