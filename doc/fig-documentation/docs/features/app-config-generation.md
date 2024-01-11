---
sidebar_position: 22



---

# App.config File Generation

Fig integrates into the configuration framework built into ASP.NET core. As part of that framework it is possible to draw configuration from a variety of sources including appSettings.json files, environment variables and app.config files. App.config files are largely legacy but are sometimes required for backwards compatibility. They can be tricky to write so they are compatible with the nested structure of the appSettings.json files.

To simplify the process of using app.config files, Fig supports the generation of these files from the Fig configuration. This allows them to be used in the cases where the Fig server is not available.

To generate app.config files, run your application with the command line argument `--printappconfig`. This will result in the application writing the configuration that is required in the app.config file to the configured log file.

For example:

```xml
---- App.Config Configuration ----
<appSettings>
<add key="Mode" value="Mode A" />
<add key="ModeASetting" value="Some Value" />
<add key="ModeBSetting1" value="Thing" />
<add key="ModeBSetting2" value="Another thing" />
<add key="UseSecurity1" value="False" />
<add key="Url1" value="http://www.google.com" />
<add key="UseSecurity2" value="False" />
<add key="Url2" value="http://www.google.com" />
<add key="Option" value="Select Option..." />
<add key="ControlledString" value="" />
<add key="ControlledInt" value="0" />
<add key="ControlledBool" value="False" />
<add key="ControlledLong" value="0" />
<add key="ControlledDouble" value="0" />
<add key="ControlledDateTime" value="" />
<add key="ControlledTimeSpan" value="" />
<add key="Groups" value="" />
<add key="Services:0:Name" value="Service 0" />
<add key="Services:0:Group" value="" />
<add key="Services:0:ValidationType" value="" />
<add key="Services:0:CustomString" value="" />
</appSettings>
```

