---
sidebar_position: 11


---

# Setting Constants

Fig supports substituting live values with pre-defined constants in string settings so they are available to the using services.

For example, if there are 3 services installed on different machines and each requires the current machine name to be set in the settings, the setting value can be set to the constant value and each service will receive the machine name of the current machine where they are installed.

Most of the constants take their values from the `Environment` class in dotnet.

Available constants include:

| Constant          | Description                                                  | Example      |
| ----------------- | ------------------------------------------------------------ | ------------ |
| `${MachineName}`    | The hostname of the machine where the application using the setting is installed. | Server01     |
| `${Domain}`         | The domain of the machine where the application using the setting is installed. | MyDomain     |
| `${User}`           | The user logged into the machine where the application is running | User1        |
| `${IPAddress}`      | The IP Address of the machine where the application is running | 192.168.1.34 |
| `${ProcessorCount}` | The number of processors on the current machine              | 4            |
| `${OSVersion}`     | The version of the operating system where the application is running | 6.2.9200.0   |

