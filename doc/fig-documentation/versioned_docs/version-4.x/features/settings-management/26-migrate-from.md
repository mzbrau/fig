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

During the next updated registration, Fig copies the value from `OldSettingName` into `NewSettingName` if the old setting exists and has the same value type. It also moves the old setting's value history to `NewSettingName` and adds a history entry that records the rename and the migrated value. If the old setting does not exist, Fig keeps the normal default value for the new setting.

## Type-changing migrations

If the old and new settings use different value types, add a migration method name to `MigrateFrom`. The method is a `public static` method on the settings class. It runs in your application during registration, not in the Fig API.

```csharp
[Setting("Request timeout")]
[MigrateFrom("TimeoutSeconds", migrationMethodName: nameof(MigrateTimeout))]
public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

public static TimeSpan MigrateTimeout(int timeoutSeconds)
{
    return TimeSpan.FromSeconds(timeoutSeconds);
}
```

The method must have exactly one parameter and return a value compatible with the new setting type. Use `nameof(...)` so refactoring tools keep the attribute in sync.

Migration methods can also reshape values into complex settings:

```csharp
[Setting("Endpoint config")]
[MigrateFrom("LegacyEndpoint", migrationMethodName: nameof(MigrateEndpoint))]
public EndpointConfig EndpointConfig { get; set; } = new();

public static EndpointConfig MigrateEndpoint(string legacyEndpoint)
{
    return new EndpointConfig
    {
        Routes = [new EndpointRoute { Url = legacyEndpoint, Enabled = true }]
    };
}
```

If the source setting is a `List<T>` and the target is something else, Fig passes the source value to your migration method as `List<Dictionary<string, object>>`. Each dictionary represents one row from the stored list/data-grid value.

- For `List<string>`, each row uses the key `Values`.
- For lists of custom objects, the keys match the generated column/property names.

```csharp
[Setting("Allowed regions summary")]
[MigrateFrom("AllowedRegions", migrationMethodName: nameof(MigrateRegions))]
public string AllowedRegionsSummary { get; set; } = string.Empty;

public static string MigrateRegions(List<Dictionary<string, object>> items)
{
    return string.Join(", ", items
        .Select(row => row.TryGetValue("Values", out var value) ? value?.ToString() : null)
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}
```

Custom migration methods are intentionally client-side only. The Fig API sends the old stored value to the registering client, the client runs the static method locally, and the API validates and stores the converted value. Fig never accepts or executes scripts, delegates, or method bodies from clients.

## Imports

`MigrateFrom` also applies to value-only imports. If an import file contains the old setting name and the registered client only contains the renamed setting, Fig applies the imported value to the renamed setting:

```json
{
  "Name": "OldSettingName",
  "Value": "Imported value"
}
```

With `[MigrateFrom("OldSettingName")]` on `NewSettingName`, the value above is imported into `NewSettingName`.

When this happens, Fig logs a warning and shows import feedback telling the user to update the import file to use the new setting name. The import still succeeds. Deferred value-only imports use the same migration behavior when they are applied during client registration, but only log the warning at that time.

Custom migration methods do not run for imports because imports are processed by the Fig API and the API does not execute application code. If an old-name import entry requires a custom migration method, Fig reports that the entry could not be applied and tells the user to update or externally migrate the import file to the new setting name and value shape.

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
- For value-only imports, exact current-name matches also take precedence. If an import file contains both the old and new setting names, Fig applies the new setting name and warns that the old setting entry should be removed.
- Without a custom migration method, the source and target setting types must match. If they do not match, Fig keeps the new default value and logs an error.
- If a custom migration method is supplied, it runs whenever the source setting exists and the target setting does not already have an exact-name match, even if the source and target types are the same.
- When the source setting is a `List<T>` and the target is a different shape, the migration method parameter should be `List<Dictionary<string, object>>`.
- Custom migrations from a secret source setting to a non-secret target setting are blocked to avoid downgrading protected data into a visible setting.
- If the source setting still exists in the updated registration, Fig still migrates the value. In DEBUG builds, Fig.Client logs a warning in the source application so developers can remove or correct the ambiguous migration.
- When migration happens during registration, Fig rewrites the old setting's value history to the new setting name and adds a one-time history row describing the rename. This history stays with the new setting even after `MigrateFrom` is removed later.
- `MigrateFrom` is intended to be temporary. After all environments have registered the renamed setting and migrated the value, remove the attribute.
