---
sidebar_position: 22
sidebar_label: Externally Managed Settings
---

# Externally Managed Settings

When using Fig with a CICD pipeline, you may want some or all settings to be managed by that pipeline (e.g. stored in git). In this case, you might use the [powershell module](https://github.com/mzbrau/fig/blob/main/scripts/fig-sdk.psm1) to import setting values into Fig so they can be read by the applications.

If these settings are written on every deploy, you won't want someone editing the values within Fig as the value will just be overriden. In this case, you can mark the setting as 'externally managed'. This makes it read only in Fig.

Settings can be marked as externally managed in 4 ways.

## Automatic Detection via Configuration Provider Override

When Fig is used as a configuration provider, other configuration providers (e.g., environment variables, appsettings.json) may override Fig's values. Fig automatically detects when a setting value differs from what Fig provided and marks it as externally managed.

After the application starts, a background service checks if the actual setting values match what Fig provided. If any differences are found, the settings are flagged as externally managed and the overridden values are sent to the API during the next status sync. This detection happens once per application run session.

The externally managed flag is latching - once set, it can only be cleared via a value-only import that explicitly sets `IsExternallyManaged` to `false`. This means if you have multiple application instances and only one is overriding a value, the setting will still be marked as externally managed for all instances.

## Via Value-Only Import - Globally

Add the attribute at the top of the import.

```json
{
  "ExportedAt": "2025-02-07T20:28:16.175812Z",
  "ImportType": 3,
  "Version": 1,
  "IsExternallyManaged": true,
  "Clients": [
    {
      "Name": "AspNetApi",
      "Instance": null,
      "Settings": [
        {
          "Name": "Location",
          "Value": "Adelaide"
        },
        {
          "Name": "MicrosoftLogOverride",
          "Value": "Information"
        }
      ]
    }
  ]
}
```

## Via Value-Only Import - Per Setting

Alternatively you can mark individual settings as being externally managed. This is only supported for value only imports.

```json
{
  "ExportedAt": "2025-02-07T20:28:16.175812Z",
  "ImportType": 3,
  "Version": 1,
  "Clients": [
    {
      "Name": "AspNetApi",
      "Instance": null,
      "Settings": [
        {
          "Name": "Location",
          "Value": "Adelaide",
          "IsExternallyManaged": true
        },
        {
          "Name": "MicrosoftLogOverride",
          "Value": "Information"
        }
      ]
    }
  ]
}
```

## Via Value-Only Import - A Combination of Both

```json
{
  "ExportedAt": "2025-02-07T20:28:16.175812Z",
  "ImportType": 3,
  "Version": 1,
  "IsExternallyManaged": true,
  "Clients": [
    {
      "Name": "AspNetApi",
      "Instance": null,
      "Settings": [
        {
          "Name": "Location",
          "Value": "Adelaide",
          "IsExternallyManaged": false
        },
        {
          "Name": "MicrosoftLogOverride",
          "Value": "Information"
        }
      ]
    }
  ]
}
```

## Client Display

Any setting that is externally managed will be shown read only in the UI.

![alt text](./img/externally-managed-setting.png)

When the user clicks the red padlock, they will be prompted to confirm that they understand the setting is externally managed and then they will be allowed to edit the setting again as normal.

When a change is made to an externally managed setting:

- A warning is shown to the user before save
- An addition event log is added indicating that an externally managed setting was changed.
