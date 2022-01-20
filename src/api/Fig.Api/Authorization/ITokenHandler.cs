using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Authorization;

public interface ITokenHandler
{
    public string Generate(UserBusinessEntity user);

    public Guid? Validate(string? token);
}