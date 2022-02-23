<div id="top"></div>




<!-- PROJECT SHIELDS -->
<!--
*** I'm using markdown "reference style" links for readability.
*** Reference links are enclosed in brackets [ ] instead of parentheses ( ).
*** See the bottom of this document for the declaration of the reference variables
*** for contributors-url, forks-url, etc. This is an optional, concise syntax you may use.
*** https://www.markdownguide.org/basic-syntax/#reference-style-links

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]
-->


<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/mzbrau/fig">
    <img src="resources/image/fig_logo_name_below_orange_500x820.png" alt="Logo" width="125" height="205">
  </a>


  <p align="center">
    Centralized settings management for dotnet microservices.
    <br />
    <a href="https://github.com/mzbrau/fig"><strong>Explore the docs »</strong></a>
    <br />
    <br />
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#what-is-fig">What is Fig?</a></li>
      <li><a href="#why-does-fig-exist">Why does fig exist?</a></li>
     <li><a href="#built-with">Built With</a></li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

## NOTE - Current Status
Note that this project is currently under development and there are no guarantees that it will work or contain all features when you clone it. This page will be updated as progress continues. A beta release is expected Q2 2022. 

<!-- ABOUT THE PROJECT -->
## What is Fig?

Fig is a complete solution for managing settings across multiple micro-services. It consists of three components: a client library, API and web front end. 

