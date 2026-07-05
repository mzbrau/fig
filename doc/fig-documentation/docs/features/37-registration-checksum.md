---
sidebar_position: 37
sidebar_label: Registration Checksum
---

# Registration Checksum

By default, the Fig client compares a checksum of your settings definition against a file stored on disk at startup. When the checksum matches, the client skips the `POST /clients` registration call and goes straight to requesting setting values. This reduces startup time when your settings class has not changed since the last successful registration.

The checksum covers the stable parts of the registration payload:

- Client description
- All setting definitions (including default values and attributes)
- Custom actions registered with the client
- Whether the client has display scripts

Runtime-only fields such as client setting overrides, migration preview results, and client version are excluded so they do not force unnecessary re-registration.

## How startup works

1. The client builds the settings definition from your `SettingsBase` subclass.
2. It computes a SHA-256 checksum of that definition.
3. If the checksum matches the value stored on disk, registration is skipped and the client requests setting values immediately.
4. If the checksum differs, the file is missing, or the feature is disabled, the client registers settings and then requests values (the previous behaviour).
5. After a successful registration, the checksum is saved to disk for the next startup.
6. If the settings request returns **404 Not Found** (for example, the client was deleted on the server), the client registers settings and retries the settings request.

## File location and naming

Checksum files are stored in the same Fig app-data folder as [offline settings](./20-offline-settings.md):

| Platform | Typical path |
| -------- | ------------ |
| Windows | `%LocalApplicationData%\Fig\` |
| macOS | `~/Library/Application Support/Fig/` |
| Linux | `~/.local/share/Fig/` |

Filenames follow the same convention as offline settings files, using the **client name** and optional **instance**:

| Condition | Filename pattern | Example |
| --------- | ---------------- | ------- |
| No instance | `{ClientName}.checksum` | `MyApp.checksum` |
| With instance | `{ClientName}_{Instance}.checksum` | `MyApp_Production.checksum` |

Invalid filename characters are removed and spaces are stripped from client and instance names, matching offline settings behaviour.

The file contains a plain-text SHA-256 hex string. It is not encrypted.

## Disabling the feature

Set the following environment variable to restore the previous behaviour (always register, then request values):

```
FIG_DISABLE_REGISTRATION_CHECKSUM=true
```

The value `1` is also accepted.

## Container deployments

Checksum files live in the container's local application data directory. Without a persistent volume, the checksum file is lost when the container restarts. The client will safely fall back to full registration on the next startup.

Map a volume to the Fig app-data folder so the checksum (and offline `.dat` files) survive restarts.

### Linux container example

```yaml
services:
  myapp:
    image: myapp:latest
    environment:
      - FIG_API_URI=https://fig-api.example.com
    volumes:
      - fig-client-data:/root/.local/share/Fig

volumes:
  fig-client-data:
```

The exact mount path depends on the container user. For images running as a non-root user, use that user's home directory (for example `/home/app/.local/share/Fig`).

### Windows container example

Mount a volume to the user's local application data Fig folder, for example:

```
C:\Users\ContainerUser\AppData\Local\Fig
```

### Docker Compose with both offline settings and checksum

A single volume mount covers offline settings (`.dat`) and registration checksum (`.checksum`) files because they share the same directory:

```yaml
volumes:
  - fig-client-data:/root/.local/share/Fig
```

## When registration still runs

Registration is always performed when:

- The checksum file does not exist
- The checksum does not match the current settings definition
- `FIG_DISABLE_REGISTRATION_CHECKSUM` is set
- The settings request returns 404 (client not found on the server)
- A previous registration attempt failed (no checksum is saved until registration succeeds)

Changing setting definitions, descriptions, default values, or custom actions changes the checksum and triggers registration on the next startup.
