using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Exceptions;
using Fig.Contracts.Settings;

namespace Fig.Client;

public class SettingUpdater<TSettings> : ISettingUpdater<TSettings> where TSettings : SettingsBase
{
    private const string DefaultChangeMessage = "Client self-update from application";
    private readonly List<SettingDataContract> _updates = [];
    private string? _changeMessage;

    public ISettingUpdater<TSettings> Set<TValue>(Expression<Func<TSettings, TValue>> setting, TValue value)
    {
        var update = SettingUpdateContractFactory.Create(setting, value);
        _updates.RemoveAll(a => a.Name == update.Name);
        _updates.Add(update);
        return this;
    }

    public ISettingUpdater<TSettings> WithMessage(string changeMessage)
    {
        if (string.IsNullOrWhiteSpace(changeMessage))
            throw new ArgumentException("Change message cannot be empty.", nameof(changeMessage));

        _changeMessage = changeMessage;
        return this;
    }

    public async Task ApplyAsync()
    {
        if (!_updates.Any())
            throw new InvalidOperationException("At least one setting update must be supplied.");

        if (!FigClientBridgeRegistry.TryGet(typeof(TSettings), out var bridge, out _))
            throw new ConfigurationException(
                $"Fig has not been initialized for settings type {typeof(TSettings).FullName}.");

        var updates = new SettingValueUpdatesDataContract(_updates.ToList(), _changeMessage ?? DefaultChangeMessage);
        await bridge!.UpdateSettings(updates).ConfigureAwait(false);
        _updates.Clear();
        _changeMessage = null;
    }
}
