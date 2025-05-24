---
sidebar_position: 19
---

# Valid Values (Dropdowns)

In many cases, settings may have a limited set of valid values. In these scenarios, it will assist those configuring the application to provide a dropdown list of valid values to choose from. One example might be the selection of log levels or setting an enum value.

## Usage

Valid values can be set in 2 different ways:

### Explicit specification

Values can be specified explicitly as follows:

```csharp
[ValidValues("a", "b", "c")]
[Setting("Choose from a, b or c", "a")]
public string DropDownStringSetting { get; set; }
```

It is also possible to specify different values to be displayed in the dropdown. This can be useful in the case that the values require additional context in the dropdown.

```csharp
[ValidValues("1 -> High", "2 -> Medium", "3 -> Low")]
[Setting("Enum value", 1)]
public int Levels { get; set; }
```

### Enum Definition

```csharp
[ValidValues(typeof(LogLevel))]
[Setting("Choice of log levels", LogLevel.Info)]
public LogLevel EnumSetting { get; set; }
```

## Appearance

![2022-08-02 21.25.39](../../../static/img/valid-values.png)