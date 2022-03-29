using Fig.Api.Exceptions;

namespace Fig.Api.Validators;

public class PasswordValidator : IPasswordValidator
{
    public void Validate(string password)
    {
        var result = Zxcvbn.Core.EvaluatePassword(password);

        if (result.Score < 3)
        {
            throw new InvalidPasswordException($"Password is too weak. {result.Feedback.Warning}");
        }
    }
}