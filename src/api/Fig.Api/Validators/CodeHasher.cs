using Fig.Api.SettingVerification.Dynamic;
using Microsoft.Extensions.Options;

namespace Fig.Api.Validators;

public class CodeHasher : ICodeHasher
{
    private readonly IOptions<ApiSettings> _apiSettings;

    public CodeHasher(IOptions<ApiSettings> apiSettings)
    {
        _apiSettings = apiSettings;
    }
    
    public string GetHash(string code)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword($"{code}{_apiSettings.Value.Secret}");
    }

    public bool IsValid(string hash, string? code)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify($"{code}{_apiSettings.Value.Secret}", hash);
    }
}