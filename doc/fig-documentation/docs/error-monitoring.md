---
sidebar_position: 10

---

# Error Monitoring

Fig has an integration to [Sentry](https://sentry.io) for monitoring unhandled exceptions that might occur. 

As fig is not provided as a managed service, the sentry integration is not configured globally. This means that if you intend to use fig and you want to capture errors that occur, you will need to install / sign up for Sentry independently and then add your DSN links into the API and Web project configurations. Once set up a user feedback button will be enabled and any unhandled exceptions will be sent to the server for analysis.



## Initial Setup

Sentry is a cloud service. They have a free tier as well as paid subscriptions. To sign up for Sentry, head [to their website](https://sentry.io/welcome/) and follow the prompts.

Alternatively, Sentry can be run self hosted, see [here](https://develop.sentry.dev/self-hosted/) for details.

Once you have logged into Sentry, create 2 projects:

- fig-api
- fig-web

Copy the DSN for each project and add them into the corresponding appsettings.json files for each project. For example:

```json
{
  "WebSettings": {
    "ApiUri": "https://localhost:7281",
    "SentryDsn": "https://XXX.ingest.sentry.io/123",
    "Environment": "Development"
  }
}
```

Ensure you set the Environment to the correct name as this will be used for error analysis.

## Monitoring

You can monitor errors that occur from the Issues page in Sentry.

![image-20230816104835624](C:\Development\SideProjects\fig\doc\fig-documentation\static\img\image-20230816104835624.png)

## Reporting Problems

If you find a bug in fig that requires fixing, please create an issue on the [Fig Github page](https://github.com/mzbrau/fig/issues). Please include as much detail as possible (the whole error report can be exported as JSON). You are also welcome to create a pull request with a fix for the issue.