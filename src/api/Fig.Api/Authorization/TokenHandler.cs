using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fig.Api.Authorization;

public class TokenHandler : ITokenHandler
{
    private readonly ApiSettings _apiSettings;

    public TokenHandler(IOptions<ApiSettings> apiSettings)
    {
        _apiSettings = apiSettings.Value;
    }

    public string Generate(UserBusinessEntity user)
    {
        var securityTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_apiSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("role", user.Role.ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(_apiSettings.TokenLifeMinutes),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = securityTokenHandler.CreateToken(tokenDescriptor);
        return securityTokenHandler.WriteToken(token);
    }

    public Guid? Validate(string? token)
    {
        if (token == null)
            return null;

        var id = ValidateInternal(token, _apiSettings.Secret);
        return id ?? ValidateInternal(token, _apiSettings.PreviousSecret);
    }

    private Guid? ValidateInternal(string? token, string secret)
    {
        var securityTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secret);
        try
        {
            securityTokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken) validatedToken;
            var userId = Guid.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

            // return user id from JWT token if validation successful
            return userId;
        }
        catch
        {
            return null;
        }
    }
}