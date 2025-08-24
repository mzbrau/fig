---
sidebar_position: 1
---

# Fig Microsoft Sentinel Integration

This integration forwards Fig webhook events to Microsoft Sentinel for Security Information and Event Management (SIEM) monitoring.

:::warning[Experimental]

This integration is experimental and may not work as expected. Create a ticket on GitHub if a problem is found.

:::

## Features

- **Security Event Monitoring**: Forwards login attempts, user creation events, and other security events
- **Configuration Change Tracking**: Monitors setting value changes across your applications
- **Client Health Monitoring**: Tracks client registrations, status changes, and health events
- **Configurable Event Filtering**: Choose which event types to process and forward
- **Robust Error Handling**: Built-in retry logic with exponential backoff
- **Health Checks**: Monitor the integration's connection to Microsoft Sentinel

## Configuration

The integration uses Fig for configuration management. You'll need to configure the following settings in Fig:

### Required Settings

- **HashedSecret**: The hashed secret provided by Fig when configuring the webhook client
- **SentinelWorkspaceId**: Your Microsoft Sentinel workspace ID (Customer ID from workspace settings)
- **SentinelWorkspaceKey**: Your Microsoft Sentinel workspace primary or secondary key

### Optional Settings

- **SentinelLogType**: Custom log type name in Sentinel (default: "FigEvents")

## Setup

### 1. Microsoft Sentinel Workspace

1. In the Azure portal, navigate to your Microsoft Sentinel workspace
2. Go to **Settings** > **Workspace settings**
3. Copy the **Customer ID** (this is your `SentinelWorkspaceId`)
4. Go to **Agents management** > **Log Analytics agent instructions**
5. Copy the **Primary key** or **Secondary key** (this is your `SentinelWorkspaceKey`)

### 2. Fig Configuration

1. Register the integration as a webhook client in Fig
2. Configure the required settings mentioned above
3. Note the hashed secret provided by Fig for webhook authentication

### 3. Running the Integration

#### Using Docker

```bash
# Pull the image
docker pull mzbrau/fig-sentinel-integration:latest

# Run the container
docker run -d \
  --name fig-sentinel-integration \
  -p 8080:80 \
  -e FIG_API_URI="https://your-fig-api-url" \
  -e FIG_MicrosoftSentinelIntegration_SECRET="your-client-secret" \
  mzbrau/fig-sentinel-integration:latest
```

#### Using .NET

```bash
cd src/integrations/Fig.Integration.MicrosoftSentinel
export FIG_API_URI="https://your-fig-api-url"
export FIG_MicrosoftSentinelIntegration_SECRET="your-client-secret"
dotnet run
```

### 4. Configure Fig Webhooks

In the Fig web application:
1. Go to **Webhooks** section
2. Add a new webhook pointing to your integration endpoint
3. Set the URL to `http://your-integration-host:port/[EventType]`
4. Choose the event types you want to forward
5. Use the client secret for authentication

## Webhook Endpoints

The integration provides the following endpoints for different event types:

- `POST /SecurityEvent` - Security events (login attempts, user creation)
- `POST /SettingValueChanged` - Setting value changes
- `POST /ClientRegistration` - Client registrations
- `POST /ClientStatusChanged` - Client connection status changes
- `POST /ClientHealthChanged` - Client health status changes
- `POST /MinRunSessions` - Minimum run sessions events
- `GET /health` - Health check endpoint
- `GET /` - Basic service information

## Microsoft Sentinel Log Structure

Events are sent to Sentinel with the following structure:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "eventType": "SecurityEvent",
  "figEventType": "Login",
  "username": "admin",
  "success": false,
  "ipAddress": "192.168.1.100",
  "hostname": "workstation-01",
  "failureReason": "Invalid password",
  "source": "Fig",
  "integration": "MicrosoftSentinel",
  "severity": "Medium"
}
```

## Querying Logs in Sentinel

Once configured, logs will appear in Microsoft Sentinel under the custom log type (default: `FigEvents_CL`). You can query them using KQL:

```kql
// View all Fig events
FigEvents_CL
| order by TimeGenerated desc

// View failed login attempts
FigEvents_CL
| where eventType_s == "SecurityEvent"
| where success_b == false
| order by TimeGenerated desc

// View setting changes
FigEvents_CL
| where eventType_s == "SettingValueChanged"
| order by TimeGenerated desc
```

## Health Monitoring

The integration includes health checks accessible at `/health`. The health check validates the connection to Microsoft Sentinel by sending a test log entry.

## Security Considerations

- Use the webhook authentication feature to secure your integration endpoint
- Store the Sentinel workspace key securely (use Fig's secret setting type)
- Consider using HTTPS for the webhook endpoints in production
- Monitor the integration logs for any authentication or connectivity issues

## Troubleshooting

### Common Issues

1. **Authentication Failed**: Verify the HashedSecret matches what's configured in Fig
2. **Sentinel Connection Failed**: Check SentinelWorkspaceId and SentinelWorkspaceKey
3. **No Events Received**: Verify webhook configuration in Fig points to correct endpoints
4. **Events Not Appearing in Sentinel**: Check that at least one event type is enabled for processing

### Logs

The integration logs to both console and file (`logs/sentinel-integration-*.log`). Check these logs for detailed error information.

### Health Check

Use the `/health` endpoint to verify the integration's connection to Microsoft Sentinel:

```bash
curl http://your-integration-host:port/health
```

## Development

To extend or modify the integration:

1. Clone the Fig repository
2. Navigate to `src/integrations/Fig.Integration.MicrosoftSentinel`
3. Make your changes
4. Build and test: `dotnet build && dotnet run`

The integration follows Fig's established patterns and can be used as a template for other SIEM integrations.