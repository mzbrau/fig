---
sidebar_position: 6
---

# User Management

Administrators in the Fig web client have the ability to manage users. They are able to create and delete users (or other administrators) as well as set and reset passwords.

![image-20230918214819526](../../static/img/image-20230918214819526.png)

## Client Filter Regex

It is possible to specify a regex to filter the clients available for a user. Logged in users will only see clients that match the specified filter. Any attempt to query or update clients that do not match the filter will result in an unauthorized from the server.

Administrators can also have filter regexes but as they are able to manage users, they'll be able to update their own.

## Managing your own Account

All users can change their own password using the avatar image in the top right corner.

![image-20220802233557475](../../static/img/manage-account.png)