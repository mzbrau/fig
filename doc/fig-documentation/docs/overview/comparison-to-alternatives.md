---
sidebar_position: 3
---

# Comparison To Alternatives

Modern dotnet applications might be configured to draw from a range of different configuration providers. This provides a lot of flexibility but can also be confusing for those configuring the application. Fig is also a configuration provider and as such, can work along side other configuration sources. However, fig is more than just a configuration provider. It is a complete solution for managing settings across multiple micro-services. This is because when an application starts up, it registers its configuration with Fig meaning those settings are now viewable and editable from within the Fig web application.

## Cloud Configuration Services

### AWS Systems Manager Parameter Store

AWS Systems Manager Parameter Store is a popular choice for storing configuration data and secrets in AWS environments.

**Why Fig is Better:**


- **Rich Web UI**: Fig provides a comprehensive web interface for managing settings, while Parameter Store relies on the AWS Console which lacks specialized configuration management features
- **Real-time Updates**: Fig pushes configuration changes to applications instantly without requiring application restarts or polling
- **Type Safety**: Fig provides strongly-typed configuration with validation, while Parameter Store treats all values as strings
- **Cross-Environment Management**: Fig can manage configurations across multiple environments and cloud providers, not just AWS
- **Audit Trail**: Fig provides detailed change history with user attribution, timestamps, and change descriptions
- **No Vendor Lock-in**: Fig can be deployed anywhere, while Parameter Store ties you to AWS infrastructure

### Azure App Configuration

Azure App Configuration is Microsoft's cloud-native configuration service for Azure applications.

**Why Fig is Better:**


- **Specialized .NET Integration**: Fig is built specifically for .NET applications and integrates seamlessly with the existing .NET configuration system
- **Advanced Features**: Fig includes features like lookup tables, custom validation scripts, dependent settings, and scheduled changes that App Configuration lacks
- **Multi-tenancy Support**: Fig supports multiple instances and client overrides out of the box
- **Self-hosted Option**: Fig can be deployed on-premises or in any cloud, while App Configuration is Azure-only
- **Cost Effectiveness**: Fig eliminates per-request charges and provides unlimited configuration reads
- **Enhanced Security**: Fig encrypts all values at rest and provides granular access control with user classifications

### Google Cloud Secret Manager

Google Cloud Secret Manager focuses primarily on secrets management with some configuration capabilities.

**Why Fig is Better:**

- **Configuration-First Design**: Fig is designed specifically for application configuration, not just secrets
- **Interactive Management**: Fig provides an intuitive web interface for configuration management, while Secret Manager is primarily API/CLI driven
- **Live Monitoring**: Fig shows real-time client status and health checks, providing visibility into which services are running and their configuration state
- **Rich Data Types**: Fig supports complex data types like data grids, nested objects, and custom validation, while Secret Manager is limited to key-value pairs
- **Change Management**: Fig includes features like scheduled changes, change approval workflows, and rollback capabilities

## Open Source Alternatives

### Consul by HashiCorp

Consul provides service discovery and configuration management capabilities.

**Why Fig is Better:**

- **Specialized .NET Support**: Fig integrates natively with .NET's configuration system, while Consul requires custom integration work
- **User-Friendly Interface**: Fig provides a purpose-built web UI for configuration management, while Consul's UI is primarily focused on service discovery
- **Configuration Validation**: Fig includes built-in validation, type checking, and custom validation scripts
- **Simpler Deployment**: Fig has fewer infrastructure requirements compared to Consul's cluster setup
- **Rich Feature Set**: Fig includes advanced features like time machine (configuration history), scheduled changes, and custom actions
- **No Programming Required**: Fig allows non-technical users to manage configurations through its web interface

### etcd

etcd is a distributed key-value store often used for configuration in Kubernetes environments.

**Why Fig is Better:**

- **Higher-Level Abstraction**: Fig provides configuration management at the application level rather than raw key-value storage
- **Web Interface**: Fig includes a comprehensive web UI, while etcd requires separate tools for visualization
- **Type Safety and Validation**: Fig ensures configuration correctness with type checking and validation rules
- **User Management**: Fig includes built-in user authentication and authorization
- **Configuration Documentation**: Fig allows inline documentation and descriptions for each setting
- **Change Tracking**: Fig provides detailed audit trails and change history

### Apache Zookeeper

Zookeeper is a centralized service for maintaining configuration information and distributed synchronization.

**Why Fig is Better:**

- **Modern Architecture**: Fig uses contemporary web technologies and RESTful APIs, while Zookeeper uses older protocols
- **Ease of Use**: Fig provides a user-friendly web interface compared to Zookeeper's command-line tools
- **Configuration-Specific Features**: Fig includes features designed specifically for application configuration management
- **Better Monitoring**: Fig provides real-time client monitoring and health checks
- **Simplified Operations**: Fig requires less operational overhead and complexity than maintaining a Zookeeper ensemble

## Traditional Configuration Methods

### JSON/XML Configuration Files

Traditional file-based configuration stored in version control.

**Why Fig is Better:**

- **Live Updates**: Fig allows configuration changes without application restarts or redeployments
- **Centralized Management**: Manage all microservice configurations from a single interface
- **Environment Management**: Easily manage different configurations across environments without code changes
- **Access Control**: Fig provides user-based access control, while files rely on filesystem permissions
- **Change Validation**: Fig validates changes before applying them, preventing configuration errors
- **Audit Trail**: Complete history of who changed what and when

### Environment Variables

Using environment variables for configuration management.

**Why Fig is Better:**

- **Rich Data Types**: Fig supports complex data structures, not just string values
- **Runtime Changes**: Update configurations without restarting applications or containers
- **Centralized Visibility**: See all configurations across all services in one place
- **Validation and Documentation**: Fig provides validation rules and documentation for each setting
- **Change Management**: Track changes with timestamps, user information, and change descriptions
- **Security**: Fig encrypts sensitive values and provides granular access control

## When to Choose Fig

Consider Fig if your solution:

- Is comprised of multiple .NET applications or services requiring settings
- Requires frequent configuration changes across different environments
- Benefits from a centralized configuration management interface
- Needs strong type safety and validation for configuration values
- Requires detailed audit trails and change management
- Values operational simplicity and reduced deployment complexity

Do not consider Fig if your solution:

- Is not .NET based
- Has minimal or static configuration requirements
- Is a single application with rarely-changing settings
