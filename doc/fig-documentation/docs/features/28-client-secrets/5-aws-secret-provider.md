---
sidebar_position: 5
sidebar_label: AWS Client Secret Provider
---

# Fig AWS Secret Provider

:::warning[Experimental]

This integration has not been well tested and may contain bugs. Please report any bugs to the GitHub repo

:::

This package provides AWS Secrets Manager integration for Fig client secret management using managed identity authentication.

## Features

- **Managed Identity Support**: Uses AWS SDK's default credential chain (IAM roles, instance profiles, etc.)
- **Automatic Secret Creation**: Secrets are only auto-created if the environment variable `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is set to `Development`. In all other environments, secrets must already exist or a `SecretNotFoundException` will be thrown.
- **Thread-Safe**: Safe for concurrent use across multiple threads
- **Retry Logic**: Built-in exponential backoff for transient errors
- **Resource Management**: Implements IDisposable for proper cleanup

## Installation

```bash
dotnet add package Fig.Client.SecretProvider.Aws
```

## Usage

### Basic Usage with Default Credentials

```csharp

builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "AspNetApi";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new AwsSecretProvider(RegionEndpoint.USEast1)]
    });
```

## Secret Naming Convention

Secrets are stored in AWS Secrets Manager using the format: `FIG_{CLIENT_NAME}_SECRET`

For example, if your client name is "MyService", the secret will be named: `FIG_MYSERVICE_SECRET`

## Authentication

This provider uses the AWS SDK's default credential chain, which checks for credentials in the following order:

1. Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`)
2. AWS credentials file (`~/.aws/credentials`)
3. IAM roles for Amazon EC2 instances
4. IAM roles for Amazon ECS tasks
5. IAM roles for AWS Lambda functions

## Required AWS Permissions

The IAM role or user must have the following permissions:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "secretsmanager:GetSecretValue",
                "secretsmanager:CreateSecret",
                "secretsmanager:DescribeSecret"
            ],
            "Resource": "arn:aws:secretsmanager:*:*:secret:FIG_*"
        }
    ]
}
```

In non-development environments, secrets are never auto-created, so you only need the `GetSecretValue` permission.

## Error Handling

The provider handles common AWS errors gracefully:

- **ResourceNotFoundException**: Thrown when a secret doesn't exist and the environment is not `Development`
- **Transient Errors**: Automatically retried with exponential backoff
- **Concurrent Creation**: Handles race conditions when multiple instances try to create the same secret

## Best Practices

1. **Use Managed Identity**: Deploy to AWS services (EC2, ECS, Lambda) with appropriate IAM roles
2. **Least Privilege**: Grant only the minimum required permissions
3. **Resource Cleanup**: Dispose the provider when your application shuts down
4. **Error Monitoring**: Log and monitor for `SecretNotFoundException` and permission errors
5. **No AutoCreate in Production**: In production and all non-development environments, secrets will not be auto-created.
