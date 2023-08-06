using System.Text;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Newtonsoft.Json;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class
    DataGridSettingConfigurationModel : SettingConfigurationModel<List<Dictionary<string, IDataGridValueModel>>>
{
    private readonly Lazy<string> _originalJson;

    public DataGridSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent)
        : base(dataContract, parent)
    {
        DataGridConfiguration = new DataGridConfigurationModel(dataContract.DataGridDefinition!);
        Value ??= new List<Dictionary<string, IDataGridValueModel>>();
        OriginalValue ??= new List<Dictionary<string, IDataGridValueModel>>();
        _originalJson = new Lazy<string>(() => JsonConvert.SerializeObject(OriginalValue, JsonSettings.FigDefault));
    }

    protected override object? GetValue(bool formatAsT = false)
    {
        if (formatAsT)
            return Value;

        var result = new List<Dictionary<string, object?>>();

        if (Value == null)
            return result;

        foreach (var row in Value)
        {
            var column = row.ToDictionary(
                a => a.Key,
                b => b.Value.ReadOnlyValue);
            result.Add(column);
        }

        return result;
    }

    public override string GetStringValue()
    {
        var rows = GetValue() as List<Dictionary<string, object?>>;
        
        if (rows is null || !rows.Any())
            return "<NOT SET>";

        var builder = new StringBuilder();
        foreach (var row in rows.Take(3))
        {
            builder.AppendLine(string.Join(",", row.Values));
        }

        if (rows.Count > 3)
            builder.AppendLine($"--{rows.Count - 3} more row(s) not shown--");

        return builder.ToString();
    }

    public override void EvaluateDirty()
    {
        var currentJson = JsonConvert.SerializeObject(Value, JsonSettings.FigDefault);
        IsDirty = _originalJson.Value != currentJson;
    }
    
    protected override void EvaluateDirty(List<Dictionary<string, IDataGridValueModel>>? value)
    {
        // Use the one above instead.
    }

    public override SettingValueBaseDataContract? GetValueDataContract()
    {
        if (Value == null)
            return new DataGridSettingDataContract(null);

        var result = Value.Select(item => 
            item.ToDictionary(val => 
                val.Key, val => 
                val.Value.ReadOnlyValue))
            .ToList();
        
        return ValueDataContractFactory.CreateContract(result, typeof(List<Dictionary<string, object?>>));
    }

    

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        return new DataGridSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };
    }
}