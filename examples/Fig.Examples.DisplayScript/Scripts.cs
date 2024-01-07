namespace Fig.Examples.DisplayScript;

public class Scripts
{
    public const string SelectMode = @"if (Mode.Value == 'Mode A') {
ModeASetting.IsVisible = true;
ModeBSetting1.IsVisible = false;
ModeBSetting2.IsVisible = false;
log('mode A selected')
} else {
ModeASetting.IsVisible = false;
ModeBSetting1.IsVisible = true;
ModeBSetting2.IsVisible = true;
log('mode B selected')
}";

    public const string ValidateSecurity = @"
var prefix = Url1.Value.slice(0, 5);
if (UseSecurity1.Value && prefix.toLowerCase() != 'https') {
Url1.IsValid = false;
Url1.ValidationExplanation = 'If security is used then the url should start with https';
} else if (!UseSecurity1.Value && prefix.toLowerCase() == 'https') {
   Url1.IsValid = false;
   Url1.ValidationExplanation = 'If security is not used then the url should not start with https';
} else {
Url1.IsValid = true;
}";

    public const string AutoUpdateValue = @"
if (UseSecurity2.Value) {
Url2.Value = Url2.Value.replace('http://', 'https://');
log('After: ' + Url2.Value)
} else {
log('Before2: ' + Url2.Value)
Url2.Value = Url2.Value.replace('https://', 'http://');
log('After2: ' + Url2.Value)
}";


    public const string UpdateValidValues = @"
const validGroups = Groups.Value.map(a => a.Values);
for (let item of Services.ValidValues) {
item.Group = validGroups;
}

for (let i = 0; i < Services.Value.length; i++) {
    if (!Services.Value[i].Name) {
        Services.ValidationErrors[i].Name = 'name should not be blank';
    } else {
       Services.ValidationErrors[i].Name = null;
    }
    if (Services.Value[i].ValidationType == 'Custom String') {
        Services.IsReadOnly[i].CustomString = false;
    } else {
        Services.IsReadOnly[i].CustomString = true;
    }
}
";

    public const string ControlOtherSettings = @"

Option.ValidValues = ['Select Option...', 'All Read Only', 'All Writable', 'Line Count 4', 'Category Stuff', 'Category Reset'];
if (ControlledString.Advanced) {
    Option.ValidValues.push('Controlled String Normal');
}
else {
    Option.ValidValues.push('Controlled String Advanced');
}

if (Option.Value == 'All Read Only') {
log('making everthing read only');    
ControlledString.IsReadOnly = true;
    ControlledString.IsReadOnly = true;
    ControlledInt.IsReadOnly = true;
    ControlledBool.IsReadOnly = true;
    ControlledLong.IsReadOnly = true;
    ControlledDouble.IsReadOnly = true;
    ControlledDateTime.IsReadOnly = true;
    ControlledTimeSpan.IsReadOnly = true;
} else if (Option.Value == 'All Writable') {
    ControlledString.IsReadOnly = false;
    ControlledString.IsReadOnly = false;
    ControlledInt.IsReadOnly = false;
    ControlledBool.IsReadOnly = false;
    ControlledLong.IsReadOnly = false;
    ControlledDouble.IsReadOnly = false;
    ControlledDateTime.IsReadOnly = false;
    ControlledTimeSpan.IsReadOnly = false;
} else if (Option.Value == 'Controlled String Advanced') {
    ControlledString.Advanced = true;
} else if (Option.Value == 'Controlled String Normal') {
    ControlledString.Advanced = true;
} else if (Option.Value == 'Line Count 4') {
    ControlledString.EditorLineCount = 4;
} else if (Option.Value == 'Category Stuff') {
    ControlledString.CategoryColor = '#969998';
    ControlledString.CategoryName = 'Stuff';
} else if (Option.Value == 'Category Reset') {
    ControlledString.CategoryColor = '#8a2d69';
    ControlledString.CategoryName = 'Setting Manipulation';
}

if (Option.Value != 'Select Option...') {
   Option.Value = 'Select Option...';
}
";
}