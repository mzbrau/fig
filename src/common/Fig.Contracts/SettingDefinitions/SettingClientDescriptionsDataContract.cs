using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions;

public class SettingClientDescriptionsDataContract
{
    public SettingClientDescriptionsDataContract(List<SettingClientDescriptionDataContract> descriptions)
    {
        Descriptions = descriptions;
    }

    public List<SettingClientDescriptionDataContract> Descriptions { get; }
}