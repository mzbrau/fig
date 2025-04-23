using System;
using Fig.Contracts.Settings;
using Fig.Web.ExtensionMethods;
using Humanizer;

namespace Fig.Web.Models.Scheduling;

public class DeferredChangeModel
{
    private DateTime _originalTime;
    public DeferredChangeModel(Guid id,
        DateTime executeAtUtc,
        string requestingUser,
        string clientName,
        string? instance,
        SettingValueUpdatesDataContract? changeSet)
    {
        Id = id;
        ExecuteAtUtc = executeAtUtc;
        RequestingUser = requestingUser;
        ClientName = clientName;
        Instance = instance;
        ChangeSet = changeSet;
        _originalTime = executeAtUtc;
        Message = changeSet?.ChangeMessage;
        Changes = BuildChangeString(changeSet);
    }

    public Guid Id { get; set; }
    
    public DateTime ExecuteAtUtc { get; set; }

    public DateTime ExecuteAtLocal
    {
        get => ExecuteAtUtc.ToLocalTime();
        set => ExecuteAtUtc = value.ToUniversalTime();
    }

    public string ExecuteAtHuman => ExecuteAtLocal.Humanize();
    
    public string RequestingUser { get; set; } = string.Empty;
    
    public string ClientName { get; set; } = string.Empty;
    
    public string? Instance { get; set; }
    
    public string? Changes { get; set; }
    
    public string? Message { get; set; }
    
    public SettingValueUpdatesDataContract? ChangeSet { get; set; }

    public void Revert()
    {
        ExecuteAtUtc = _originalTime;
    }

    public void Save()
    {
        _originalTime = ExecuteAtUtc;
    }
    
    private string? BuildChangeString(SettingValueUpdatesDataContract? changeSet)
    {
        return changeSet == null
            ? null
            : string.Join(Environment.NewLine,
                changeSet.ValueUpdates.Select(x => $"{x.Name}: {GetStringValue(x.Value?.GetValue())}"));
    }
    
    private string GetStringValue(object? value)
    {
        if (value is List<Dictionary<string, object?>> dataGrid)
        {
            return dataGrid.ToDataGridStringValue(5, false);
        }
        
        return value?.ToString() ?? string.Empty;
    }
}
