using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IUserConverter
{
    UserDataContract Convert(UserBusinessEntity user);

    AuthenticateResponseDataContract ConvertToResponse(UserBusinessEntity user, string token, bool passwordChangeRequird);

    UserBusinessEntity ConvertFromRequest(RegisterUserRequestDataContract request);
}