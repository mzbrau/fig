---
sidebar_position: 26
sidebar_label: Azure Key Vault Integration
---

# Azure Key Vault Integration

Fig can handle secret values with all setting values being stored encrypted in an enterprise database. However for some high security deployments it may be desirable to store secret values in a dedicated secret store such as Azure Key Vault. Fig has an integration to Azure Key Vault and if enabled, all values marked as secret attribute will be stored there rather than the fig database.

:::warning[Experimental]
This feature is currently experimental and requires further testing.
:::

## Pre-requisites

Before this integration can be enabled, the following prerequisites must be met:

1. The Fig API must be running in Azure
2. A KeyVault must be created in Azure
3. The identity of the machine or container running the Fig API must be granted access to read and write to the KeyVault. It will create and update secrets. It will not delete secrets, even if they are deleted from Fig. Alternatively, you could log into Azure from the console and then run Fig from that console to use the existing connection.

## Enabling the Integration

To enable the integration, log into Fig as an administrator and select **Configuration**.
Enable __Azure Key Vault for Secrets__ and enter the name of the KeyVault created earlier.
Press the TEST button to verify that the configuration is correct.

## Using the integration

Once the configuration is enabled, secrets will automatically be pushed to the KeyVault whenever they are updated in the Fig web application. Secrets are not automatically migrated over when the integration is enabled. It is therefore recommended that you change the secret values after enabling the integration.
Clients will access the Fig API as normal but secrets will be populated from the KeyVault rather than the Fig database.

## Integration Design

The design of the integration is shown below.

![Diagram - Option1](../../static/img/keyvault-option1.excalidraw.png)
