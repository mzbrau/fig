﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Contracts.Settings;

namespace Fig.Client.OfflineSettings;

internal interface IOfflineSettingsManager
{
    Task Save(string clientName, string? instance, IEnumerable<SettingDataContract> settings);

    Task<IEnumerable<SettingDataContract>?> Get(string clientName, string? instance);

    void Delete(string clientName, string? instance);
}