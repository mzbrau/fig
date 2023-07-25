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

<img src="../../../static/img/image-20230725221606792.png" alt="image-20230725221606792" style="zoom:50%;" />

However, setting descriptions also support **basic Markdown syntax** which allow developers to convey information in a format that is easy to understand and digest for the person performing the configuration. A detailed description is recommended and may look like this:

```csharp
[Setting("**Debug Mode** results in the following changes to the application:\r\n" +
             "- Increases *logging* level\r\n" +
             "- Outputs **full stack traces**\r\n" +
             "- Logs *timings* for different operations \r\n" +
             "\r\nExample output with *debug mode* on:\r\n" +
             "```\r\nMethod: Do Stuff, Execution Time: 45ms\r\n```", false)]
public bool DebugMode { get; set; }
```

Which results in a more readable text description:

<img src="../../../static/img/image-20230725222814110.png" alt="image-20230725222814110" style="zoom:50%;" />
