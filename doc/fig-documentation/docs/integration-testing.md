---
sidebar_position: 7

---

# Integration Testing

It is possible to write integration tests for services using fig. If using Microsoft's DI framework, it is possible to overwrite items registered in the container for use in the component being tested. See [this link](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0) about how to write integration tests for ASP.NET core.

The following code snippet shows how to inject extra items into the container when integration testing.

```csharp
// Create a settings mock. This will be a mock of the interface to the settings that are managed by Fig. 
// This example uses Moq, you may prefer to use a different mocking framework such as NSubstitute
var settingsMock = new Mock<ISettings>();
application = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
{
    builder.ConfigureServices(services =>
    {
        // Register the object as a singleton. Now, anywhere you ask for an ISettings in the constructor, you will get
        // this mock rather than the real settings.
        services.AddSingleton(settingsMock.Object);
        // Overwrite the fig configuration provider. 
        //This is not strictly necessary but it prevents Fig attempting to make any REST calls to the Fig server.
        services.AddSingleton(Mock.Of<IFigConfigurationProvider>());
    });
});

// Setup your settings as you require for your integration tests.
settingsMock.Setup(a => a.MyProperty).Returns("Overridden value");
```

