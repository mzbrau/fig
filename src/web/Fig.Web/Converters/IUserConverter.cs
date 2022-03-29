using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;

namespace Fig.Web.Converters;

public interface IUserConverter
{
    UserModel Convert(UserDataContract user);

    UpdateUserRequestDataContract ConvertForUpdate(UserModel user);

    RegisterUserRequestDataContract ConvertForRegistration(UserModel user);

    AuthenticatedUserModel Convert(AuthenticateResponseDataContract user);
}