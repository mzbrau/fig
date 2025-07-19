---
sidebar_position: 23
---

# Indent Attribute

## Overview

The `IndentAttribute` is used to visually indent settings in the Fig web UI, creating a hierarchical display that helps organize related settings.

## Usage

```csharp
[Setting("Root setting")]
public string RootSetting { get; set; } = "Root";

[Setting("Child setting")]
[Indent(1)]  // Indented by 10px
public string ChildSetting { get; set; } = "Child";

[Setting("Grandchild setting")]
[Indent(2)]  // Indented by 20px  
public string GrandchildSetting { get; set; } = "Grandchild";
```

## Parameters

- **Level**: An integer value between 0 and 5 (inclusive) that specifies the indentation level
- **Default**: 1 if not specified when using the attribute
- **Multiplier**: Each level adds 10px of left margin in the web UI

## Validation

- Level must be between 0 and 5 (inclusive)
- Values outside this range will throw an `ArgumentOutOfRangeException`
- A level of 0 results in no indentation (same as not using the attribute)

## Behavior

- When the attribute is not present, no indentation is applied
- The indentation is applied as a left margin in the web UI
- Indentation is calculated as: `level * 10px`
- Maximum indentation is 50px (level 5)
