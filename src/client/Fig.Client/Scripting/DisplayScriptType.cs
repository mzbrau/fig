namespace Fig.Client.Scripting
{
    public enum DisplayScriptType
    {
        [DisplayScriptDefinition("if (setting.Value !== '{0}') { setting.IsVisible = false; } else { setting.IsVisible = true; }")]
        HideIfNotMatch,
        [DisplayScriptDefinition("if (setting.Value !== '{0}') { setting.IsVisible = true; } else { setting.IsVisible = false; }")]
        ShowIfNotMatch,
        [DisplayScriptDefinition("if (setting.Value === '{0}') { setting.IsVisible = false; } else { setting.IsVisible = true; }")]
        HideIfMatch,
        [DisplayScriptDefinition("if (setting.Value === '{0}') { setting.IsVisible = true; } else { setting.IsVisible = false; }")]
        ShowIfMatch,
        [DisplayScriptDefinition("if (setting.Value === '{0}') { setting.IsReadOnly = true; } else { setting.IsReadOnly = false; }")]
        SetReadOnlyIfMatch,
        [DisplayScriptDefinition("if (setting.Value === '{0}') { setting.IsReadOnly = false; } else { setting.IsReadOnly = true; }")]
        SetReadWriteIfMatch,
        [DisplayScriptDefinition("let val = parseFloat(setting.Value); if (isNaN(val) || val < {0} || val > {1}) { setting.IsValid = false; setting.ValidationExplanation = 'Value must be a number between {0} and {1}.'; } else { setting.IsValid = true; setting.ValidationExplanation = null; }")]
        ValidateRange,
        Custom
    }
}
