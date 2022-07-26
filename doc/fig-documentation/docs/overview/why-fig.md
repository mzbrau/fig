---
sidebar_position: 1
---

# Why Fig?

If you have ever created a service or other application, you probably found at some stage you needed to add some configurable components. This is particularly the case if the application could be deployed into multiple environments. 
This could be things like:
- an API URL;
- a timeout value;
- credentials to access an external service; or 
- a logging level.

There are many ways of passing this information to your application including **command parameters** or **environment variables**, however in dotnet framework, this was most commonly achieved using an `app.config` file and using `ConfigurationManager.AppSettings["SettingName"]`. This worked ok but had a lot of shortcomings.

In dotnet core/6 you would probably use an `appsettings.json` file and then inject an `IOptions<T>` where it is required to access your settings. This pattern addressed many problems with the app.config structure including live reload, some support for concrete types and better testing support. However, there are still many problems including the ability to set settings across multiple applications in an efficient way, sharing settings between applications, audit logging settings changes among others.

Fig provides an alternative approach. Settings are defined by the application requiring them in code. Attributes are added to give Fig a hint about how they should be managed and Fig does the rest. The settings are registered on startup towards the Fig API. They are then made available via the Fig web client where they can be updating and managed. Settings editors for each type ensure those configuring the application are not inputting incorrect information and text descriptions explain what the setting is and how it should be used. 

Fig allows settings to be managed across many micro services in an efficient, secure and fool proof way.

