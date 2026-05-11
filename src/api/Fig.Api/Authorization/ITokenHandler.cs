using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Authorization;

public interface ITokenHandler
{
    public string Generate(UserBusinessEntity user, bool passwordChangeRequired);

    public ValidatedTokenData? Validate(string? token);
}
