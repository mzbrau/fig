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
[![MIT License][license-shield]][license-url]
[![codecov](https://codecov.io/gh/mzbrau/fig/branch/main/graph/badge.svg?token=ABYY27W5TS)](https://codecov.io/gh/mzbrau/fig)

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
<!-- ABOUT THE PROJECT -->

## What is Fig?

Fig is a complete solution for managing settings across multiple micro-services. It consists of three components: a client library, API and web front end. 

![product-diagram](https://github.com/mzbrau/fig/blob/main/resources/image/fig_diagram.png)

The client library should be added to a micro-service (or other app) that should have its settings managed by Fig. This will be a nuget package in the future to make installation easier. It will communicate with the API and allow settings for the service to be managed in the Fig web front end in a secure way.

![webpage-screenshot](https://github.com/mzbrau/fig/blob/main/resources/image/fig_website_settings_screenshot.png)

<!--Each of these features are outlined in more detail in the Features section below. -->

### Built With

* [dotnet 7](https://dotnet.microsoft.com/en-us/
* [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)

<p align="right">(<a href="#top">back to top</a>)</p>

## Features

- Free and Open Source
- Complete settings management
- Read configuration on startup
- Support for default values
- Live reload of configuration
- Dependency Injectable
- Mock in unit tests
- Concrete types
- Secret settings
- Common settings across services
- Setting value validation
- Guidance on valid values
- Change log auditing
- Setting value history
- Manage configuration for multiple services in one place
- Configuration managed only by authenticated users
- Configuration export for multiple services
- Manage configuration remotely
- Promote commonly changed settings
- Configure-time verification of configuration
- Vary configuration across instances
- Remove service restart

<p align="right">(<a href="#top">back to top</a>)</p>



## Getting Started

Read the quickstart guide [here](http://www.figsettings.com/docs/intro)

Examples can be found [here](https://github.com/mzbrau/fig/tree/main/examples).

<p align="right">(<a href="#top">back to top</a>)</p>

<!-- ROADMAP -->

## Roadmap

- [ ] End to end integration tests with Playwright 
- [ ] More unit testing
- [ ] Improvements to the Fig Web Application

See the [open issues](https://github.com/mzbrau/fig/issues) for a full list of proposed features (and known issues).

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

### Setting JSON (Unsupported types)

![webpage-json](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_json_setting.gif)

### Event History

![webpage-eventhistory](https://github.com/mzbrau/fig/blob/main/resources/gif/fig_events.gif)


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
[build-shield]: https://img.shields.io/github/workflow/status/mzbrau/fig/build%20and%20test/main
[build-url]: (https://github.com/mzbrau/fig/actions/workflows/dotnet_build.yml/badge.svg)