using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Fig.Web.Services.Authentication;

public static class OidcProviderOptionsConfigurator
{
    public const string IdentityProviderHintParameterName = "kc_idp_hint";
    public const string PromptParameterName = "prompt";

    public static void Apply(OidcProviderOptions providerOptions, WebKeycloakAuthenticationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(providerOptions);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.EnableIdentityProviderHint &&
            !string.IsNullOrWhiteSpace(settings.IdentityProviderHint))
        {
            providerOptions.AdditionalProviderParameters[IdentityProviderHintParameterName] =
                settings.IdentityProviderHint.Trim();
        }

        if (!string.IsNullOrWhiteSpace(settings.LoginPrompt))
        {
            providerOptions.AdditionalProviderParameters[PromptParameterName] = settings.LoginPrompt.Trim();
        }
    }
}
