---
sidebar_position: 3
---

# Encrypting Client Secrets

When installed on Windows, Fig requires client secrets to be encrypted in DPAPI. 

This API can be called via Powershell but Fig includes an encryption / decryption utility which is able to perform this operation.

Simpliy start the Fig.Dpapi.Client application, generate or enter your client secret and copy the output and add it into an environment variable named `Fig_<ClientName>_Secret`.

Note:

- This can only be run on windows
- The tool must be running on the same machine as where the encryption key will be used
- The tool must be running as the same user as the application will run as