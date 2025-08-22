---
sidebar_position: 5
---

# Security Features

Fig has a number of security features to ensure your settings remain safe.

Features include:

- All settings values are encrypted in the database using the server secret as the encryption key
- Fig web application is protected with user credentials
- Admin user can be forced to change password on first login
- Fig only accepts 'good' passwords as rated by [zxcvbn](https://github.com/dropbox/zxcvbn)
- Secret setting values are never sent to the Fig Web Application
- Clients must use their secret to access their settings
  - Secrets can be securely stored in a number of different locations
- All actions are logged and recorded in the database
- Offline settings files are encrypted using the client secrets
  - Offline settings can also be disabled
- File imports can be disabled
- New setting registrations can be disabled
- User roles only have access to setting and history information (minus user activity)

## SIEM Integration

Fig provides built-in integration with Security Information and Event Management (SIEM) systems through webhooks. This allows organizations to monitor security events in real-time and integrate Fig with their existing security infrastructure.

### Security Events

Fig automatically generates security events for the following activities:

- **Login attempts** (both successful and failed)
- **User creation** (when new users are registered)

Each security event includes:

- **Event Type**: The type of security event (e.g., "Login")
- **Timestamp**: UTC timestamp when the event occurred
- **Username**: The username associated with the event
- **Success**: Whether the operation was successful
- **IP Address**: The IP address of the client making the request
- **Hostname**: The hostname of the client making the request
- **Failure Reason**: If the operation failed, the reason for failure

### Setting Up SIEM Integration

To integrate Fig with your SIEM system:

1. **Create a webhook endpoint** in your SIEM system or middleware that can receive HTTP POST requests
2. **Register the webhook** in Fig by navigating to the webhooks section in the web interface
3. **Select "Security Event"** as the webhook type
4. **Configure the endpoint URL** where Fig should send security events

### Example Security Event

```json
{
  "eventType": "Login",
  "timestamp": "2024-01-15T10:30:00Z",
  "username": "admin",
  "success": false,
  "ipAddress": "192.168.1.100",
  "hostname": "workstation-01",
  "failureReason": "Invalid password"
}
```

# Security Recommendations

The following recommendations will ensure your application settings are as safe as possible.

1. **Use secret settings** - Secret settings are not sent down to the web client and not shown once they are entered. They should be used for passwords, keys and any other sensitive values.
1. **HTTPS everywhere** - Fig should be deployed with HTTP for both API and Client. Setting values are transmitted unencrypted to the setting clients so HTTPS will ensure those values cannot be intercepted while in transit.
1. **Disabling the administrator login** - Fig ships with an administrator login 'admin' with 'admin' as the password. The API can be configured to require a password change for that user on first login. The user can also be removed and replace with other administrative logins. 
1. **Strong Passwords** - Fig has a password rating view where you set your password. It will not accept any passwords rated worse than 'Good'.
1. **Dedicated user accounts** - Each user of fig should be allocated their own account. This will ensure the audit log accurately reflects who made the change. If all changes are made by Admin it won't add much value.
1. **SQL Server Security** - Fig uses Sqllite out of the box but should be changed to SQL server for production deployments. All setting values are encrypted in the sql database but it is good to ensure that is also secure.
1. **Disabling new registrations** - The Fig registration endpoint is unsecured. This means any client is able to register with Fig. It is possible to turn off new client registrations and this should be done in production once all known clients have registered with Fig.
1. **Rolling API Secret** - The API secret is used to sign login tokens as well as encrypt all settings in the database. It can be changed at any time, however the old client secret must be retained to decrypt existing values in the database. See [the guide](http://www.figsettings.com/docs/guides/api-secret-migration) for steps.
1. **Protect API Secret** - If the API secret is compromised then it will be possible to decrypt values in the database (assuming that they can access the database). It is important that it be protected either by storing it in DPAPI (Windows only) or as a docker secret.
1. **Changing client secrets** - Client secrets can be changed during runtime using the web client. Clients need to be updated within the grace period. See [the guide](http://www.figsettings.com/docs/guides/client-secret-migration) for steps.
1. **Protect client secrets** - Client secrets protect the values for that client and as a result, they should be kept secret. Fig supports 4 ways of reading client secrets. For windows installations, the DPAPI is the recommended way to store secrets. It will protect them for the user. For other installations, a secret store pushing to an environment variable or appsettings.json file is recommended.
1. **Web Hook Alerts** - Setting up web hook alerts will ensure you are kept informed if settings are changed.
1. **Disable Client Overrides** - If client overrides are not being used, disable this feature or at least limit it to the clients that should have access. This can avoid unwanted consequences.
1. **Enable TLS** - Fig supports TLS for both the Web and Api instances. See [the guide](http://www.figsettings.com/docs/guides/configuring-tls) for steps and example config.
