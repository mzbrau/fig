---
sidebar_position: 2


---

# Client Secret Migration

The client secret is used to safeguard the setting values for any specific setting client. This guide explains how the secret can be changed.

1. Log into the web application as an administrator
2. Select the setting client that you wish to migrate
3. Click the 'change client secret' button in the top right hand corner
4. In the dialog you can either generate a new secret (which is a guid) or you can supply your own. It must be a guid. Take a copy of the secret that has been generated.
5. Select the date and time when the previous secret will expire. You should give yourself long enough to update all instances of the client to start using the new secret. Once the old secret has expired, any client attempting to use it to register, report status or request settings will get an unauthenticated response.
6. Click change
7. Change the client secret on every instance of the setting client. Note that you can use the DPAPI tool that is under code in the repository to encrypt the secret if you are deploying on Windows.