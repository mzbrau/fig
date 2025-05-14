using System.Dynamic;
using Fig.Web.Models.Setting.ConfigurationModels;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.Scripting;

public class SettingWrapper
{
    private readonly IScriptableSetting _setting;
    private List<object>? _dataGridValue;
    private List<object>? _dataGridValidValues;
    private List<object>? _dataGridIsReadOnly;
    private List<object>? _dataGridValidationErrors;
    private List<object>? _dataGridEditorLineCount;

    public SettingWrapper(IScriptableSetting setting)
    {
        _setting = setting;
    }

    public string Name => _setting.Name;

    public bool IsValid
    {
        get => _setting.IsValid;
        set => _setting.IsValid = value;
    }

    // Data grid only
    public dynamic? ValidationErrors
    {
        get
        {
            if (_dataGridValidationErrors is not null)
                return _dataGridValidationErrors;
            
            if (_setting is DataGridSettingConfigurationModel dataGridSetting)
            {
                _dataGridValidationErrors = ConvertValidationsToObjectList(dataGridSetting);
                return _dataGridValidationErrors?.ToArray();
            }

            return null;
        }

        set
        {
            if (value is IEnumerable<object> rows && _setting is DataGridSettingConfigurationModel)
            {
                List<string> validationErrors = new();
                ProcessUpdates(rows, (property, setting, _, index) =>
                {
                    if (property.Value is string validationError)
                    {
                        var values = setting.GetValue(true) as List<Dictionary<string, IDataGridValueModel?>>;
                        var value = values![index][property.Key]?.ReadOnlyValue;
                        validationErrors.Add($"[{property.Key} - {value}] {validationError}");
                    }
                });
                
                if (validationErrors.Any())
                {
                    IsValid = false;
                    var additionalErrorsMessage =
                        validationErrors.Count > 1 ? $" (and {validationErrors.Count - 1} other error(s))" : string.Empty;

                    ValidationExplanation = $"{validationErrors.First()}{additionalErrorsMessage}";
                }
                else
                {
                    IsValid = true;
                }
            }
        }
    }

    public string ValidationExplanation
    {
        get => _setting.ValidationExplanation;
        set => _setting.ValidationExplanation = value;
    }

    public bool Advanced
    {
        get => _setting.Advanced;
        set => _setting.Advanced = value;
    }

    public dynamic? EditorLineCount
    {
        get
        {
            if (_dataGridEditorLineCount is not null)
                return _dataGridEditorLineCount;
            
            if (_setting is DataGridSettingConfigurationModel dataGridSetting)
            {
                _dataGridEditorLineCount = ConvertToObjectList(dataGridSetting.GetValue(true) as List<Dictionary<string, IDataGridValueModel?>>, model => model.EditorLineCount);
                if (_dataGridEditorLineCount is null)
                    return false;
                
                return _dataGridEditorLineCount?.ToArray();
            }

            return _setting.EditorLineCount;
        }
        set
        {
            if (value is IEnumerable<object> rows && _setting is DataGridSettingConfigurationModel)
            {
                ProcessUpdates(rows, (property, setting, _, index) =>
                {
                    if (property.Value is double editorLineCount)
                    {
                        setting.Value![index][property.Key].EditorLineCount = (int)editorLineCount;
                    }
                    else
                    {
                        setting.Value![index][property.Key].EditorLineCount = null;
                    }
                });
            }
            else if (value is double doubleValue)
            {
                _setting.EditorLineCount = (int)doubleValue;
            }
            else if (value is null)
            {
                _setting.EditorLineCount = null;
            }
        }
    }
    
    public int? DisplayOrder
    {
        get => _setting.DisplayOrder;
        set => _setting.DisplayOrder = value;
    }
    
    public bool IsVisible
    {
        get => !_setting.Hidden;
        set => _setting.SetVisibilityFromScript(value);
    }
    
    public string CategoryColor
    {
        get => _setting.CategoryColor;
        set => _setting.CategoryColor = value;
    }
    
    public string CategoryName
    {
        get => _setting.CategoryName;
        set => _setting.CategoryName = value;
    }

    public dynamic? ValidValues
    {
        get
        {
            if (_dataGridValidValues is not null)
                return _dataGridValidValues;
            
            if (_setting is DropDownSettingConfigurationModel dropDownSetting)
            {
                return dropDownSetting.ValidValues.ToArray();
            }
            
            if (_setting is DataGridSettingConfigurationModel dataGridSetting)
            {
                _dataGridValidValues = ConvertToObjectList(dataGridSetting.GetValue(true) as List<Dictionary<string, IDataGridValueModel?>>, model => model.ValidValues);
                return _dataGridValidValues?.ToArray();
            }

            return null;
        }
        set
        {
            if (value is object[] valArray && _setting is DropDownSettingConfigurationModel dropDownSetting)
            {
                dropDownSetting.ValidValues = valArray
                    .Select(a => a.ToString())
                    .Where(a => a is not null)
                    .ToList()!;
            }

            if (value is IEnumerable<object> rows && _setting is DataGridSettingConfigurationModel)
            {
                ProcessUpdates(rows, (property, setting, _, index) =>
                {
                    if (property.Value is IEnumerable<object> values)
                    {
                        setting.Value![index][property.Key].ValidValues = values
                            .Select(a => a.ToString())
                            .ToList()!;
                    }
                });
            }
        }
    }

