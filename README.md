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

[![Build][build-shield]][build-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![Apache 2.0 License][license-shield]][license-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/mzbrau/fig">
    <img src="resources/image/fig_logo_name_below_orange_500x820.png" alt="Logo" width="125" height="205">
  </a>


  <p align="center">
    Centralized settings management for dotnet microservices.
    <br />
    <a href="http://www.figsettings.com/"><strong>Explore the docs »</strong></a>
    <br />
    <br />
  </p>

</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#what-is-fig">What is Fig?</a></li>
    <li><a href="#quick-start">Quick Start</a></li>
     <li><a href="#built-with">Built With</a></li>
    <li><a href="#features">Features</a></li>
    <li><a href="#getting-started">Getting Started</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->

## What is Fig?

Fig is a complete solution for managing settings across multiple micro-services. It consists of three components: a client library, API and web front end.

![product-diagram](https://github.com/mzbrau/fig/blob/main/resources/image/fig_architecture.excalidraw.png)

To add your application to Fig, add the [Fig.Client](https://www.nuget.org/packages/Fig.Client) nuget package to your application and register it as a configuration provider (see [documentation](https://www.figsettings.com/docs/intro) for details). You also need an environment variable to tell your app the API address. The Fig API and Blazor Web Application can run in containers locally or in the cloud. Fig is able to manage any number of applications including full configuration support, online status and much more.

![webpage-screenshot](https://github.com/mzbrau/fig/blob/main/resources/image/fig_website_settings_screenshot.png)

[![Configuration With Fig](https://img.youtube.com/vi/-2Bth4m0RcM/0.jpg)](https://youtu.be/-2Bth4m0RcM?si=j_aNAFQQeM_Y6aTA)


### Quick Start

To see a running Fig solution, clone and run the [Fig Quick Start](https://github.com/mzbrau/fig-quick-start) repository.

### Built With

* [dotnet 10](https://dotnet.microsoft.com/en-us/)
* [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)

<p align="right">(<a href="#top">back to top</a>)</p>

## Features

- Free and Open Source
- Modern, built on latest dotnet technology
- Complete settings management
  - Offline settings support
  - Centrally manage settings
  - Setting history and audit logging
  - Import & Export of settings and values
  - Live reload of settings
  - Remote restart of settings clients
  - Configuration Provider (support for IOptions<T>)
  - Secure - authentication required for settings management
  - Automatic settings registration
- Features to improve setting management
  - Setting descriptions driven from markdown documentation (including images)
  - Default values driven by application
  - Different editors depending on setting type
  - Complex value validation
  - Secret setting support
  - Settings grouping
  - Setting verification support using extensible framework
  - Hide settings with reasonable defaults
  - Lookup tables for improved setting translation
  - Instance support to vary settings for individual clients
- Stateless backend for scalability
- Seamless Aspire integration

<p align="right">(<a href="#top">back to top</a>)</p>

## Getting Started

Read the quickstart guide [here](http://www.figsettings.com/docs/intro)

Examples can be found [here](https://github.com/mzbrau/fig/tree/main/examples).

There is also a quick start repository [here](https://github.com/mzbrau/fig-quick-start).

## Authentication Modes

Fig supports two API/web authentication modes:

- `FigManaged` (default): users authenticate directly against Fig (`/users/authenticate`).
- `Keycloak`: users authenticate through Keycloak (OIDC/JWT). Fig user-management endpoints are not available in this mode.

For full setup, claim mapping, endpoint behavior, and troubleshooting, see:

- [Security Features](./doc/fig-documentation/docs/security.md)
- [User Management](./doc/fig-documentation/docs/features/3-user-management.md)

## Fig NuGet Packages

Fig provides several NuGet packages to support different integration scenarios and environments:

### Core Packages

#### [Fig.Client](https://www.nuget.org/packages/Fig.Client)

The main client library for integrating Fig into your applications. This is the primary package you'll need for most scenarios.

- **Description**: Client library to include in your project when using Fig managed settings
- **Target Framework**: .NET Standard 2.0
- **Usage**: Configuration management, settings integration
- **Documentation**: [Client Configuration](./client-configuration.md)

#### [Fig.Client.Abstractions](https://www.nuget.org/packages/Fig.Client.Abstractions)

Lightweight abstractions and attributes for Fig configuration settings that can be referenced by third-party libraries without requiring the full Fig.Client package.

- **Description**: Abstractions and attributes for Fig configuration settings
- **Target Framework**: .NET Standard 2.0
- **Usage**: Third-party library integration, minimal dependencies
- **Documentation**: See active pull request for integration scenarios

### Secret Provider Packages

Fig supports secure secret management through specialized provider packages:

#### [Fig.Client.SecretProvider.Azure](https://www.nuget.org/packages/Fig.Client.SecretProvider.Azure)

Azure Key Vault integration for secure secret management.

- **Description**: Fig secret provider for Azure Key Vault
- **Documentation**: [Azure KeyVault Integration](./features/26-azure-keyvault-integration.md)

#### [Fig.Client.SecretProvider.Aws](https://www.nuget.org/packages/Fig.Client.SecretProvider.Aws)

AWS Secrets Manager integration for secure secret management.

- **Description**: Fig secret provider for AWS Secrets Manager

#### [Fig.Client.SecretProvider.Google](https://www.nuget.org/packages/Fig.Client.SecretProvider.Google)

Google Cloud Secret Manager integration for secure secret management.

- **Description**: Fig secret provider for Google Cloud Secret Manager

#### [Fig.Client.SecretProvider.Docker](https://www.nuget.org/packages/Fig.Client.SecretProvider.Docker)

Docker secrets integration for containerized environments.

- **Description**: Fig secret provider for Docker secrets

#### [Fig.Client.SecretProvider.Dpapi](https://www.nuget.org/packages/Fig.Client.SecretProvider.Dpapi)

Windows Data Protection API (DPAPI) integration for Windows environments.

- **Description**: Fig secret provider for DPAPI
- **Platform**: Windows only

### Testing and Development Packages

#### [Fig.Client.Testing](https://www.nuget.org/packages/Fig.Client.Testing)

Testing framework for Fig clients that allows developers to unit and integration test settings-related functionality.

- **Description**: A testing framework for Fig clients for unit and integration testing
- **Usage**: Unit testing, integration testing, development workflows

#### [Fig.Client.Contracts](https://www.nuget.org/packages/Fig.Client.Contracts)

Internal contracts and interfaces used by Fig client components.

- **Description**: Fig client contracts
- **Usage**: Internal package, typically not directly referenced

### Aspire

#### [Fig.Aspire](https://www.nuget.org/packages/Fig.Aspire)

Contains extension methods for using Fig with [Aspire](https://aspire.dev/).

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- ROADMAP -->

## Roadmap

Fig is getting close to being feature complete but is accepting suggestions for new features and improvements.

Included in the roadmap are:

- [ ] End to end integration tests with Playwright 
- [ ] Oauth 2.0 Support
- [ ] Minor fixes and improvements

See the [open issues](https://github.com/mzbrau/fig/issues) to suggest something else.

<p align="right">(<a href="#top">back to top</a>)</p>


<!-- CONTRIBUTING -->
## Contributing

If you have any suggestions, please open an issue with the tag 'enhancement'. Don't forget to star the page if the project is useful to you.

If you are interested in contributing to the development, please raise a pull request.

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

Fig stands on the shoulders of giants. Many thanks to all the open source software that makes it possible.

* [JetBrains - Provided Rider licence as part of their support for open source projects](https://jb.gg/OpenSourceSupport) - A big thank you to them for making this project possible.
* [Jason Watmore's blog - user management and auth tokens](https://jasonwatmore.com/post/2022/01/07/net-6-user-registration-and-login-tutorial-with-example-api)
* [Danien Bod's GitHub - Encryption examples](https://github.com/damienbod/SendingEncryptedData)
* [Radzen Blazor Components](https://blazor.radzen.com/) A fantastic set of UI components for Blazor.
* [Jint](https://github.com/sebastienros/jint) - Javascript interpretor for dotnet
* [Markdig](https://github.com/xoofx/markdig) - Markdown documentation support
* [Benchmark.NET](https://github.com/dotnet/BenchmarkDotNet)
* [MinVer](https://github.com/adamralph/minver)
* [Moq](https://github.com/devlooped/moq)
* [Polly](https://github.com/App-vNext/Polly)
* [Blazor Hot Keys](https://github.com/jsakamoto/Toolbelt.Blazor.HotKeys2)
<!--* []() -->

<p align="right">(<a href="#top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/mzbrau/fig.svg
[contributors-url]: https://github.com/mzbrau/fig/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/mzbrau/fig.svg
[forks-url]: https://github.com/mzbrau/fig/network/members
[stars-shield]: https://img.shields.io/github/stars/mzbrau/fig.svg
[stars-url]: https://github.com/mzbrau/fig/stargazers
[issues-shield]: https://img.shields.io/github/issues/mzbrau/fig.svg
[issues-url]: https://github.com/mzbrau/fig/issues
[license-shield]: https://img.shields.io/github/license/mzbrau/fig.svg
[license-url]: https://github.com/mzbrau/fig/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/linkedin_username
[product-screenshot]: images/screenshot.png
[build-shield]: https://img.shields.io/github/actions/workflow/status/mzbrau/fig/dotnet_build.yml?branch=main
[build-url]: (https://github.com/mzbrau/fig/actions/workflows/dotnet_build.yml/badge.svg)