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