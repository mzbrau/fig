---
sidebar_position: 14
sidebar_label: Authentication
---

# Authentication

Fig Web is protected by user authentication. Fig-managed mode (the default) uses a username and password. An admin account is created when the database is first created:

```
user: admin
password: admin
```

Change that password before exposing Fig beyond a local machine. The API can require a change on first login (`ForceAdminDefaultPasswordChange`).

Web requests use JWT tokens. Client endpoints (register / get settings) are authenticated with the **client secret**, not a user JWT. Any client can register its settings and must present the same secret to read values later. You can disable new registrations from the Configuration page once all known clients are registered.

Fig can instead validate users through **Keycloak / OIDC**. In that mode, Fig user-management endpoints are unavailable. See [Security — Keycloak authentication](../security.md#keycloak-authentication-mode) and [User Management](./3-user-management.md).
