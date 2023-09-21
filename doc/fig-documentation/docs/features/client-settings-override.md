---
sidebar_position: 20



---

# Client Settings Overrides

In some situations you want the settings to be driven by the client. For example when deploying applications using Docker compose if you have address to other containers within the compose file, you can reference them by their container name. In that case, you would like the setting to be set by the docker compose file rather than having to be manully set in Fig or use a default.

In this case, we can use the client settings override feature. Just set an environment variable with the exact same name as the setting you wish to set and the Fig client library will read that on startup and send that along with the registration information. If client overrides are enabled on the API those values will be used to update the setting value.

This feature is disabled by default but can be enabled in the fig configuration which is available when logged into the web application as an administrator.

It is also possible to enter a regular expression there to define which clients will be allowed to override their settings and which will not. The regular expression is evaluated against the client name.