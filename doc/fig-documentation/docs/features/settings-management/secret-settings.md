---
sidebar_position: 4
---

# Secret Settings

Fig supports protecting the values of some settings. Secret settings are treated as passwords within the web client and are not shown to the configuring user.

**Note: Secrets are only supported for strings and nullable string types.**

## Usage

```csharp
[Setting("Secret Setting", "SecretString")]
[Secret]
public string SecretSetting { get; set; }
```

### Appearance

![secret-settings](../../../static/img/secret-settings.png)

## Secrets in Data Grids

From Fig 0.11, secrets are supported for individual properties in Data grids (List settings).
Secrets behave in the same way as stand alone secrets including:

- Encryption on exports (and decryptionon imports)
- Redaction in event history and setting history
- No values ever sent down to the web client

However data grid settings do not currently support the [Azuure Key Vault](../azure-keyvault-integration.md) integration.

### Data Grid Appearance

![secret-settings-datagrid](../../../static/img/password-in-datagrid.png)
