using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Fig.Client;

public interface ISettingUpdater
{
    ISettingUpdater<TSettings> For<TSettings>() where TSettings : SettingsBase;
}

public interface ISettingUpdater<TSettings> where TSettings : SettingsBase
{
    ISettingUpdater<TSettings> Set<TValue>(Expression<Func<TSettings, TValue>> setting, TValue value);

    ISettingUpdater<TSettings> WithMessage(string changeMessage);

    Task ApplyAsync();
}
