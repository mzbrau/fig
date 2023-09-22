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
- File imports and dynamic verifiers can be disabled
- New setting registrations can be disabled
- User roles only have access to setting and history information (minus user activity)

# Security Recommendations

The following recommendations will ensure your application settings are as safe as possible.

1. **Use secret settings** - Secret settings are not sent down to the web client and not shown once they are entered. They should be used for passwords, keys and any other sensitive values.
1. **HTTPS everywhere** - Fig should be deployed with HTTP for both API and Client. Setting values are transmitted unencrypted to the setting clients so HTTPS will ensure those values cannot be intercepted while in transit.
1. **Disabling the administrator login** - Fig ships with an administrator login 'admin' with 'admin' as the password. The API can be configured to require a password change for that user on first login. The user can also be removed and replace with other administrative logins. 
1. **Strong Passwords** - Fig has a password rating view where you set your password. It will not accept any passwords rated worse than 'Good'.
1. **Dedicated user accounts** - Each user of fig should be allocated their own account. This will ensure the audit log accurately reflects who made the change. If all changes are made by Admin it won't add much value.
1. **SQL Server Security** - Fig uses Sqllite out of the box but should be changed to SQL server for production deployments. All setting values are encrypted in the sql database but it is good to ensure that is also secure.
1. **Disabling dynamic verifications** - Dynamic verifications allow client code to be executed on the server. It should be disabled if you don't fully trust all the connected clients.
1. **Disabling new registrations** - The fig registration endpoint is unsecured. This means any client is able to register with fig. It is possible to turn off new client registrations and this should be done in production once all known clients have registered with fig.
1. **Rolling API Secret** - The API secret is used to sign login tokens as well as encrypt all settings in the database. It can be changed at any time, however the old client secret must be retained to decrypt existing values in the database.
1. **Changing client secrets** - Client secrets can be changed during runtime using the web client. Clients need to be updated within the grace period.
1. **Protect client secrets** - Client secrets protect the values for that client and as a result, they should be kept secret. Fig supports 4 ways of reading client secrets. For windows installations, the DPAPI is the recommended way to store secrets. It will protect them for the user. For other installations, a secret store pushing to an environment variable or appsettings.json file is recommended.
1. **Web Hook Alerts** - Setting up web hook alerts will ensure you are kept informed if settings are changed.
1. **Disable Client Overrides** - If client overrides are not being used, disable this feature or at least limit it to the clients that should have access. This can avoid unwanted consequences.
