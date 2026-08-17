---
sidebar_position: 6
---

# Fig Version 1.x to 2.x Migration Guide

## Introduction

There are a number of breaking changes between Fig v1.x and v2.x. This guide is designed to make the migration process as smooth as possible.

## Compatibility Matrix

|              | Component                                   | Notes                                                        |
| ------------ | ------------------------------------------- | ------------------------------------------------------------ |
| Fig API v1.2 | :white_check_mark: ​Fig Client v1.2          |                                                              |
|              | :white_check_mark: Fig Client v2.0          |                                                              |
|              | :white_check_mark: Fig Web Application v1.2 |                                                              |
|              | :x: Fig Web Application v2.0​                | The api version and web app version must be the same major version. |
| Fig API v2.0 | :white_check_mark: ​Fig Client v1.2          |                                                              |
|              | :white_check_mark: Fig Client v2.0          |                                                              |
|              | :x: Fig Web Application v1.2                | The api version and web app version must be the same major version. |
|              | :white_check_mark: Fig Web Application v2.0​ |                                                              |

In addition, you will need to make code changes when you update the `Fig.Client` nuget package from version 1.2 to 2.0. These are outlined below.

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

## DI Registration

Container registrations have been simplified.

Replace:

```csharp
builder.Host.UseFigValidation<Settings>();
builder.Host.UseFigRestart<Settings>();
```

with this:

```csharp
builder.Host.UseFig<Settings>();
```

Assuming `Settings` is the name of your configuration class.

## Setting Validation Errors

Validations have changed in Fig 2.0. Previously there was a method within the settings class which might have looked something like this:

```csharp
public override void Validate(ILogger logger)
{
    var validationErrors = GetValidationErrors().ToList();

    if (validationErrors.Any() && !HasConfigurationError)
    {
        SetConfigurationErrorStatus(true, validationErrors);
        foreach (var error in validationErrors)
        {
            logger.LogError("{Error}", error);
        }
    }

    if (!validationErrors.Any())
    {
        SetConfigurationErrorStatus(false);
    }
}

private IEnumerable<string> GetValidationErrors()
{
    if (!MockResults && string.IsNullOrWhiteSpace(Server))
    {
        yield return $"Server should have a value";
    }
}
```

This was complicated, convoluted and duplicated the validation logic.

In Fig 2.0, there are the following changes:

1. The `Validate` method has been removed and replaced with `GetValidationErrors`
2. The validation attribute is now used to validate the settings client side

To migrate, replace the methods above with:

```csharp
public override IEnumerable<string> GetValidationErrors()
{
    return []; // Replace this with your validation logic
}
```

Review all `[Verification]` attributes. If you are confident that they will be correct 100% of the time, no change is required.

If you think there is a chance that they will not be correct in some circumstances or you don't want them to be part of the health check, then update them to be excluded from the health check:

```csharp
[Validation(ValidationType.NotEmpty, includeInHealthCheck: false)]
```

This means you can remove duplicated validation checks that you may have had in code previously. The code validation errors check is still useful for complex or multi-setting validation.

### Nested Settings

If you have [nested settings](../features/settings-management/13-nested-settings.md) then you need to ensure that any custom validation logic is rolled up to the top level. This was not the case previously as the validation errors were held in a static class.

However, this has now been removed and you'll need to call validation manually on nested classes as only the top level class is called.

## New Validation Attributes

Four new [validation attributes](../features/settings-management/20-validation.md) have been added:

- `[ValidateGreaterThan(5)]`
- `[ValidateLessThan(5)]`
- `[ValidateIsBetween(5, 10)]`
- `[ValidateSqlServerConnectionString]`

Consider if they could be added to your settings.

## Verifiers

The verifiers feature has been removed. If you had any references to verifiers, they should be removed.

## Display Order

Display order has been deprecated for some time and is now removed. Just order the settings in your class and that will be the order used.

## Environment Specific

The [environment specific attribute](../features/settings-management/9-environment-specific.md) has been added. This should be applied to any settings that will be different between environments (QA, Production, etc.) to facilitate easier export and transfer of settings between environments.

## Custom Actions

[Custom Actions](../features/27-custom-actions.md) have been added. Consider how they might be useful.

## Categories

[Categories](../features/settings-management/2-category.md) have been updated with pre-defined category types. Using these will lead to a more consistent experience with multiple applications are developed across teams.
