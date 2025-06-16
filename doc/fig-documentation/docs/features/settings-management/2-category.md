---
sidebar_position: 2
---

# Category

It is possible to display an association between different settings using the category feature. When a setting is assigned a category, it is assigned a category name and a color which will visually indicate that it is related to other settings with the same color and category name.

## Usage

```csharp
[Setting("the username")]
[Category("Authentication", CategoryColor.Red)]
public string? ServiceUsername { get; set; }
```

It is also possible to manually specify the color

```csharp
[Setting("the username")]
[Category("Authentication", "#6d8750")]
public string? ServiceUsername { get; set; }
```

From Fig 2.0 you can just specify the category from a predefined list:

```csharp
[Setting("the username")]
[Category(Category.Authentication)]
public string? ServiceUsername { get; set; }
```

This will automatically select the color and name leading to a more consistent experience when applications are developed across teams.

## Appearance

The category name appears as a tooltip.

![image-20230831083953172](../../../static/img/image-20230831083953172.png)
