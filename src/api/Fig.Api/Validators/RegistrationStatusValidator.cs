using System.Diagnostics;
using Fig.Api.Enums;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Validators;

public class RegistrationStatusValidator : IRegistrationStatusValidator
{
    private readonly IHashValidationCache _hashValidationCache;

    public RegistrationStatusValidator(IHashValidationCache hashValidationCache)
    {
        _hashValidationCache = hashValidationCache;
    }

    public CurrentRegistrationStatus GetStatus(List<SettingClientBusinessEntity>? existingRegistrations, string clientSecret)
    {
        if (existingRegistrations == null || !existingRegistrations.Any())
            return CurrentRegistrationStatus.NoExistingRegistrations;
            
        var firstRegistration = existingRegistrations.First();
        return GetStatus(firstRegistration, clientSecret);
    }

    public CurrentRegistrationStatus GetStatus(ClientBase client,
        string clientSecret)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var currentSecretMatches = _hashValidationCache.ValidateClientSecret(
            client.Name, 
            clientSecret, 
            client.ClientSecret);
        
        if (currentSecretMatches)
            return CurrentRegistrationStatus.MatchesExistingSecret;

        if (client.IsInSecretChangePeriod() &&
            _hashValidationCache.ValidateClientSecret(
                $"{client.Name}:previous", 
                clientSecret, 
                client.PreviousClientSecret!))
            return CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret;

        return CurrentRegistrationStatus.DoesNotMatchSecret;
    }
}