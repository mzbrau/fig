---
sidebar_position: 6
---

# Fig Version 1.x to 2.x Migration Guide

## Introduction

There are a number of breaking changes between Fig v1.x and v2.x. This guide is designed to make the migration process as smooth as possible.

## Client Secrets

Fig v1.x had support for 4 client secret providers:

- DPAPI
- Docker Secret
- Command Line
- Hard coded (not for production)

All these are still supported in v2 but the DPAPI and Docker Secret providers have been broken into their own nuget packages.

If you want to keep the existing functionality, do the following:

1. Add 2 nuget packages to your application:
   1. `dotnet add package Fig.Client.SecretProvider.Docker`
   2. `dotnet add package Fig.Client.SecretProvider.Dpapi`
2. Add the following line into your Fig Registration:
   1. `options.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];`

For example, the full registration might look like this:

```csharp
builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "AspNetApi";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];
    });
```

## Setting Validation Errors

TODO - more here

- Chain validations
- Rename method for handling validations

## Verifiers

Removed

## Environment Specific

Added
