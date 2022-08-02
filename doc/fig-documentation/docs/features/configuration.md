---
sidebar_position: 9
---

# Configuration

There are a few parameters which can be configured for Fig which enable or disable certain features in the application.

### Allow New Client Registrations

When disabled, new client registrations (those who have not previously registered with Fig) will not be allowed to register.

It is recommended that new registrations be disabled in a production system once all clients are registered for security reasons.

### Allow Updated Client Registrations

When disabled, clients will not be allowed to change the setting definitions when they register.

This could be useful in a live upgrade situation where a new version of a client adds settings. Once the new settings have been added, disable updated registrations to avoid any instances of the older clients reverting the registration and removing the new settings.

### Allow Offline Settings

Fig clients can save the settings values locally in a file so they client can still start even if the Fig API is down.

Settings are encrypted using the client secret and stored in a binary file. However, it may still be desirable to disable this feature if additional security is more important than up time.

### Allow Dynamic Verifications

Fig supports plugin and dynamic verifications. Dynamic verifications are defined client side and compiled and run on the server.

While a useful feature in some situations, this does allow for remote code execution on the server provided the user is able to register a client and has Fig credentials. It can be disabled for security reasons if it is not required in production.

### Allow File Imports

Fig supports loading export from an import directory. This is a useful feature when Fig is deployed in a container as a Helm chart or similar can be used to set the initial configuration.

However, depending on the level of access to the import directory, it may impose a security risk as imports can be configured to remove existing clients and settings.

## Appearance

![image-20220802231541473](../../static/img/fig-configuration.png)