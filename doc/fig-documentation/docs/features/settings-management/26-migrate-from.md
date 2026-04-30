---
sidebar_position: 26
---

# Migrate From

`MigrateFromAttribute` lets you rename a setting without losing the value already stored in Fig.

When a client registers updated settings, Fig normally preserves values by matching the setting name. If a setting has been renamed, add `MigrateFrom` to the new property and point it at the previous setting name:

```csharp
[Setting("The new setting name")]
[MigrateFrom("OldSettingName")]
public string NewSettingName { get; set; } = "Default value";
```

During the next updated registration, Fig copies the value from `OldSettingName` into `NewSettingName` if the old setting exists and has the same value type. If the old setting does not exist, Fig keeps the normal default value for the new setting.

## Nested settings

For settings in the same nested settings class, you can use the old C# property name and Fig will resolve it to the nested Fig setting path:

```csharp
public class NestedSettings
{
    [Setting("The renamed nested setting")]
    [MigrateFrom("OldSetting")]
    public string NewSetting { get; set; } = "Default value";
}
```

The example above resolves to `Nested->OldSetting`.

When the old setting is in a different nested class, use the full Fig setting path:

```csharp
[Setting("The renamed nested setting")]
[MigrateFrom("OtherNestedSettings->OldSetting")]
public string NewSetting { get; set; } = "Default value";
```

## Behavior

- Exact name matches take precedence. If the new setting name already exists in Fig, that value is preserved instead of the `MigrateFrom` source.
- The source and target setting types must match. If they do not match, Fig keeps the new default value and logs an error.
- If the source setting still exists in the updated registration, Fig still migrates the value. In DEBUG builds, Fig.Client logs a warning in the source application so developers can remove or correct the ambiguous migration.
- `MigrateFrom` is intended to be temporary. After all environments have registered the renamed setting and migrated the value, remove the attribute.
