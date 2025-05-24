---
sidebar_position: 1
---

# API Secret Migration

The API Secret is used to sign and verify login tokens as well as to encrypt data within the database. 

It is good practice to change the secret periodically to mitigate any risk associated with the secret being compromised. This guide explains how the secret should be changed.

1. Verify that you do not have any deferred imports that have encrypted values.
2. Generate a new API secret. The secret can be anything but a good approach might be to concatenate a few guids together.
3. For each Fig API instance, copy the secret value to the PreviousSecret value and add your new secret to the secret value. You should do this wherever the API is configured, this could be in the `appSettings.json` file, environment variables or docker secrets.
4. Once all API instances have been updated, it should still be possible to use Fig. User sessions generated with the old secret are still respected and all data in the database can still be decrypted with the previous secret. Any new data added will be encrypted with the new secret.
5. Log into the web application as an administrator role and select the configuration page.
6. Take note of the number of event logs that need to be migrated. In testing, it seemed to take about 10 seconds per 1000 logs to migrate but this is likely to be quickly with a release build and on faster machines.
7. Press the migrate button, you will get a notification when the migration is complete.
8. Once migration is complete, you can remove the previous secret. Note that removing it will log out any users who logged in before the secret change.