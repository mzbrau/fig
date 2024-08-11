using System.Diagnostics;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Validators;

public static class RegistrationStatusValidator
{
    public static CurrentRegistrationStatus GetStatus(List<SettingClientBusinessEntity>? existingRegistrations, string clientSecret)
    {
        if (existingRegistrations == null || !existingRegistrations.Any())
            return CurrentRegistrationStatus.NoExistingRegistrations;
            
        var firstRegistration = existingRegistrations.First();
        return GetStatus(firstRegistration, clientSecret);
    }

    public static CurrentRegistrationStatus GetStatus(ClientBase client,
        string clientSecret)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var currentSecretMatches = BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, client.ClientSecret);
        if (currentSecretMatches)
            return CurrentRegistrationStatus.MatchesExistingSecret;

        if (client.IsInSecretChangePeriod() &&
            BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, client.PreviousClientSecret))
            return CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret;

        return CurrentRegistrationStatus.DoesNotMatchSecret;
    }
}