---
sidebar_position: 7
---

# Fig Version 2.0 to 3.0 Migration Guide

## Introduction

There are some changes that may require code changes when migrating from 2.0 to 3.0.

## Fig.Client

A new package has been added `Fig.Client.Abstractions` which now contains a number of components used with Fig including the attributes.

This means that some namespaces have been removed, e.g.

- `Fig.Client.Attributes`
- `Fig.Client.Enums`
- `Fig.Client.Validation`

These have been replaced with other namespaces, including:

- `Fig.Client.Abstractions.Attributes`
- `Fig.Client.Abstractions.Enums`
- `Fig.Client.Abstractions.Validation`

You'll need to update your using statements once updating to the new version of Fig.Client.

## Connection String

If using SQL Server, you may need to add `TrustServerCertificate=True` to the connection string for non production settings to enable the Fig.Api to connect.

## DependentSettings attribute removed

Replaced by `DependsOn`. See [Conditional Settings](../features/settings-management/22-conditional-settings.md) for details of how that is implemented.

## Display script custom setting implementations

If you maintain a custom implementation of `IScriptableSetting` outside the core Fig packages, add the new nullable `InformationText` property when updating to newer 3.x builds:

```csharp
string? InformationText { get; set; }
```

Display scripts use this property to show blue informational callouts below a setting, and setting it to `null` hides the callout again. If your type cannot expose a public property directly, implement the interface member explicitly.
