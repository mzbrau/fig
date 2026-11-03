---
sidebar_position: 3
---

# Encrypting Client Secrets

When installed on Windows, Fig encrypts client secrets with DPAPI using **LocalMachine** scope so Windows services (including Local System) can decrypt values created by an interactive admin on the same machine.

This API can be called via PowerShell, but Fig includes an encryption / decryption utility which is able to perform this operation.

Simply start the Fig.Dpapi.Client application, generate or enter your client secret, copy the output, and add it into a **Machine** environment variable named `FIG_<ClientName>_SECRET` (uppercase, no spaces).

Note:

- This can only be run on Windows
- The tool must be running on the **same machine** where the secret will be decrypted
- Any local account can encrypt; Local System and other accounts on that machine can decrypt
- Any process on the machine that can read the ciphertext can decrypt LocalMachine DPAPI values — protect environment variables and config files with ACLs
- Prefer a **Machine** environment variable for Windows services; a User environment variable is invisible to Local System

## Migration from CurrentUser DPAPI

Earlier Fig versions encrypted with `DataProtectionScope.CurrentUser`. After upgrading:

1. Upgrade Fig.Client, Fig.Api (if using `SecretsDpapiEncrypted`), and the DPAPI tool together where possible
2. Re-encrypt each client secret with the updated tool or PowerShell using `LocalMachine`
3. Store the ciphertext in the **Machine** environment variable (move any User-only values)
4. If using offline mode, regenerate `appsettings.fig.json` on the target machine with `--printappsettings`, passing each current secret as an override (for example `Password=MyCurrentSecret`) so the file keeps live values instead of code defaults
5. If using `SecretsDpapiEncrypted`, re-Protect `Secret` / `PreviousSecret` with LocalMachine and replace the config values
6. Confirm Fig.Client warnings about CurrentUser crypto or User-scoped env vars disappear after migration

Fig.Client still decrypts legacy CurrentUser ciphertext (with a warning) so same-user installs keep working until you re-encrypt. Treat re-encryption as required — the CurrentUser fallback may be removed in a future release.

:::tip Local System
Encrypting as an interactive user with LocalMachine scope and storing the value in a Machine environment variable is the supported path for services running as Local System. Dual-try decrypt does **not** make old CurrentUser secrets work under Local System if they were encrypted by a different account.
:::
