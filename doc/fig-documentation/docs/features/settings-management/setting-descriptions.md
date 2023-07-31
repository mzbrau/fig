---
sidebar_position: 14

---

# Setting Descriptions

Descriptions should be supplied with each setting to explain what the setting does and any potential implications of changing it. Descriptions are provided within the `[Setting]` attribute.

A basic description might look like this:

``` csharp
[Setting("Turns on the debug mode", false)]
public bool DebugMode { get; set; }
```

![image-20230725221606792](../../../static/img/image-20230725221606792.png)

However, setting descriptions also support **basic Markdown syntax** which allow developers to convey information in a format that is easy to understand and digest for the person performing the configuration. A detailed description is recommended and may look like this:

``` csharp
[Setting("**Debug Mode** results in the following changes to the application:\r\n" +
             "- Increases *logging* level\r\n" +
             "- Outputs **full stack traces**\r\n" +
             "- Logs *timings* for different operations \r\n" +
             "\r\nExample output with *debug mode* on:\r\n" +
             "```\r\nMethod: Do Stuff, Execution Time: 45ms\r\n```", false)]
public bool DebugMode { get; set; }
```

Which results in a more readable text description:

![image-20230725222814110](../../../static/img/image-20230725222814110.png)



## Setting Descriptions from Markdown Files

While the example above looks pretty good for the person configuring the application. It it is difficult to read for the developer. An easier way to manage the documentation is to store it in a markdown file which is an embedded resource in the application and then reference it in the fig configuration.

Steps are as follows:

1. Create a markdown file within the project (entry assembly) and give it a name (it doesn't matter what)
2. Make the markdown file an embedded resource in the project
3. Write your documentation within the markdown file
4. Reference the file within fig using the following syntax:

```csharp
$FullyQualifiedResourceName
```

For example

```csharp
$Fig.Integration.SqlLookupTableService.ServiceDescription.md
```

However, there might be many settings and in this case you don't what to create a markdown file per setting. Fig allows you to specify a section of a markdown file using the following syntax:

```
$FullyQualifiedResourceName#HeadingName
```

For example

```
$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUri
```

This will take all the text and subheadings below that heading block, but not that heading block itself.

You can see a full working example of this [here](https://github.com/mzbrau/fig/blob/main/src/integrations/Fig.Integration.SqlLookupTableService/Settings.cs#L11).

