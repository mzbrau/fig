---
sidebar_position: 20



---

# Client Settings Overrides

In some situations you want the settings to be driven by the client. For example when deploying applications using Docker compose if you have address to other containers within the compose file, you can reference them by their container name. In that case, you would like the setting to be set by the docker compose file rather than having to be manually set in Fig or use a default.

In this case, we can use the client settings override feature. Just set an environment variable with the exact same name as the setting you wish to set prefixed by the client name and the Fig client library will read that on startup and send that along with the registration information. If client overrides are enabled on the API those values will be used to update the setting value.

For example, imagine you have an application called ProductService with the following settings:

```c#
public class ProductService : SettingsBase
{
    public override string ClientDescription => "Sample Product Service";

    [Setting("This is the address of a website", "http://www.google.com")]
    public string WebsiteAddress { get; set; }
}
```

You could then define your docker compose file like this:

```
productserviceapp:
    image: myProductServiceImage:latest
    container_name: ProductServiceImage
    environment:
      - ProductService__WebSiteAddress=http://bing.com
```

If overrides are enabled for this client, the value 'http://bing.com' will be sent along with the registration of the client and will be used to update the value of WebsiteAddress.

Note: You will be able to change the value in the Fig web application but it will revert back to the override value again when you next restart the client application. If that is undesirable, you can always turn off the client override feature.

This feature is enabled by default but can be disabled in the fig configuration which is available when logged into the web application as an administrator.

It is also possible to enter a regular expression there to define which clients will be allowed to override their settings and which will not. The regular expression is evaluated against the client name.