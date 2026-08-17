---
sidebar_position: 25
---

# Init Only Export (InitOnlyExport attribute)

The `InitOnlyExport` attribute marks settings that should be exported separately for init-only imports.

## Why use it?

Infrastructure as code workflows often need two value-only imports:

- **Init-only** values that should only apply before a client has ever registered (`Update Values Init Only`)
- **Regular** values that should always update (`Update Values`)

`InitOnlyExport` lets you tag those bootstrap-only settings in code.

## Usage

```csharp
[Setting("Bootstrap API Key")]
[InitOnlyExport]
public string BootstrapApiKey { get; set; } = "initial-key";

[Setting("Runtime timeout seconds")]
public int RuntimeTimeoutSeconds { get; set; } = 30;
```

## Behavior

- The metadata is stored with the setting definition in Fig
- Full exports include this metadata
- Value-only exports include this metadata
- Split value-only export can use this metadata to generate init-only and regular files per client
