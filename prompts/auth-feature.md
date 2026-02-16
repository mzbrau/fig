# Auth Feature

Currently Fig handles authentication and authorization itself.

Users of the web client will call the UsersController and authenticate towards the api. Other users of the api will do the same.
Authenticate calls the UserService which verifies the credentails provided against the hashed version of the password stored in the database. It then generates a new auth token for the user, signed by the api's configured secret value.

The user can then use this token for future requests towards the api.

This works well but in some environments a single credential provider is used and Fig cannot work with that currently.

This task involves introducing Keycloak into the solution. Keycloak should be added to the Aspire dashboard and should allow web users to acquire a token which can then be used towards the back end api.

I would still like to retain the ability to use Fig only logins. This would be configured in the Fig.Api and Fig.Web configuration files. If both were configured for Fig managed logins, it would work exactly as it does today.

Otherwise, if Fig is configured for Keycloak logins, then keycloak would be the only trusted token provider allowed to call the api.

The security model should remain the same. Today Fig has the following roles:

- Administrator,
- User,
- LookupService,
- ReadOnly

Each of these roles have different permissions allowing them to call different endpoints within the API. These roles must remain but be configured within Keycloak as claims within the provided token. As a result, the user management and role assignment to users would be managed in Keycloak rather than Fig.
In addition, users today can also be assigned AllowedClassifications which detirmine which settings they will see and a Client Filter Regex which can be used to filter which clients they are able to see and update. These would also be managed in Keycloak in the future and just read by the api.

When in the Keycloak mode, the Users razor page would not longer be available to administrators as this detail would no longer be managed in Fig.

The client secret method of authentication used by the applications interacting with Fig will remain unchanged. They will not use Keycloak in any way.

The keycloak feature should be a smooth workflow where the user is automatically directed to Keycloak if they are not already logged in and once logged in, they are automatically directed back to the main Fig page so they can start working.

Based on the existing state of the code and the above plan, produce a detailed design that could be used as the basis for implementing this new feature.
Please ask any clarification questions required to produce the design and provide recommendations for the best way forward for each of them.