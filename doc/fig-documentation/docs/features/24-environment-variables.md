---
sidebar_position: 24
sidebar_label: Environment Variables
---

# Environment Variables

Fig reads a number of environment variables. Some are required for base functionality while others can be used to override setting configuration without having to made code changes.

The following environment variables are available:

| Category                   | Function                                                     | Format                                    | Example                                        |
| -------------------------- | ------------------------------------------------------------ | ----------------------------------------- | ---------------------------------------------- |
| Mandatory - Basic Function | Specifies the location of the Fig API so the client knows where it should contact. | `FIG_API_URI`                             | `FIG_API_URI=http://localhost:7281`            |
| Setting Configuration      | Overrides the group attribute for a specific setting.        | `FIG_[SettingName]_GROUP`                 | `FIG_MYSETTING_GROUP = MyGroup`                |
| Setting Configuration      | Overrides the Validation Regex attribute for a specific setting | `FIG_[SettingName]_VALIDATIONREGEX`       | `FIG_MYSETTING_VALIDATIONREGEX = \d`           |
| Setting Configuration      | Overrides the Validation Explanation attribute for a specific setting | `FIG_[SettingName]_VALIDATIONEXPLANATION` | `FIG_MYSETTING_VALIDATIONEXPLANATION = Number` |
| Setting Configuration      | Overrides the lookup table key attribute for a specific setting | `FIG_[SettingName]_LOOKUPTABLEKEY`        | `FIG_MYSETTING_LOOKUPTABLEKEY = MyLookup`      |
| Client Configuration       | Sets the instance that this client should attempt to read. If instance does not exist, it will get the base settings. This can also be set with `--instance=MyInstance`, which takes precedence over the environment variable. | `FIG_[CLIENTNAME]_INSTANCE`               | `FIG_MYCLIENT_INSTANCE = MyInstance`           |
| Client Configuration       | Overrides the poll interval that will be used to poll the API for updates | `FIG_POLL_INTERVAL_MS`                    | `FIG_POLL_INTERVAL_MS = 30000`                 |
| Client Configuration       | Overrides the classification of the setting                  | `FIG_[SettingName]_CLASSIFICATION`          | `FIG_MYSETTING_CLASSIFICATION = Functional`    |
| Client Configuration       | Overrides the HTTP request timeout (in whole seconds) used when contacting the Fig API. Takes precedence over `FigOptions.ApiRequestTimeout` and the hard-coded context-based defaults (Windows Service with offline settings: 2 s; Windows Service without offline settings: 5 s; other contexts with offline settings: 5 s; other contexts without offline settings: 60 s). Useful for raising the timeout in production without a code change. Must be a positive integer. | `FIG_API_REQUEST_TIMEOUT_SECONDS` | `FIG_API_REQUEST_TIMEOUT_SECONDS = 10` |
| Client Configuration       | Disables the [registration checksum](./37-registration-checksum.md) optimisation so the client always registers settings on startup. | `FIG_DISABLE_REGISTRATION_CHECKSUM` | `FIG_DISABLE_REGISTRATION_CHECKSUM=true` |
| Client Configuration       | Override the folder used for offline settings and registration checksum files. See [Registration Checksum](./37-registration-checksum.md). | `FIG_APP_DATA_DIR` | `FIG_APP_DATA_DIR=/var/fig` |
| Client Configuration       | When `true` or `1`, the client accepts the Fig API certificate without validation. Development only. | `FIG_INSECURE_SSL` | `FIG_INSECURE_SSL=true` |
| Client Configuration       | When `true` or `1`, skip `[MigrateFrom]` value migration on registration. | `FIG_DISABLE_MIGRATE_FROM` | `FIG_DISABLE_MIGRATE_FROM=true` |
| Client Configuration       | Override the order of [client secret providers](./28-client-secrets/1-client-secret-providers.md) (`Docker`, `Dpapi`, `Azure`, `Aws`, `Google`). | `FIG_CLIENT_SECRET_PROVIDERS` | `FIG_CLIENT_SECRET_PROVIDERS=Docker,Dpapi` |