    public dynamic IsReadOnly
    {
        get
        {
            if (_dataGridIsReadOnly is not null)
                return _dataGridIsReadOnly;
            
            if (_setting is DataGridSettingConfigurationModel dataGridSetting)
            {
                _dataGridIsReadOnly = ConvertToObjectList(dataGridSetting.GetValue(true) as List<Dictionary<string, IDataGridValueModel?>>, model => model.IsReadOnly);
                if (_dataGridIsReadOnly is null)
                    return false;
                
                return _dataGridIsReadOnly.ToArray();
            }

            return _setting.IsReadOnly;
        }
        set
        {
            if (value is IEnumerable<object> rows && _setting is DataGridSettingConfigurationModel)
            {
                ProcessUpdates(rows, (property, setting, _, index) =>
                {
                    if (property.Value is bool isReadOnly)
                    {
                        setting.Value![index][property.Key].IsReadOnly = isReadOnly;
                    }
                });
            }
            else if (value is bool isReadOnly)
            {
                _setting.SetReadOnly(isReadOnly);
            }
        }
    }
    
    public dynamic? Value
    {
        get
        {
            if (_dataGridValue is not null)
                return _dataGridValue;

            if (_setting is TimeSpanSettingConfigurationModel)
            {
                return ((TimeSpan?)_setting.GetValue(true))?.TotalMilliseconds;
            }

            if (_setting is DataGridSettingConfigurationModel)
            {
                _dataGridValue = ConvertToObjectList(_setting.GetValue(true) as List<Dictionary<string, IDataGridValueModel?>>, model => model.ReadOnlyValue);
                return _dataGridValue?.ToArray();
            }
            
            return _setting.GetValue();
        }
        set
        {
            var theValue = value;
            if (_setting is TimeSpanSettingConfigurationModel && value is double d)
            {
                theValue = TimeSpan.FromMilliseconds(d);
            }
            else if (value is IEnumerable<object> rows && _setting is DataGridSettingConfigurationModel)
            {
                ProcessUpdates(rows, (property, setting, columns, index) =>
                {
                    var column = columns?.FirstOrDefault(a => a.Name == property.Key);
                    var targetType = Nullable.GetUnderlyingType(column?.Type ?? typeof(string)) ?? column?.Type ?? typeof(string);
                    var typedValue = Convert.ChangeType(property.Value, targetType);
                    setting.Value![index][property.Key].SetValue(typedValue);
                });

                return;
            }
            
            var underlyingType = Nullable.GetUnderlyingType(_setting.ValueType);
            var type = underlyingType ?? _setting.ValueType;
            var typedValue = Convert.ChangeType(theValue, type);
            _setting.SetValue(typedValue);
        }
    }

    public void ApplyChangesToDataGrid()
    {
        if (_dataGridValue is not null)
            Value = _dataGridValue;

        if (_dataGridValidValues is not null)
            ValidValues = _dataGridValidValues;
        
        if (_dataGridIsReadOnly is not null)
            IsReadOnly = _dataGridIsReadOnly;

        if (_dataGridValidationErrors is not null)
            ValidationErrors = _dataGridValidationErrors;

        if (_dataGridEditorLineCount is not null)
            EditorLineCount = _dataGridEditorLineCount;
    }
    
    private List<dynamic>? ConvertToObjectList(List<Dictionary<string, IDataGridValueModel?>>? sourceCollection, Func<IDataGridValueModel, object?> getValue)
    {
        if (sourceCollection is null)
            return null;

        var propertyNames = sourceCollection.SelectMany(dict => dict.Keys).Distinct().ToList();
        var resultCollection = sourceCollection.Select(item =>
        {
            var dynamicObject = new ExpandoObject();

            foreach (var propertyName in propertyNames)
            {
                var value = getValue(item.GetValueOrDefault(propertyName, null)!);
                (dynamicObject as IDictionary<string, object?>)[propertyName] = value;
            }

            return dynamicObject;
        }).ToList<dynamic>();
        
        return resultCollection;
    }
    
    private List<dynamic>? ConvertValidationsToObjectList(DataGridSettingConfigurationModel setting)
    {
        var sourceCollection = setting.GetValue(true) as List<Dictionary<string, IDataGridValueModel?>>;
        
        if (sourceCollection is null)
            return null;

        Dictionary<string, string?> validationErrors = new();
        setting.ValidateDataGrid((row, column, explanation) =>
        {
            validationErrors.Add($"{row}:{column}", explanation);
        });

        var propertyNames = sourceCollection.SelectMany(dict => dict.Keys).Distinct().ToList();

        List<object> result = new();
        for (int i = 0; i < sourceCollection.Count; i++)
        {
            var dynamicObject = new ExpandoObject();
            
            foreach (var propertyName in propertyNames)
            {
                string? validationError = null;
                if (validationErrors.ContainsKey($"{i}:{propertyName}"))
                    validationError = validationErrors[$"{i}:{propertyName}"];

                (dynamicObject as IDictionary<string, object?>)[propertyName] = validationError;
            }

            result.Add(dynamicObject);
        }

        return result;
    }

    private void ProcessUpdates(IEnumerable<object> rows, Action<KeyValuePair<string, object?>, DataGridSettingConfigurationModel, List<DataGridColumn>?, int> applyUpdate)
    {
        var rowsToProcess = rows.ToList();
        var dataGridSetting = _setting as DataGridSettingConfigurationModel;
        var columns = dataGridSetting?.DataGridConfiguration?.Columns;
        if (dataGridSetting is null || columns is null)
            return;
        
        for (int i = 0; i < rowsToProcess.Count(); i++)
        {
            foreach (var property in (IDictionary<string, object?>)rowsToProcess[i])
            {
                applyUpdate(property!, dataGridSetting, columns, i);
            }
        }
    }
}