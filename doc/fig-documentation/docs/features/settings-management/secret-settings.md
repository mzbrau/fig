---
sidebar_position: 4
---

# Secret Settings

Fig supports protecting the values of some settings. Secret settings are treated as passwords within the web client and are not shown to the configuring user.

## Usage

```c#
[Setting("Secret Setting", "SecretString")]
[Secret]
public string SecretSetting { get; set; }
```

## Appearance

![2022-07-26 22.25.50](../../../static/img/:Users:michaelbrowne:Documents:Projects:fig:doc:fig-documentation:static:img:secret-settings.png)