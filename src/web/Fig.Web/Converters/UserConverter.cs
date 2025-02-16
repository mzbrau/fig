using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;

namespace Fig.Web.Converters;

public class UserConverter : IUserConverter
{
    public UserModel Convert(UserDataContract user)
    {
        return new UserModel
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ClientFilter = user.ClientFilter,
            AllowedClassifications = user.AllowedClassifications
        };
    }

    public UpdateUserRequestDataContract ConvertForUpdate(UserModel user)
    {
        return new UpdateUserRequestDataContract
        {
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            Password = user.Password,
            ClientFilter = user.ClientFilter,
            AllowedClassifications = user.AllowedClassifications
        };
    }

    public RegisterUserRequestDataContract ConvertForRegistration(UserModel user)
    {
        return new RegisterUserRequestDataContract(user.Username!, user.FirstName!, user.LastName!, user.Role,
            user.Password, user.ClientFilter!, user.AllowedClassifications);
    }

    public AuthenticatedUserModel Convert(AuthenticateResponseDataContract user)
    {
        return new AuthenticatedUserModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Token = user.Token,
            Role = user.Role,
            PasswordChangeRequired = user.PasswordChangeRequired,
            AllowedClassifications = user.AllowedClassifications
        };
    }
}