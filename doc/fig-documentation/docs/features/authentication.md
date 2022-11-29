---
sidebar_position: 14
---

# Authentication

Fig has built in authentication to protect your settings. Access to the Fig web application is protected by a username and password.

An admin account is created on database creation with the following credentials:

```
user: admin
password: admin
```

Web authentication is managed via JWT tokens which are included with every request. 

There is no JWT authentcation on certain client based endpoints. This is by design as clients do not have credentials to access Fig. Any client can register their settings and include a client secret. This secret is then required to read back the setting values at a later date. 

It is possible to turn off setting registrations from within the Fig webpage once all known clients are registered.

