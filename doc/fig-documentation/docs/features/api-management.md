---
sidebar_position: 11
---

# API Management

The Fig API is stateless and as a result, it is possible to run multiple instances of the API pointing towards the same database. 

Plug in verifications must be installed on all API instances.

All API instances must have the same server secret as this is used for encryption in the database and token generation.

The API Status page shows the API's that are currently running in the system along with a number of details relating to that API.

## Appearance

![image-20220802222209450](../../static/img/image-20220802222209450.png)