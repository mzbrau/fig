namespace Fig.Api.Authorization;

public class ValidatedTokenData
{
    public ValidatedTokenData(Guid userId, bool passwordChangeRequired)
    {
        UserId = userId;
        PasswordChangeRequired = passwordChangeRequired;
    }

    public Guid UserId { get; }

    public bool PasswordChangeRequired { get; }
}