![product-diagram](https://github.com/mzbrau/fig/blob/main/resources/image/fig_diagram.png)

The client library should be added to a micro-service (or other app) that should have its settings managed by Fig. This will be a nuget package in the future to make installation easier. It will communicate with the API and allow settings for the service to be managed in the Fig web front end in a secure way.

![webpage-screenshot](https://github.com/mzbrau/fig/blob/main/resources/image/fig_website_settings_screenshot.png)

<p align="right">(<a href="#top">back to top</a>)</p>

## Why does Fig exist?

If you have ever created a service or other application, you probably found at some stage you needed to add some configurable components. This is particularly the case if the application could be deployed into multiple environments. 
This could be things like:
- an API URL;
- a timeout value;
- credentials to access an external service; or 
- a logging level.

There are many ways of passing this information to your application including **command parameters** or **environment variables**, however back in dotnet framework days, this was most commonly achieved using an `app.config` file and using `ConfigurationManager.AppSettings["SettingName"]`. This worked ok but had a lot of shortcomings.

Fast forward to dotnet core/6 you would probably use an `appsettings.json` file and then inject an `IOptions<T>` where it is required to access your settings. This pattern addressed many problems with the app.config structure including live reload, some support for concrete types and better testing support. However, there are still many problems. This is where Fig comes in. A comparison of these different options compared to fig is shown below.

|                                                         | app.config                   | appsettings.json               | Fig                         |
|---------------------------------------------------------|------------------------------|--------------------------------|-----------------------------|
| Read configuration on startup                           | :white_check_mark:           | :white_check_mark:             | :white_check_mark:          |
| Support for default values                              | :white_check_mark: (partial) | :white_check_mark: (partial)   | :white_check_mark:          |
| Live reload of configuration                            |                              | :white_check_mark:             | :white_check_mark: (roadmap)|
| Dependency Injectable                                   |                              | :white_check_mark:             | :white_check_mark:          |
| Mock in unit tests                                      |                              | :white_check_mark: (partial)   | :white_check_mark:          |
| Concrete types                                          |                              | :white_check_mark: (partial)   | :white_check_mark:          |
| Secret settings                                         |                              |                                | :white_check_mark:          |
| Common settings across services                         |                              |                                | :white_check_mark:          |
| Setting value validation                                |                              |                                | :white_check_mark:          |
| Guidance on valid values                                |                              |                                | :white_check_mark:          |
| Change log auditing                                     |                              |                                | :white_check_mark: (roadmap)|
| Setting value history                                   |                              |                                | :white_check_mark: (roadmap)|
| Manage configuration for multiple services in one place |                              |                                | :white_check_mark: (roadmap)|
| Configuration managed only by authenticated users       |                              |                                | :white_check_mark:          |
| Configuration export for multiple services              |                              |                                | :white_check_mark: (roadmap)|
| Manage configuration remotely                           |                              |                                | :white_check_mark:          |
| Promote commonly changed settings                       |                              |                                | :white_check_mark: (roadmap)|
| Configure-time verification of configuration            |                              |                                | :white_check_mark: (roadmap)|
| Vary configuration across instances                     | :white_check_mark: (manual)  | :white_check_mark: (manual)    | :white_check_mark: (roadmap)|

<!--Each of these features are outlined in more detail in the Features section below. -->

<p align="right">(<a href="#top">back to top</a>)</p>


### Built With

* [dotnet 6](https://dotnet.microsoft.com/en-us/)
* [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)

<p align="right">(<a href="#top">back to top</a>)</p>



## Getting Started

At this stage, Fig has not been packaged to make it easy to use. This will come in the future. This means you will need to download and compile all the components yourself.

### Prerequisites

A dotnet development environment with dotnet 6 support such as Visual Studio 2022 or Rider.

### Installation

1. Clone the repo
   ```sh
   git clone https://github.com/mzbrau/fig.git
   ```
2. Set an API secret in `appsettings.json` (under Fig.API project). Recommendation is to generate a few GUID's and combine them.
   ```
     "ApiSettings": {
       "Secret": "<your secret here>",
       "TokenLifeMinutes": 10080
     }
   },
   ```
3. Run both the Fig.Api project and Fig.Web project
   ```
   cd fig/src/api/fig.api
   dotnet run
   ```

   ```
   cd fig/src/web/fig.web
   dotnet run
   ```
4. Register and use your settings. See example below.
5. Visit `https://localhost:7148/` to view and edit setting values. Login credentials are:
   User: admin
   Password: admin
   Users can currently not be managed via the webpage but this is supported in the API.

<p align="right">(<a href="#top">back to top</a>)</p>


## Usage

To use the settings client, create a project and reference the client library.
Create a settings class with the settings you need and reference the provided base class `SettingsBase`. For example:
```
public class ExampleSettings : SettingsBase, IExampleSettings
{
    public override string ClientName => "ExampleService";
    public override string ClientSecret => "87c20b6a-9159-4daa-a171-9e297f47e08d";

    [Setting("My favourite animal", "Cow")]
    public string FavouriteAnimal { get; set; }

    [Setting("My favourite number", 66)]
    public int FavouriteNumber { get; set; }
    
    [Setting("True or false, your choice...", false)]
    public bool TrueOrFalse { get; set; }
}
```

In the example, there is also an interface which will expose the settings to the application. This is not required but is recommended for test mocking.
```
public interface IExampleSettings
{
    string FavouriteAnimal { get; }
    int FavouriteNumber { get; }
    bool TrueOrFalse { get; }
}
```

Then we just register and initialize the settings. 
For a console application:
```
var figOptions = new FigOptions();
figOptions.StaticUri("https://localhost:7281"); // Fig API URI
var provider = new FigConfigurationProvider(figOptions, log => Console.WriteLine(log));
IExampleSettings settings = await provider.Initialize<ExampleSettings>();
Console.WriteLine($"Favourite Animal: {settings.FavouriteAnimal}");
```

For dependency injection (ASP.NET core):
```
TODO
```

<!--_For more examples, please refer to the [Documentation](https://example.com)_-->

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- ROADMAP -->
## Roadmap

- [ ] Documentation site
- [ ] Github continuous integration pipeline
- [ ] Instance support on web (already supported in API)
- [ ] Change log auditing
- [ ] Setting value history
- [ ] Config export / import
- [ ] Support 'advanced' settings in the web
- [ ] Setting verification support in web (already supported in API)
- [ ] Setting grouping
- [ ] Setting live reload (polling)
- [ ] Web UI Improvements
- [ ] Productify (nuget for client, docker images for server)

<!--See the [open issues](https://github.com/mzbrau/fig/issues) for a full list of proposed features (and known issues). -->

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

As the project is under initial development, unsolicitated code contributions are currently not welcome. We hope to open this up once we reach a beta release.
If you have any suggestions, please open an issue with the tag 'enhancement'. Don't forget to star the page if the project is useful to you.
<!--Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
-->

<p align="right">(<a href="#top">back to top</a>)</p>


<!-- GIFS -->
## Gifs

The following gifs provide a sample of how a user interacts with the Fig webpage when managing settings.

### Login

![webpage-login](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_login.gif)

### Setting Validation

![webpage-validation](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_validation.gif)

### Setting Buttons

![webpage-setting-buttons](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_setting_buttons.gif)

### Groups

![webpage-groups](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_groups.gif)

### Setting History

![webpage-history](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_setting_history.gif)

### Setting Verification

![webpage-verification](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_verification.gif)

### Setting Data Grid (Complex settings)

![webpage-datagrid](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_datagrid.gif)


<!-- LICENSE -->
## License

Distributed under the Apache 2.0 License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

<!--Your Name - [@twitter_handle](https://twitter.com/twitter_handle) - email@email_client.com -->

Project Link: [https://github.com/mzbrau/fig](https://github.com/mzbrau/fig)

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [JetBrains - Provided Rider licence as part of their support for open source projects](https://jb.gg/OpenSourceSupport)
* [Jason Watmore's blog - user management and auth tokens](https://jasonwatmore.com/post/2022/01/07/net-6-user-registration-and-login-tutorial-with-example-api)
* [Danien Bod's github - Encryption examples](https://github.com/damienbod/SendingEncryptedData)
* [Radzen Blazor Components](https://blazor.radzen.com/)
* [Nate McMasters's plugin framework](https://github.com/natemcmaster/DotNetCorePlugins)
<!--* []() -->

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/mzbrau/fig.svg?style=for-the-badge
[contributors-url]: https://github.com/mzbrau/fig/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/mzbrau/fig.svg?style=for-the-badge
[forks-url]: https://github.com/mzbrau/fig/network/members
[stars-shield]: https://img.shields.io/github/stars/mzbrau/fig.svg?style=for-the-badge
[stars-url]: https://github.com/mzbrau/fig/stargazers
[issues-shield]: https://img.shields.io/github/issues/mzbrau/fig.svg?style=for-the-badge
[issues-url]: https://github.com/mzbrau/fig/issues
[license-shield]: https://img.shields.io/github/license/mzbrau/fig.svg?style=for-the-badge
[license-url]: https://github.com/mzbrau/fig/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/linkedin_username
[product-screenshot]: images/screenshot.png