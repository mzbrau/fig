using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class UserConverter : IUserConverter
{
    public UserDataContract Convert(UserBusinessEntity user)
    {
        return new UserDataContract(user.Id, user.Username, user.FirstName, user.LastName, user.Role);
    }

    public AuthenticateResponseDataContract ConvertToResponse(UserBusinessEntity user)
    {
        return new AuthenticateResponseDataContract
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }

    public UserBusinessEntity ConvertFromRequest(RegisterUserRequestDataContract request)
    {
        return new UserBusinessEntity
        {
            Username = request.Username,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
        };
    }
}