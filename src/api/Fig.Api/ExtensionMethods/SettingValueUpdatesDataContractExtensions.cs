using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Json;
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class SettingValueUpdatesDataContractExtensions
{
    public static SettingValueUpdatesDataContract Clone(
        this SettingValueUpdatesDataContract settingValueUpdatesDataContract)
    {
        var json = JsonConvert.SerializeObject(settingValueUpdatesDataContract, JsonSettings.FigDefault);
        return JsonConvert.DeserializeObject<SettingValueUpdatesDataContract>(json, JsonSettings.FigDefault)!;
    }

    public static SettingValueUpdatesDataContract Redact(
        this SettingValueUpdatesDataContract settingValueUpdatesDataContract)
    {
        foreach (var setting in settingValueUpdatesDataContract.ValueUpdates.Where(a => a.IsSecret))
        {
            setting.Value = new StringSettingDataContract("******");
        }
        
        return settingValueUpdatesDataContract;
    }
}