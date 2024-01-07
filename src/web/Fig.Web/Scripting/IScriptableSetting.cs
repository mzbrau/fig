namespace Fig.Web.Scripting;

public interface IScriptableSetting
{
    string Name { get; }
    
    bool IsValid { get; set; }
    
    string ValidationExplanation { get; set; }
    
    bool Advanced { get; set; }

    int? DisplayOrder { get; set; }
    
    bool Hidden { get; }
    
    string CategoryColor { get; set; }
    
    string CategoryName { get; set; }
    
    bool IsReadOnly { get; }
    
    Type ValueType { get; }
    
    int? EditorLineCount { get; set; }

    object? GetValue(bool formatAsT = false);

    void SetValue(object? value);
    
    void SetVisibilityFromScript(bool isVisible);

    void SetReadOnly(bool isReadOnly);
}