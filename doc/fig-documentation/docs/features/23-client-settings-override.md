---
sidebar_position: 23
sidebar_label: Client Settings Overrides
---

# Client Settings Overrides

In some situations you want the settings to be driven by the client. For example when deploying applications using Docker compose if you have address to other containers within the compose file, you can reference them by their container name. In that case, you would like the setting to be set by the docker compose file rather than having to be manually set in Fig or use a default.

In this case, we can use the client settings override feature. Fig reads environment variables using the same naming convention as the .NET environment variable configuration provider. The Fig client library will read matching environment variables on startup and send them along with the registration information. If client overrides are enabled on the API, those values will be used to update the setting value.

## Environment Variable Naming Convention

Fig uses the standard .NET environment variable naming convention with `__` as the delimiter for configuration sections:

### Simple Settings

For simple settings, use the setting name directly:

```yaml
environment:
  - WebsiteAddress=http://bing.com
```

### Configuration Section Overrides

If your setting uses a `ConfigurationSectionOverride` attribute, the environment variable should match the full configuration path:

```c#
[Setting("The minimum log level", "Information")]
[ConfigurationSectionOverride("Serilog:MinimumLevel", "Default")]
public string MinLogLevel { get; set; }
```

The environment variable would be:

```yaml
environment:
  - Serilog__MinimumLevel__Default=Warning
```

### Nested Settings

For nested settings (using `NestedSettingAttribute`), use the path with `__` as separator:

```c#
[NestedSetting]
public School School { get; set; }

// In School class:
[Setting("School name", "Default")]
public string Name { get; set; }
```

The environment variable would be:

```yaml
environment:
  - School__Name=Oxford
```

### List Settings

For `List<string>` settings, use indexed environment variables:

```yaml
environment:
  - StringList__0=FirstItem
  - StringList__1=SecondItem
  - StringList__2=ThirdItem
```

For lists of complex objects (`List<T>`), include the property name after the index:

```yaml
environment:
  - ComplexList__0__Name=First
  - ComplexList__0__Value=100
  - ComplexList__1__Name=Second
  - ComplexList__1__Value=200
```

### Complex Object Settings (JSON)

For single complex object settings (serialized as JSON), use the property names:

```yaml
environment:
  - ComplexObject__StringVal=MyString
  - ComplexObject__IntVal=42
```

:::warning Limitation

When overriding complex object (JSON) settings via environment variables, all property values are treated as strings in the resulting JSON. This means numeric and boolean properties will be serialized as string values (e.g., `"IntVal":"42"` instead of `"IntVal":42`).

If your application requires strict JSON type validation, consider using list settings or simple typed settings instead.

:::

## Example Usage

For example, imagine you have an application called ProductService with the following settings:

```c#
public class ProductService : SettingsBase
{
    public override string ClientDescription => "Sample Product Service";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}
```

You could then define your docker compose file like this:

```yaml
productserviceapp:
    image: myProductServiceImage:latest
    container_name: ProductServiceImage
    environment:
      - WebsiteAddress=http://bing.com
```

If overrides are enabled for this client, the value 'http://bing.com' will be sent along with the registration of the client and will be used to update the value of WebsiteAddress.

## Externally Managed Settings

When a client setting override is applied, the setting is automatically marked as [externally managed](./22-externally-managed-settings.md). This means:

- The setting will appear as read-only in the Fig web application (with a padlock icon)
- The change history will include details about which application set the value
- Administrators can still unlock and edit the setting if needed, but the value will revert on the next client registration

:::note

You can change the value in the Fig web application by unlocking the setting, but it will revert back to the override value when you next restart the client application. If that is undesirable, you can always turn off the client override feature.

:::

## Configuration

This feature is enabled by default but can be disabled in the fig configuration which is available when logged into the web application as an administrator.

It is also possible to enter a regular expression there to define which clients will be allowed to override their settings and which will not. The regular expression is evaluated against the client name.
