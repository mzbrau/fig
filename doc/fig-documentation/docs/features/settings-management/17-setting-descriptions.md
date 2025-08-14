---
sidebar_position: 17

---

# Setting Descriptions

Descriptions should be supplied with each setting to explain what the setting does and any potential implications of changing it. Descriptions are provided within the `[Setting]` attribute.

A basic description might look like this:

``` csharp
[Setting("Turns on the debug mode", false)]
public bool DebugMode { get; set; }
```

![setting-description-tooltip](./img/setting-description-tooltip.png)

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

![setting-description-markdown](./img/setting-description-markdown.png)

## Admonitions Support

[Admonitions](https://docusaurus.io/docs/markdown-features/admonitions) within markdown files are supported, with special handling for certain admonition types:

### Internal Admonitions

The `internal` admonition type will be stripped from the content when processed by Fig. This allows you to include documentation that is only visible in the source markdown but not in the Fig UI. This is useful for developer notes, internal implementation details, or comments that shouldn't be shown to end users configuring the application.

```markdown
This content will be visible in Fig.

:::internal
This content will be stripped and won't appear in Fig.
It's useful for internal notes or developer comments.
:::

This content will also be visible in Fig.
```

### Fig Exclude Admonitions

The `figexclude` admonition type allows you to exclude specific content from appearing in Fig while keeping it visible in other documentation contexts (like generated documentation sites). This is useful when you have content that is relevant for general documentation but not for the configuration interface.

```markdown
This content will be visible in Fig.

:::figexclude
This content will be excluded from Fig but may appear in other documentation.
Use this for content that's relevant for docs but not for configuration.
:::

This content will also be visible in Fig.
```

Both `internal` and `figexclude` admonitions are completely removed from the final description shown in Fig, along with any document frontmatter.

## Setting Descriptions from Markdown Files

While the example above looks pretty good for the person configuring the application. It it is difficult to read for the developer. An easier way to manage the documentation is to store it in a markdown file which is an embedded resource in the application and then reference it in the fig configuration.

### Processing Notes

When Fig processes markdown files, it automatically:

- Strips YAML frontmatter (content between `---` markers at the beginning of files)
- Removes `:::internal` and `:::figexclude` admonition blocks
- Converts internal links (non-HTTP/HTTPS/mailto/data URLs) to bold text
- Embeds images as base64 data URLs

### Setup Steps

1. Create a markdown file within the project (entry assembly) and give it a name (it doesn't matter what)
2. Make the markdown file an embedded resource in the project
3. Write your documentation within the markdown file
4. Reference the file within fig using the following syntax:

```csharp
$FullyQualifiedResourceName
```

OR

```csharp
$filenameWithoutExtension
```

Note: if using just the filename, there is a small risk of conflicting names. In that case, the first one will be used.

For example

```csharp
$Fig.Integration.SqlLookupTableService.ServiceDescription.md
```

OR

```csharp
$ServiceDescription
```

However, there might be many settings and in this case you don't want to create a markdown file per setting. Fig allows you to specify a section of a markdown file using the following syntax:

```csharp
$FullyQualifiedResourceName#HeadingName
```

OR

```csharp
$filenameWithoutExtension#HeadingName
```

For example

```csharp
$Fig.Integration.SqlLookupTableService.ServiceDescription.md#FigUri
```

OR

```csharp
$ServiceDescription#FigUri
```

This will take all the text and subheadings below that heading block, but not that heading block itself.

You can see a full working example of this [here](https://github.com/mzbrau/fig/blob/main/src/integrations/Fig.Integration.SqlLookupTableService/Settings.cs#L11).

Fig even supports multiple files separated by a comma. For example:

```csharp
$Service.ServiceDescription.md#FigUri,$Service.OtherDoc.md
```

Each section can be a full document or part of a document. A line is inserted between documents. Documents are added in the order they are provided.

## Images

Fig supports displaying images (`.png` and `.svg` files) in both setting descriptions and client descriptions.

To add images, take the following steps:

1. Reference the image in your markdown file e.g. `![MyImage](C:\Temp\MyImage.png)`
2. Add the image as an **embedded resource** in your application
3. That's it, Fig will do the rest. What happens behind the scenes is that Fig will replace the image path with a base64 encoded version of the image which means it can be embedded in the document. This is the version that is registered with the API.

In the image below, the Fig logo has been added to the markdown file and appears in the setting description.

![setting-description-image](./img/setting-description-image.png)

## Links

External links (HTTP, HTTPS, mailto, and data URLs) are retained in the processed markdown. However, internal links (links to other markdown files or relative paths) are converted to bold text by Fig and don't function as clickable links in the UI. For example:

- `[External Link](https://example.com)` → remains as a clickable link
- `[Internal Link](./other-file.md)` → becomes **Internal Link** (bold text)
- `[Another Internal Link](../docs/guide.md)` → becomes **Another Internal Link** (bold text)

This ensures that the text content is preserved while preventing broken links in the Fig interface.
