---
sidebar_position: 3
---

# Comparison To Alternatives

Modern dotnet applications might be configured to draw from a range of different configuration providers. This provides a lot of flexibility but can also be confusing for those configuring the application. Fig is also a configuration provider and as such, can work along side other configuration sources. However, fig is more than just a configuration provider. It is a complete solution for managing settings across multiple micro-services. This is because when an application starts up, it registers its configuration with Fig meaning those settings are now viewable and editable from within the Fig web application.

Consider Fig if your solution:

- Is comprised of multiple applications or services requiring settings
- Requires settings changes depending on the installation for example in different environments or at different installation sites
- Is written in dotnet

Do not consider fig if your solution:

- Is not dotnet based
- Does not have configuration settings
- Is comprised of a single application (you can still use Fig here, but the benifits are reduced)
- Is only installed in a single production environment where settings are rarely changed.
