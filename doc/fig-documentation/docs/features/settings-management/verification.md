---
sidebar_position: 9
---

# Verification

Verification attrbutes can be added to classes to enable verification from within the Web Client.

For details on how to create verifications, see the [Verifications](http://www.figsettings.com/docs/features/verifications) page.

## Usage


```csharp
[Verification("Rest200OkVerifier", nameof(WebsiteAddress))]
public class ProductService : SettingsBase
```

Appearance

![image-20220802234242307](../../../static/img/setting-verifier.png)
