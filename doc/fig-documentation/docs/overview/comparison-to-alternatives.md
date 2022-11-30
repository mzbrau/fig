---
sidebar_position: 3
---

# Comparison To Alternatives

When using dotnet you have a wide range of options for providing settings to your application such as app.config files, appsettings.json files, registry settings, environment variables and built in dotnet settings management. Depending on where you deploy your application there are also options such as kubernetes key stores and hashicorp vault. 

All of these options may be good choices depending on your application and deployment types but they do not offer the comprehensive settings management suite offered by Fig. We are not aware of any other solution that offers a similar feature set.

Features such as automatic settings registration, validation regexes and verifiers are quite unique and can enhance your soultion configuration options.

Consider Fig if your solution:

- Is comprised of multiple applications or services requiring settings
- Requires settings changes depending on the installation for example in different environments or at different installation sites
- Is written in dotnet (framework or core / 5,6,7)

Do not consider fig if your solution:

- Is not dotnet based
- Does not have configuration settings
- Is comprised of a single application (you can still use Fig here, but the benifits are reduced)
- Is only installed in a single production environment where settings are rarely changed