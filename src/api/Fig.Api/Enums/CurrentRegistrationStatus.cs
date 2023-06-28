namespace Fig.Api.Enums;

public enum CurrentRegistrationStatus
{
    NoExistingRegistrations,
    MatchesExistingSecret,
    IsWithinChangePeriodAndMatchesPreviousSecret,
    DoesNotMatchSecret
}