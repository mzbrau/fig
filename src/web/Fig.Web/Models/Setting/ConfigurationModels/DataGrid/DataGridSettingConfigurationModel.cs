using Fig.Contracts.SettingDefinitions;
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
        DataGridConfiguration = new DataGridConfigurationModel(dataContract.DataGridDefinition);
        Value ??= new List<Dictionary<string, IDataGridValueModel>>();
        OriginalValue ??= new List<Dictionary<string, IDataGridValueModel>>();
        _originalJson = new Lazy<string>(() => JsonConvert.SerializeObject(OriginalValue));
    }

    public override dynamic? GetValue(bool formatAsT = false)
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

    public override void EvaluateDirty()
    {
        var currentJson = JsonConvert.SerializeObject(Value);
        IsDirty = _originalJson.Value != currentJson;
    }

    protected override void EvaluateDirty(dynamic? value)
    {
        // Use the one above instead.
    }

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty)
    {
        return new DataGridSettingConfigurationModel(DefinitionDataContract, parent)
        {
            IsDirty = setDirty
        };
    }
}