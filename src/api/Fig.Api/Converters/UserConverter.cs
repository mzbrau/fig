using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class UserConverter : IUserConverter
{
    public UserDataContract Convert(UserBusinessEntity user)
    {
        return new UserDataContract(user.Id, user.Username, user.FirstName, user.LastName, user.Role, user.ClientFilter, user.AllowedClassifications ?? []);
    }

    public AuthenticateResponseDataContract ConvertToResponse(UserBusinessEntity user, string token, bool passwordChangeRequired)
    {
        return new AuthenticateResponseDataContract(
            user.Id, 
            user.Username, 
            user.FirstName, 
            user.LastName, 
            user.Role,
            token, 
            passwordChangeRequired,
            user.AllowedClassifications ?? []);
    }

    public UserBusinessEntity ConvertFromRequest(RegisterUserRequestDataContract request)
    {
        return new UserBusinessEntity
        {
            Username = request.Username,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            ClientFilter = request.ClientFilter,
            AllowedClassifications = request.AllowedClassifications
        };
    }
}