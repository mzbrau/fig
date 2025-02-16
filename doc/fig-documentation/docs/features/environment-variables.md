---
sidebar_position: 24


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
| Client Configuration       | Sets the instance that this client should attempt to read. If instance does not exist, it will get the base settings. | `FIG_[CLIENTNAME]_INSTANCE`               | `FIG_MYCLIENT_INSTANCE = MyInstance`           |
| Client Configuration       | Overrides the poll interval that will be used to poll the API for updates | `FIG_POLL_INTERVAL_MS`                    | `FIG_POLL_INTERVAL_MS = 30000`                 |
| Client Configuration       | Overrides the classification of the setting                  | `FIG_[SettingName]_CLASSIFICATION`          | `FIG_MYSETTING_CLASSIFICATION = Functional`    |

