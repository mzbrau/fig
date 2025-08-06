using System;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing.Scripts;

/// <summary>
/// A test setting implementation for testing display scripts
/// </summary>
public class TestSetting : IScriptableSetting
{
    private object? _value;
    private bool _hidden;
    private bool _isReadOnly;

    public TestSetting(string name, Type valueType, object? initialValue = null)
    {
        Name = name;
        ValueType = valueType;
        _value = initialValue;
        ValidationExplanation = string.Empty;
        CategoryColor = string.Empty;
        CategoryName = string.Empty;
    }

    public string Name { get; }
    
    public bool IsValid { get; set; } = true;
    
    public string? ValidationExplanation { get; set; }
    
    public bool Advanced { get; set; }
    
    public int? DisplayOrder { get; set; }
    
    public bool Hidden => _hidden;
    
    public bool IsVisible => !_hidden;
    
    public string CategoryColor { get; set; }
    
    public string CategoryName { get; set; }
    
    public bool IsReadOnly => _isReadOnly;
    
    public Type ValueType { get; }
    
    public int? EditorLineCount { get; set; }

    public virtual object? GetValue(bool formatAsT = false) => _value;

    public void SetValue(object? value) => _value = value;

    public void SetVisibilityFromScript(bool isVisible) => _hidden = !isVisible;

    public void SetReadOnly(bool isReadOnly) => _isReadOnly = isReadOnly;
}
