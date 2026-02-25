namespace Fig.Api.Validators;

public class ClientSecretValidator : IClientSecretValidator
{
    private const int MinLength = 32;
    private const int MinUniqueCharacters = 10;

    public bool IsValid(string clientSecret)
    {
        if (clientSecret.Length < MinLength)
            return false;

        var uniqueChars = new HashSet<char>(clientSecret).Count;
        return uniqueChars >= MinUniqueCharacters;
    }
}