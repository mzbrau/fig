---
sidebar_position: 12
---

# Multi-Line Strings

Multi-line string settings are supported in Fig by adding an attribute to the string setting.
The number indicates how many lines will be shown in the editor.

## Usage

```csharp
[Setting("Multi Line Setting")]
[MultiLine(6)]
public string? MultiLineString { get; set; }
```

## Appearance

![image-20220726221132075](../../../static/img/image-20220726221132075.png)
